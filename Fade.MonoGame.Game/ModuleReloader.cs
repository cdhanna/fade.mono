using System;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Sdk;
using FadeBasic.Virtual;
using FadeBasic.Virtual.HotReload;

namespace Fade.MonoGame.Core
{
    /// <summary>
    /// Opt-in, state-preserving "module reload" for the running Fade program.
    ///
    /// Distinct from the F1 full restart (which rebuilds the VM from scratch and
    /// resets all state). This drives a <see cref="HotReloadSession"/> over the
    /// LIVE VM: when the file watcher produces a new build, it classifies the
    /// edit against the current VM at a frame safepoint (the `sync` boundary):
    ///   - ApplicableNow  → a BLUE bar; F2 applies it live, preserving state.
    ///   - PermanentlyRude → a RED bar;  only an F1 full restart can apply it.
    ///
    /// Requires FadeBasic.Lang.Core with the hot-reload API (>= 0.1.3.2).
    /// </summary>
    public sealed class ModuleReloader
    {
        VirtualMachine _vm;
        CommandCollection _commands;
        HotReloadSession _session;
        object _lastArmedBuild;

        public Verdict Verdict { get; private set; } = Verdict.NoChange;

        /// <summary>A state-preserving reload is ready to accept (blue bar).</summary>
        public bool IsModuleReloadReady =>
            _session != null && _session.HasPending && Verdict == Verdict.ApplicableNow;

        /// <summary>The pending edit can't apply live — needs an F1 full restart (red bar).</summary>
        public bool IsRude => Verdict == Verdict.PermanentlyRude;

        public string RudeReason { get; private set; }

        /// <summary>
        /// Bind to a freshly-loaded program. <paramref name="currentContext"/> is
        /// the running program (its SourceMap gives the baseline source). Disabled
        /// (no-op) if the program isn't a source-carrying runtime context.
        /// </summary>
        public void Bind(VirtualMachine vm, CommandCollection commands, FadeRuntimeContext currentContext)
        {
            _vm = vm;
            _commands = commands;
            _session = null;
            _lastArmedBuild = currentContext;
            Verdict = Verdict.NoChange;
            RudeReason = null;

            // OLD facts MUST come from the SAME compiler that produced the
            // bytecode the VM is running (ctx.Compiler.Program == vm.program),
            // so the running instructionIndex / call frames map into the facts'
            // statement table. Recompiling the source fresh would produce a
            // different instruction layout and the control gate could never say
            // ApplicableNow (→ no blue bar).
            if (currentContext == null)
            {
                Console.WriteLine("[module-reload] DISABLED: running program is not a FadeRuntimeContext");
                return;
            }
            if (currentContext.Compiler == null)
            {
                Console.WriteLine("[module-reload] DISABLED: runtime context has no Compiler");
                return;
            }
            if (currentContext.Compiler.DebugData == null)
            {
                Console.WriteLine("[module-reload] DISABLED: program compiled without debug data");
                return;
            }

            var facts = ProgramFacts.FromCompiler(currentContext.Compiler);
            _session = new HotReloadSession(vm, facts, CompileOrThrow);
            Console.WriteLine("[module-reload] ENABLED — edit a .fbasic and save; blue bar + F2 to apply");
        }

        /// <summary>
        /// Arm a parent-supplied build immediately and classify it (no apply).
        /// Used by the Playground iframe's <c>ReloadArm</c> JSInvokable, which —
        /// unlike the F2 file-watcher path — is driven explicitly by the editor
        /// rather than by <see cref="SyncPoint"/> detecting a newer build. The
        /// actual apply still happens at the next frame safepoint (SyncPoint with
        /// acceptPressed). Returns the classification verdict.
        /// </summary>
        // NOTE: takes the source STRING explicitly. The Playground compiles the
        // new build via FadeSdk.TryCreateFromString(source), which leaves
        // ctx.SourceMap == null (only file-watcher builds carry a SourceMap). The
        // old code read build.SourceMap.fullSource, got null, and bailed to
        // NoChange — so a parent-armed edit silently never armed. Arm off the
        // caller-supplied source instead; `build` is kept only to realign
        // _fadeProgram after a commit (F1 restart uses it).
        public Verdict ArmAndClassify(FadeRuntimeContext build, string source)
        {
            if (_session == null) { Verdict = Verdict.NoChange; return Verdict; }
            if (string.IsNullOrEmpty(source)) { Verdict = Verdict.NoChange; return Verdict; }
            _lastArmedBuild = build;          // so SyncPoint won't re-arm this same build
            _session.Arm(source);
            Classify();                       // advisory only — the verdict the arm returns is
            return Verdict;                   // for the editor UI; TryCommitPending re-checks each frame.
        }

        /// <summary>True when a program with debug data is bound (reload possible).</summary>
        public bool IsEnabled => _session != null;

        /// <summary>True while an armed edit still hasn't been committed or dropped.</summary>
        public bool HasPendingReload => _session != null && _session.HasPending;

        /// <summary>
        /// Debug data for the program the session is CURRENTLY bound to (advances
        /// on each commit). Used to rebind an attached debug session after a
        /// reload so its statement map matches the just-swapped bytecode.
        /// </summary>
        public DebugData CurrentDebugData => _session?.CurrentFacts?.Debug;

