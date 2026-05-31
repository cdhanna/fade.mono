using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fade.MonoGame;          // MonoGameTestHost
using Fade.MonoGame.Core;
using Fade.MonoGame.Lib;
using FadeBasic;
using FadeBasic.Sdk;          // CooperativePump
using FadeBasic.Launch;       // ITestLaunchable
using FadeBasic.Lib.Standard;
using FadeBasic.Sdk;
using FadeBasic.Testing;      // IFadeTestHost, FadeTestSessionContext, FadeTestRunContext, FadeTestResult
using FadeBasic.Virtual;      // HostMethodTable, TestManifestEntry
// FadeBasic.Sdk.Fade collides with the Fade.* MonoGame namespaces in name
// resolution. Alias it so `FadeSdk.TryCreateFromString(...)` is unambiguous.
using FadeSdk = FadeBasic.Sdk.Fade;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace WebRuntime.MonoGame.Pages
{
    // Bridge between the JS-driven rAF tick loop and a KNI-backed Fade Game1.
    //
    // Lifecycle:
    //   1. OnAfterRender(firstRender=true) compiles a tiny idle fbasic stub
    //      via FadeMonoGameCommands+StandardCommands, constructs Game1 with
    //      it, and calls game.Run() (non-blocking on KNI BlazorGL). Then it
    //      hands a DotNetObjectReference to JS so the rAF loop can call back.
    //   2. JS calls TickDotNet() once per requestAnimationFrame; that calls
    //      _game.Tick() which drives Game1.Update + Draw once.
    //   3. JS calls OnCanvasResized() on element resize → updates the back
    //      buffer + viewport via GraphicsDeviceManager.
    //   4. JS calls OnGameTimedOut() if a single tick blocked > the watchdog
    //      threshold (runaway fbasic loop) — we null the reference; future
    //      ticks become no-ops.
    //   5. Editor (or page boot) calls LoadProgram(source) to compile + swap
    //      a new fbasic source into the running game via Game1.LoadProgram.
    //      This covers both first-load and hot-reload — Game1's existing
    //      reload-on-flag path handles the swap on the next Update tick.
    public partial class Index : IDisposable
    {
        // Stub source the boot path compiles so we have a valid Game1 to
        // construct before the editor sends any user source. The do/loop
        // keeps the VM alive (without it, instructionIndex >= program.Length
        // immediately, which on desktop calls Quit()).
        private const string BootStubSource = @"do
  sync
loop
";

        private Game1 _game;
        private DotNetObjectReference<Index> _pageDotNetRef;
        private string _status = "booting…";
        // Active CommandCollection for the loaded program. Updated by
        // LoadProgramInternal on every (re-)compile so the cooperative
        // pump's CommandsAccessor returns the right surface. Same
        // pattern Export.Web uses (where FadeBridge points at its
        // _workspace.Commands).
        private CommandCollection _currentCommands;
        // Latched after first OnAfterRender so the HostBridge / WaitImpl
        // wire-up only happens once.
        private static bool _cooperativeWired;
        // Pause flag toggled by Stop / LoadProgram. The JS rAF still fires
        // TickDotNet every frame; this just makes the call a no-op so the
        // game freezes in place (canvas keeps whatever the last frame
        // rendered). Keeping the runtime warm means the next LoadProgram
        // is an instant reload, not a full KNI re-boot.
        private bool _paused;

        protected override async void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (!firstRender) return;

            try
            {
                WireCooperativePump();
                LoadProgramInternal(BootStubSource, initialBoot: true);
                _pageDotNetRef = DotNetObjectReference.Create(this);
                StateHasChanged();
                await JsRuntime.InvokeVoidAsync("initRenderJS", _pageDotNetRef);
            }
            catch (Exception ex)
            {
                _status = "boot error: " + ex.Message;
                StateHasChanged();
                Console.Error.WriteLine("Index.OnAfterRender boot error: " + ex);
            }
        }

        // Plug the host-agnostic CooperativePump into this runtime's
        // hooks. Same shape Export.Web does in FadeBridge's static ctor.
        // Game1.Update can later call CooperativePump.RunTick(BUDGET)
        // and the wait / prompt / stop flow will go through the shared
        // scheduler. Until that integration lands, wiring HostBridge
        // here means library commands (HostBridge.PostMessage /
        // SuspendVm) at least reach JS — the deeper "Game1 actually
        // honors the suspend" piece still needs the per-frame change.
        private void WireCooperativePump()
        {
            if (_cooperativeWired) return;
            _cooperativeWired = true;

            CooperativePump.CommandsAccessor = () => _currentCommands;

            StandardCommands.WaitImpl = ms => CooperativePump.OnCooperativeWait(ms);

            HostBridge.SuspendVm = () => CooperativePump.OnHostReplyWait();
            HostBridge.PostMessage = (channel, payload) =>
            {
                // Fire-and-forget call into JS. The library command
                // doesn't await this — it just posts the channel and
                // lets the page-side host-handler take it from there
                // (see runtime.js / the page's hostHandlers map).
                // We use IJSInProcessRuntime so the call is synchronous;
                // the JS function itself just posts a message and
                // returns immediately.
                if (JsRuntime is Microsoft.JSInterop.IJSInProcessRuntime inProc)
                {
                    inProc.InvokeVoid("postHostMessage", channel, payload);
                }
                else
                {
                    // Should never happen in Blazor WASM — JsRuntime
                    // is always IJSInProcessRuntime there. Fall back
                    // to fire-and-forget async if we ever run somewhere
                    // else (server-side prerender, tests).
                    _ = JsRuntime.InvokeVoidAsync("postHostMessage", channel, payload);
                }
            };

            // BrowserAudioBridge — every audio command in AudioCommands.cs
            // routes through these delegates on the BROWSER build. Same
            // sync-IJSInProcessRuntime pattern as HostBridge.PostMessage.
            // Method names match window.fadeAudio's surface in
            // wwwroot/index.html exactly.
            if (JsRuntime is Microsoft.JSInterop.IJSInProcessRuntime js)
            {
                BrowserAudioBridge.LoadClip = (clipId, name) =>
                    js.Invoke<bool>("fadeAudio.loadClip", clipId, name);
                BrowserAudioBridge.ReserveClipId = id =>
                    js.InvokeVoid("fadeAudio.reserveClipId", id);
                BrowserAudioBridge.GetHighestClipId = () =>
                    js.Invoke<int>("fadeAudio.highestClipId");

                BrowserAudioBridge.CreateInstance = (sfxId, clipId) =>
                    js.Invoke<bool>("fadeAudio.createInstance", sfxId, clipId);
                BrowserAudioBridge.ReserveInstanceId = id =>
                    js.InvokeVoid("fadeAudio.reserveInstanceId", id);
                BrowserAudioBridge.GetHighestInstanceId = () =>
                    js.Invoke<int>("fadeAudio.highestInstanceId");

                BrowserAudioBridge.Play = sfxId =>
                    js.InvokeVoid("fadeAudio.play", sfxId);
                BrowserAudioBridge.PlayWithDelay = (sfxId, ms) =>
                    js.InvokeVoid("fadeAudio.playWithDelay", sfxId, ms);
                BrowserAudioBridge.Pause = sfxId =>
                    js.InvokeVoid("fadeAudio.pause", sfxId);
                BrowserAudioBridge.Stop = sfxId =>
                    js.InvokeVoid("fadeAudio.stop", sfxId);

                BrowserAudioBridge.SetVolume = (sfxId, v) =>
                    js.InvokeVoid("fadeAudio.setVolume", sfxId, v);
                BrowserAudioBridge.GetVolume = sfxId =>
                    (float) js.Invoke<double>("fadeAudio.getVolume", sfxId);
                BrowserAudioBridge.SetPitch = (sfxId, p) =>
                    js.InvokeVoid("fadeAudio.setPitch", sfxId, p);
                BrowserAudioBridge.GetPitch = sfxId =>
                    (float) js.Invoke<double>("fadeAudio.getPitch", sfxId);
                BrowserAudioBridge.SetPan = (sfxId, p) =>
                    js.InvokeVoid("fadeAudio.setPan", sfxId, p);
                BrowserAudioBridge.GetPan = sfxId =>
                    (float) js.Invoke<double>("fadeAudio.getPan", sfxId);
                BrowserAudioBridge.SetLoop = (sfxId, b) =>
                    js.InvokeVoid("fadeAudio.setLoop", sfxId, b);

                BrowserAudioBridge.IsDone = sfxId =>
                    js.Invoke<bool>("fadeAudio.isDone", sfxId);

                BrowserAudioBridge.StopAll = () =>
                    js.InvokeVoid("fadeAudio.stopAll");
                BrowserAudioBridge.Reset = () =>
                    js.InvokeVoid("fadeAudio.clearAssets");
            }
        }

        [JSInvokable]
        public string TickDotNet()
        {
            if (_paused) return "[]";
            if (_game == null) return "[]";
            try
            {
                _game.Tick();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Game tick error: " + e);
                _game = null;
                _status = "runtime error: " + e.Message;
                StateHasChanged();
                return "[]";
            }
            // Drain any debug-session events the tick produced (stopped at
            // breakpoint, step-landed-here, scope changed, etc.). Returned
            // as JSON; the JS rAF loop dispatches each event to the
            // editor's debug control bar via monoGameHost.onDebugEvent.
            return DrainDebugEvents();
        }

        [JSInvokable]
        public void OnCanvasResized(int width, int height)
        {
            if (_game == null) return;
            var service = _game.Services.GetService(typeof(IGraphicsDeviceManager));
            if (service is GraphicsDeviceManager gdm)
            {
                gdm.PreferredBackBufferWidth = width;
                gdm.PreferredBackBufferHeight = height;
                gdm.ApplyChanges();
            }
            // Re-derive the letterbox transform against the new canvas
            // dimensions. SetMainRenderSize (called once when fbasic
            // source runs `set display W,H`) computed mainBufferPosition
            // + mainBufferScale against whatever the canvas size was at
            // *that* moment; if the canvas later resizes (window resize,
            // iframe layout settling on a host page, devicePixelRatio
            // change), those values go stale and the game renders in a
            // small corner. Recomputing on every resize keeps the
            // letterbox glued to the live canvas size. mainBuffer is
            // null until the user's program calls `set display`, so we
            // guard.
            if (Fade.MonoGame.Core.RenderSystem.mainBuffer != null)
            {
                Fade.MonoGame.Core.RenderSystem.ResetRenderPositioning();
            }
        }

        [JSInvokable]
        public void OnGameTimedOut(double frameMs)
        {
            _game = null;
            _status = $"stopped: frame blocked for {frameMs:F0}ms (watchdog)";
            StateHasChanged();
        }

        // Editor-driven Stop button. Pauses the VM (no further ticks) but
        // keeps Game1 + GraphicsDevice alive so the next LoadProgram reloads
        // instantly. The canvas keeps showing the last frame; we don't
        // black it out so the user can see what they last saw.
        //
        // Also halts every active audio instance — WebAudio playback does
        // not respect _paused on its own (the rAF tick loop only drives
        // VM update/draw, not the audio output), so without this any
        // currently-playing `play sfx`-spawned SoundEffectInstance keeps
        // emitting samples until the clip naturally ends.
        [JSInvokable]
        public void StopGame()
        {
            _paused = true;
            _status = "stopped";
            try { AudioInstanceSystem.StopAll(); }
            catch (Exception e) { Console.Error.WriteLine("StopGame: StopAll threw: " + e); }
            // BROWSER build uses Web Audio (BrowserAudioBridge) instead of
            // MonoGame's audio system. AudioInstanceSystem.StopAll only
            // touches the latter, so loud MP3s would keep playing after
            // Stop until the clip naturally ended. Call into JS too.
            try { BrowserAudioBridge.StopAll(); }
            catch (Exception e) { Console.Error.WriteLine("StopGame: BrowserAudioBridge.StopAll threw: " + e); }
            StateHasChanged();
        }

        // The main editor entry point — compile a new fbasic source against
        // the Fade.MonoGame command surface and either construct or
        // hot-reload the game. Returns true on success; surface compile
        // errors via the page status and console.
        [JSInvokable]
        public bool LoadProgram(string source)
        {
            return LoadProgramInternal(source, initialBoot: false);
        }

        // Two-phase compile/run used by the playground's asset pipeline:
        //
        //   1. CompileForRun(source)
        //        - Resets ContentSystem.entries
        //        - Compiles the source (runs the macro pass, which populates
        //          ContentSystem.entries with `push asset` / `texture
        //          compression` / `default texture compression` calls)
        //        - Stashes the FadeRuntimeContext in _pendingContext
        //        - Returns `{ ok, errors, plan }` — the plan is a serialised
        //          snapshot of ContentSystem (entries + default compression)
        //          the editor uses to compile + register assets BEFORE the
        //          program ticks.
        //
        //   2. BeginPendingProgram()
        //        - Hands the stashed context to Game1, un-pauses, clears
        //          _pendingContext. The next TickDotNet drives the user's
        //          program with all assets already in place.
        //
        // Callers that don't care about the asset plan keep using the
        // single-shot LoadProgram above.
        private FadeRuntimeContext _pendingContext;

        [JSInvokable]
        public string CompileForRun(string source)
        {
            try
            {
                // Pause Game1's tick BEFORE we let the editor wipe the
                // asset cache below. ClearAssets() disposes every loaded
                // Texture2D (via ContentManager.Unload), and the previous
                // program's Draw is mid-frame at this point — without
                // pausing, its next tick references disposed GPU resources
                // and the rAF loop parks itself with a runtime error. The
                // matching `_paused = false` is in BeginPendingProgram,
                // which fires after every asset has been re-registered.
                _paused = true;

                // Clear any prior plan so a re-Run with the same source
                // doesn't see stale entries from the previous compile.
                ContentSystem.Reset();

                var commands = new CommandCollection(
                    new FadeMonoGameCommands(),
                    new StandardCommands());

                if (!FadeSdk.TryCreateFromString(source, commands, out var ctx, out var errors))
                {
                    return JsonSerializer.Serialize(new
                    {
                        ok = false,
                        error = errors.ToDisplay(),
                        plan = (object) null,
                    }, _contentPlanJsonOpts);
                }

                // Stash for BeginPendingProgram. Commands are stashed too
                // so the cooperative pump rebinds to the same surface the
                // macro pass executed against — keeping CALL_HOST indices
                // resolvable identically.
                _pendingContext = ctx;
                _pendingCommands = commands;

                return JsonSerializer.Serialize(new
                {
                    ok = true,
                    error = (string) null,
                    plan = SerializeContentPlan(),
                }, _contentPlanJsonOpts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("CompileForRun error: " + ex);
                return JsonSerializer.Serialize(new
                {
                    ok = false,
                    error = "CompileForRun exception: " + ex.Message,
                    plan = (object) null,
                }, _contentPlanJsonOpts);
            }
        }

        // Caller-supplied access to the most recent plan without
        // recompiling. Useful if the editor needs to re-fetch (e.g. after
        // a transient OPFS read failure) without losing the pending ctx.
        [JSInvokable]
        public string GetContentBuildPlan() =>
            JsonSerializer.Serialize(SerializeContentPlan(), _contentPlanJsonOpts);

        [JSInvokable]
        public bool BeginPendingProgram()
        {
            try
            {
                if (_pendingContext == null) return false;
                var ctx = _pendingContext;
                _pendingContext = null;
                _currentCommands = _pendingCommands;
                _pendingCommands = null;
                CooperativePump.RunStartWithVm(ctx.Machine);

                if (_game == null)
                {
                    _game = new Game1(ctx);
                    _game.Run();
                    _status = "running";
                }
                else
                {
                    _game.LoadProgram(ctx);
                    _status = "reloaded";
                }
                _paused = false;
                StateHasChanged();
                return true;
            }
            catch (Exception ex)
            {
                _status = "begin error: " + ex.Message;
                Console.Error.WriteLine("BeginPendingProgram error: " + ex);
                StateHasChanged();
                return false;
            }
        }

        private CommandCollection _pendingCommands;

        private static readonly JsonSerializerOptions _contentPlanJsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
        };

        // Build the JSON-friendly snapshot of ContentSystem. Mirrors the
        // ContentEntry struct one-for-one so callers see the same surface
        // the desktop builder consumes — parameters[] is preserved so the
        // playground can interpret per-content-kind keys (notably the
        // `Compression` key set by the texture compression macros).
        private object SerializeContentPlan()
        {
            var entries = new List<object>(ContentSystem.entries.ptr);
            for (var i = 0; i < ContentSystem.entries.ptr; i++)
            {
                ref var e = ref ContentSystem.entries.buffer[i];
                entries.Add(new
                {
                    path       = e.path,
                    name       = e.name,
                    importer   = e.importer.ToString(),
                    processor  = e.processr.ToString(),
                    parameters = e.parameters ?? new Dictionary<string, string>(),
                });
            }
            return new
            {
                defaultCompression = ContentSystem.CompressionToString(
                    ContentSystem.defaultTextureCompression),
                entries,
            };
        }

        // Register a single XNB asset's bytes with the running Game1's
        // BrowserContentManager. The page calls this once per .xnb in the
        // project before invoking LoadProgram, so any `texture`/`load sfx
        // clip`/`font` commands fbasic runs can resolve via stock
        // Content.Load<T>(name).
        //
        // `name` should be the bare asset name (no extension), matching the
        // string fbasic passes to `texture`/`load sfx clip`. The page
        // strips `.xnb` before calling, but BrowserContentManager also
        // tolerates a trailing `.xnb` defensively.
        [JSInvokable]
        public void RegisterAsset(string name, byte[] bytes)
        {
            if (_game == null) return;
            _game.RegisterAsset(name, bytes);
        }

        // Wipe the registered asset dict — used when the editor switches
        // projects so stale assets from the previous project don't bleed
        // into the new run.
        [JSInvokable]
        public void ClearAssets()
        {
            _game?.BrowserContent?.ClearAssets();
        }

        // Evict a single asset by name (bytes + cached Texture2D/etc.).
        // The playground's per-asset sync uses this to invalidate stale
        // assets when their source bytes change, keeping unrelated
        // assets cached across Runs.
        [JSInvokable]
        public void UnregisterAsset(string name)
        {
            _game?.BrowserContent?.UnregisterAsset(name);
        }

        // ─── Cooperative-pump deposit entry points ──────────────────
        // Match FadeBridge's [JSExport] DepositResult* surface — the
        // JS host-handler dispatcher calls one of these after the
        // page handles a host-message (e.g. prompt$). Same wire shape
        // as Export.Web's, just routed through Blazor's JSInvokable
        // instead of JSExport. Each delegates to CooperativePump.

        [JSInvokable]
        public string DepositResultString(string value) =>
            CooperativePump.DepositResultString(value);

        [JSInvokable]
        public string DepositResultInt(int value) =>
            CooperativePump.DepositResultInt(value);

        [JSInvokable]
        public string DepositResultReal(float value) =>
            CooperativePump.DepositResultReal(value);

        [JSInvokable]
        public string DepositResultBool(bool value) =>
            CooperativePump.DepositResultBool(value);

        [JSInvokable]
        public string DepositResultByte(byte value) =>
            CooperativePump.DepositResultByte(value);

        [JSInvokable]
        public string DepositResultWord(int value) =>
            CooperativePump.DepositResultWord(value);

        [JSInvokable]
        public string DepositResultDword(int value) =>
            CooperativePump.DepositResultDword(value);

        [JSInvokable]
        public string DepositResultDint(long value) =>
            CooperativePump.DepositResultDint(value);

        [JSInvokable]
        public string DepositResultDfloat(double value) =>
            CooperativePump.DepositResultDfloat(value);

        [JSInvokable]
        public string DepositResultVoid() =>
            CooperativePump.DepositResultVoid();

        // Returns JSON with FadeBasic + KNI + .NET version strings for the
        // browser's Diagnostics panel.
        [JSInvokable]
        public string GetVersionInfo()
        {
            var asm = typeof(FadeBasic.Virtual.VirtualMachine).Assembly;
            var attrs = (System.Reflection.AssemblyInformationalVersionAttribute[])
                asm.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
            var fadeVersion = attrs.Length > 0 ? attrs[0].InformationalVersion : asm.GetName().Version?.ToString() ?? "unknown";
            var kniVersion = typeof(Microsoft.Xna.Framework.Game).Assembly.GetName().Version?.ToString() ?? "unknown";
            var dotnetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                fadeBasic = fadeVersion,
                kni = kniVersion,
                dotnet = dotnetVersion,
            });
        }

        private bool LoadProgramInternal(string source, bool initialBoot)
        {
            try
            {
                // CommandCollection: Fade.MonoGame.Lib first so its `print`
                // (which writes to MonoGame Console) wins over Standard's,
                // matching desktop precedence.
                var commands = new CommandCollection(
                    new FadeMonoGameCommands(),
                    new StandardCommands());

                if (!FadeSdk.TryCreateFromString(source, commands, out var ctx, out var errors))
                {
                    var msg = "compile error:\n" + errors.ToDisplay();
                    _status = "compile error";
                    Console.Error.WriteLine(msg);
                    if (!initialBoot) StateHasChanged();
                    return false;
                }

                // Hand the freshly-compiled commands + VM to the
                // cooperative pump. Library commands routed through
                // HostBridge (prompt$, future async ops) will reach
                // the right VM via SuspendVm → CooperativePump.RunVm.
                // Game1's per-frame Update still drives the actual
                // tick; once that's pump-aware, the integration is
                // complete. Until then, the wiring is correct but
                // SuspendVm-induced waits don't yet resume on their
                // own — see mg-export-3.md phase 2 remaining work.
                _currentCommands = commands;
                CooperativePump.RunStartWithVm(ctx.Machine);

                if (_game == null)
                {
                    _game = new Game1(ctx);
                    _game.Run();
                    _status = initialBoot ? "running (boot stub)" : "running";
                }
                else
                {
                    _game.LoadProgram(ctx);
                    _status = "reloaded";
                }
                // Un-pause so subsequent ticks resume rendering. A user
                // can Stop → edit → Run flow and we pick up smoothly.
                _paused = false;

                if (!initialBoot) StateHasChanged();
                return true;
            }
            catch (Exception ex)
            {
                _status = "load error: " + ex.Message;
                Console.Error.WriteLine("LoadProgram error: " + ex);
                if (!initialBoot) StateHasChanged();
                return false;
            }
        }

        // ─── Testing bridge ────────────────────────────────────────────
        // Lists + runs tests through the same MonoGameTestHost +
        // Game1.QueueTest plumbing that desktop's MTP-driven test runs
        // use. Each test runs on the live Game1 VM (so MonoGame commands
        // like sprite/texture/sync hit a real GraphicsDevice), with the
        // test-queue Update-loop dispatching dequeues + ResetFade-with-
        // entry. The host's RunTestAsync awaits a TCS that Game1.Update
        // SetResults on test-VM completion; the rAF tick loop keeps
        // ticking under the await thanks to RunContinuationsAsynchronously
        // on the TCS (see Game1.QueueTest).

        private static readonly JsonSerializerOptions _testJsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
        };

        // Test session state. The host + session context are constructed
        // lazily on first test invocation and reused across calls until
        // the page reloads — InitializeAsync / BeforeAllTestsAsync fire
        // once per session, not per test, matching MTP's contract. We
        // intentionally do NOT call AfterAllTestsAsync between
        // interactive single-test invocations because doing so would set
        // game.allTestsDone = true and freeze the Update loop's test-mode
        // dispatch.
        private MonoGameTestHost _testHost;
        private FadeTestSessionContext _testSession;
        private bool _testHostInitialized;

        // Reusable cancellation source for the active test. The page can
        // (eventually) signal cancellation through a JSInvokable; for now
        // the source is created per-run and left ungated.
        private CancellationTokenSource _testCts;

        [JSInvokable]
        public string ListTests(string source)
        {
            try
            {
                var ctx = CompileForTests(source, out var compileError);
                if (ctx == null)
                {
                    Console.Error.WriteLine("ListTests compile error: " + compileError);
                    return "[]";
                }

                var tests = new List<object>();
                foreach (var t in ctx.Compiler.TestManifest)
                {
                    tests.Add(new
                    {
                        name = t.name,
                        isAbstract = t.isAbstract,
                        fromParent = t.fromParent,
                        sourceLine = t.sourceLine,
                        sourceChar = t.sourceChar,
                    });
                }
                return JsonSerializer.Serialize(tests, _testJsonOpts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ListTests failed: " + ex);
                return "[]";
            }
        }

        // Runs a single test (or all concrete tests when testName is
        // empty) on the live Game1 VM via MonoGameTestHost. Async because
        // host.RunTestAsync awaits the QueueTest TCS that Game1.Update
        // resolves when the test VM finishes.
        [JSInvokable]
        public async Task<string> RunTests(string source, string testName)
        {
            try
            {
                var ctx = CompileForTests(source, out var compileError);
                if (ctx == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        passed = 0,
                        failed = 0,
                        error = "Compile failed:\n" + compileError,
                        results = Array.Empty<object>(),
                    }, _testJsonOpts);
                }

                if (_game == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        passed = 0,
                        failed = 0,
                        error = "Game not booted — Run a monogame program first.",
                        results = Array.Empty<object>(),
                    }, _testJsonOpts);
                }

                // Resolve which entries we want to execute. Empty
                // testName → run every concrete (non-abstract) test in
                // the manifest. Named → exactly that one.
                var entries = new List<TestManifestEntry>();
                if (string.IsNullOrWhiteSpace(testName))
                {
                    foreach (var t in ctx.Compiler.TestManifest)
                    {
                        if (!t.isAbstract) entries.Add(t);
                    }
                }
                else
                {
                    TestManifestEntry hit = null;
                    foreach (var t in ctx.Compiler.TestManifest)
                    {
                        if (string.Equals(t.name, testName, StringComparison.OrdinalIgnoreCase))
                        {
                            hit = t;
                            break;
                        }
                    }
                    if (hit == null)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            passed = 0,
                            failed = 0,
                            error = $"No test named '{testName}' found in the source.",
                            results = Array.Empty<object>(),
                        }, _testJsonOpts);
                    }
                    if (hit.isAbstract)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            passed = 0,
                            failed = 0,
                            error = $"Test '{testName}' is abstract and cannot be run directly.",
                            results = Array.Empty<object>(),
                        }, _testJsonOpts);
                    }
                    entries.Add(hit);
                }

                // Enter test mode without debug. The page-side dbg.start
                // path also re-enters test mode with debug for the
                // Debug-Test button — see Index.Debug.cs DebugStartTest.
                _game.SetTestMode(true, withDebug: false);
                _paused = false;

                // Keep LatestRuntime in sync with the test source so
                // Game1.Update's assertion-failure source-map lookup uses
                // the right FadeRuntimeContext (not a stale boot stub).
                GameReloader.SetBuild(ctx);

                await EnsureHostInitializedAsync(ctx).ConfigureAwait(false);

                var startedAt = DateTime.UtcNow;
                var results = new List<FadeTestResult>();
                var passedCount = 0;
                var failedCount = 0;
                _testCts?.Dispose();
                _testCts = new CancellationTokenSource();
                var hostMethods = HostMethodTable.FromCommandCollection(ctx.CommandCollection);

                foreach (var entry in entries)
                {
                    if (_testCts.IsCancellationRequested) break;
                    var runCtx = new FadeTestRunContext(ctx, entry, hostMethods);
                    FadeTestResult r;
                    try
                    {
                        r = await _testHost.RunTestAsync(runCtx, _testCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        r = new FadeTestResult
                        {
                            testName = entry.name,
                            passed = false,
                            failureMessage = "test host threw: " + ex.Message,
                        };
                    }
                    results.Add(r);
                    if (r.passed) passedCount++;
                    else failedCount++;
                }

                var payload = new
                {
                    passed = passedCount,
                    failed = failedCount,
                    duration = (DateTime.UtcNow - startedAt).TotalMilliseconds,
                    results = ResultsToObjects(results),
                };

                // Exit test mode so the next LoadProgram (Run button) can
                // reload normally. Without this, _testMode stays true and
                // Game1.Update returns early from the test-mode block before
                // it ever reaches the _reloadRequestedFromUi check, blocking
                // any subsequent game run.
                _game?.SetTestMode(false);
                _paused = true;
                try { AudioInstanceSystem.StopAll(); }
                catch (Exception e) { Console.Error.WriteLine("RunTests StopAll: " + e); }
                try { BrowserAudioBridge.StopAll(); }
                catch (Exception e) { Console.Error.WriteLine("RunTests BrowserAudioBridge.StopAll: " + e); }
                _status = "tests done";
                StateHasChanged();

                return JsonSerializer.Serialize(payload, _testJsonOpts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("RunTests threw: " + ex);
                _game?.SetTestMode(false);
                _paused = true;
                return JsonSerializer.Serialize(new
                {
                    passed = 0,
                    failed = 0,
                    error = "Runtime error: " + ex.GetType().Name + ": " + ex.Message,
                    results = Array.Empty<object>(),
                }, _testJsonOpts);
            }
        }

        // Shared between ListTests, RunTests, and DebugStartTest — every
        // test-related JSInvokable compiles fresh against the FadeMonoGame
        // command surface, then walks the produced FadeRuntimeContext
        // (which implements ITestLaunchable) to find / iterate tests.
        // Returns null + a formatted error message on compile failure.
        internal FadeRuntimeContext CompileForTests(string source, out string error)
        {
            var commands = new CommandCollection(
                new FadeMonoGameCommands(),
                new StandardCommands());
            if (!FadeSdk.TryCreateFromString(source, commands, out var ctx, out var errors))
            {
                error = errors.ToDisplay();
                return null;
            }
            error = null;
            return ctx;
        }

        // Lazy host bring-up. The host + session-context survive across
        // every test invocation in this Index instance's lifetime; only
        // the first call pays the InitializeAsync + BeforeAllTestsAsync
        // cost. Re-entrancy-safe because each invocation awaits on the
        // same _testHostInitialized flag before doing work.
        internal async Task EnsureHostInitializedAsync(FadeRuntimeContext launchable)
        {
            _testHost ??= new MonoGameTestHost(_game);
            _testSession ??= new FadeTestSessionContext(launchable, services: null);

            if (!_testHostInitialized)
            {
                await _testHost.InitializeAsync(_testSession, CancellationToken.None).ConfigureAwait(false);
                await _testHost.BeforeAllTestsAsync(_testSession, CancellationToken.None).ConfigureAwait(false);
                _testHostInitialized = true;
            }
        }

        private static List<object> ResultsToObjects(List<FadeTestResult> results)
        {
            var list = new List<object>(results.Count);
            foreach (var r in results)
            {
                list.Add(new
                {
                    name = r.testName,
                    passed = r.passed,
                    duration = r.duration.TotalMilliseconds,
                    failureMessage = r.failureMessage,
                    failureReason = r.failureReason,
                    failureSourceText = r.failureSourceText,
                });
            }
            return list;
        }

        public void Dispose()
        {
            _pageDotNetRef?.Dispose();
            _pageDotNetRef = null;
            _game = null;
        }
    }
}