        /// <summary>
        /// Per-frame drive for a parent-armed (Playground) reload. Unlike
        /// <see cref="SyncPoint"/>'s F2 gate — which only applies when the
        /// ONE-SHOT arm-time <see cref="Verdict"/> happened to be ApplicableNow —
        /// this ALWAYS re-classifies (via <see cref="HotReloadSession.Tick"/>) at
        /// each safepoint, so an edit that was transiently blocked at arm time
        /// (the VM was sitting on the edited statement) DRAINS and commits on a
        /// later frame. This is the exact behaviour the web CooperativePump has;
        /// the monogame path was missing it, so armed edits could stick forever.
        /// Returns true if the reload committed this frame.
        /// </summary>
        public bool TryCommitPending()
        {
            if (_session == null || !_session.HasPending) return false;
            if (Verdict == Verdict.PermanentlyRude) return false; // never applies live
            if (!AdvanceToCleanBoundary()) return false;
            try
            {
                var plan = _session.Tick();   // re-classifies + applies iff ApplicableNow
                // An empty diff (identical source) classifies NoChange but leaves
                // the edit armed — drop it so we don't re-check every frame forever.
                if (_session.HasPending && plan.Verdict == Verdict.NoChange) _session.Cancel();
                Verdict = _session.HasPending ? plan.Verdict : Verdict.NoChange;
                RudeReason = plan.RudeReason;
                if (plan.Verdict == Verdict.ApplicableNow)
                    Console.WriteLine($"[module-reload] committed (web) → ip={_vm.instructionIndex}");
                return plan.Verdict == Verdict.ApplicableNow;
            }
            catch
            {
                _session.Cancel();
                Verdict = Verdict.NoChange;
                return false;
            }
        }

        /// <summary>
        /// Called once per frame at the `sync` safepoint (VM suspended). Arms the
        /// session when a newer build appears, classifies it, and — if F2 was
        /// pressed and the edit is applicable — commits the module reload live.
        /// </summary>
        /// <returns>true if a module reload was committed this frame.</returns>
        public bool SyncPoint(FadeRuntimeContext latestBuild, bool acceptPressed)
        {
            if (_session == null) { Verdict = Verdict.NoChange; return false; }

            // Arm when a newer build than the one we're running shows up.
            if (latestBuild != null && !ReferenceEquals(latestBuild, _lastArmedBuild))
            {
                _lastArmedBuild = latestBuild;
                var src = latestBuild.SourceMap?.fullSource;
                if (!string.IsNullOrEmpty(src))
                {
                    _session.Arm(src);
                    Classify(); // evaluate once, at this safepoint
                    Console.WriteLine($"[module-reload] {Verdict}"
                        + (Verdict == Verdict.PermanentlyRude ? $": {RudeReason}" : "")
                        + (Verdict == Verdict.ApplicableNow ? " — press F2 to apply" : ""));
                }
            }

            if (!_session.HasPending) { Verdict = Verdict.NoChange; return false; }

            if (acceptPressed && Verdict == Verdict.ApplicableNow)
            {
                // Apply only at a CLEAN statement boundary — PC exactly at a
                // statement start (offset 0) AND no active function frames. This
                // is what the console watch does, and it's why the console is
                // robust: resuming at a NEW statement's start address is immune
                // to bytecode shifts, whereas resuming at the arbitrary post-sync
                // PC (offset > 0) lands on a shifted byte → the VM diverges
                // (freeze). We roll the VM forward to that boundary first.
                if (!AdvanceToCleanBoundary())
                {
                    Console.WriteLine("[module-reload] could not reach a clean boundary; try again");
                    return false;
                }
                try
                {
                    Console.WriteLine($"[module-reload] applying at boundary ip={_vm.instructionIndex}, depth={_vm.methodStack.ptr}");
                    var plan = _session.Tick();
                    Verdict = _session.HasPending ? plan.Verdict : Verdict.NoChange;
                    RudeReason = plan.RudeReason;
                    Console.WriteLine($"[module-reload] applied → ip={_vm.instructionIndex}, verdict={plan.Verdict}");
                    return plan.Verdict == Verdict.ApplicableNow;
                }
                catch
                {
                    // compile/apply failed — drop the pending edit; a fresh save re-arms
                    _session.Cancel();
                    Verdict = Verdict.NoChange;
                }
            }
            return false;
        }

        // Roll the VM forward until the PC sits exactly at a statement START —
        // the clean boundary the reload can resume at robustly (immune to
        // bytecode-length shifts). From a post-`sync` PC in the main loop this is
        // only a handful of instructions away. Bounded so a pathological program
        // can't hang the accept.
        //
        // We do NOT require methodStack.ptr == 0. Top-level-only gating meant a
        // game whose main loop lives inside a function (e.g. an update proc) never
        // reached a boundary, so an ApplicableNow edit armed but never committed.
        // The core applies fine at depth > 0 (RemapProgramCounter remaps every
        // call frame; Tick re-runs the control gate before committing). See
        // SafepointTests.ControlGate_InsideFunction_EditUnrelatedPastLine_Safe.
        bool AdvanceToCleanBoundary()
        {
            var facts = _session.CurrentFacts;
            for (var i = 0; i < 500_000; i++)
            {
                bool atStatementStart =
                    HotReloadUtil.StatementStartForInstruction(facts, _vm.instructionIndex) == _vm.instructionIndex;
                if (atStatementStart) return true;
                if (_vm.instructionIndex >= _vm.program.Length) return false;
                _vm.isSuspendRequested = false;
                _vm.Execute2(1); // step one instruction
            }
            return false;
        }

        void Classify()
        {
            try
            {
                var plan = _session.Poll();
                Verdict = plan.Verdict;
                RudeReason = plan.RudeReason;
            }
            catch
            {
                Verdict = Verdict.NoChange;
            }
        }

        Compiler CompileOrThrow(string src)
        {
            if (!Launcher.TryCompileSource(src, _commands, out var c, out var err))
                throw new Exception(err);
            return c;
        }
    }
}
