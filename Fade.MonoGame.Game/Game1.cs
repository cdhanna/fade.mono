using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if !BROWSER
using ImGuiNET;
#endif
using FadeBasic.Launch;
using FadeBasic.Sdk;
using FadeBasic.Testing;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#if !BROWSER
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
#endif
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

using Mouse = Microsoft.Xna.Framework.Input.Mouse;

namespace Fade.MonoGame.Core;

public class Game1 : Microsoft.Xna.Framework.Game
{
    public ConcurrentQueue<QueuedTest> queuedTests = new ConcurrentQueue<QueuedTest>();
    public QueuedTest currentTest = null;
    // Set by MonoGameTestHost.AfterAllTestsAsync once MTP has dispatched every
    // test. Until then, an empty queue means "MTP hasn't enqueued the next one
    // yet" — not "we're done" — so the testMode branch must idle, not Quit.
    public volatile bool allTestsDone = false;

    public class QueuedTest
    {
        public FadeTestRunContext ctx;
        public CancellationToken ct;
        public FadeTestResult result;
        public TaskCompletionSource source;
    }
    public async Task<FadeTestResult> QueueTest(FadeTestRunContext ctx, CancellationToken ct)
    {
        // return new FadeTestResult
        // {
        //     passed = true
        // };
        // add the data
        FileLog.WriteLine("Queue Test " + ctx.Entry.name);
        var q = new QueuedTest
        {
            ctx = ctx,
            ct = ct,
            result = new FadeTestResult
            {
                testName = ctx.Entry.name
            },
            // RunContinuationsAsynchronously: SetResult() is called from the
            // game's Update tick (main thread). With the default TCS options,
            // MTP's continuation would run inline on main, processing the
            // result and looping into the next test's RunTestAsync — all while
            // hijacking the thread that needs to be free to run the next
            // Update. This flag posts continuations to the ThreadPool, so the
            // game thread returns immediately after SetResult.
            source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        queuedTests.Enqueue(q);
        await q.source.Task;
        return q.result;
    }
    
    private ILaunchable _fadeProgram;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    public VirtualMachine _vm;
    // Public so the browser JS bridge (Pages/Index.razor.cs) can drive
    // step/pause/breakpoint requests + drain outbound messages. Desktop
    // uses its TCP socket and never accesses this from outside.
    public DebugSession DebugSession => _debugSession;
    private DebugSession _debugSession;
#if BROWSER
    // Surfaces the BrowserDebugSession subclass for JS-bridged callers
    // that need its extra public helpers (Enqueue/DrainOutbound/DebugDataAccess).
    // Same instance as _debugSession — just typed more specifically.
    public BrowserDebugSession BrowserDebugSession => (BrowserDebugSession)_debugSession;
#endif
    private LaunchOptions _options;
#if !BROWSER
    public ImGuiRenderer _imguiRenderer;
    public ContentWatcher ContentWatcher;
#endif

    private Texture2D _pixel;
    private bool _autoAcceptNewBuilds;
    private bool _reloadRequestedFromUi;
    // State-preserving "module reload" (F2 / blue bar), distinct from F1's full
    // restart (red bar). See ModuleReloader.
    private readonly ModuleReloader _moduleReloader = new ModuleReloader();
    private bool _f2WasDown;
    // Parent-driven (Playground iframe) state-preserving reload. The editor's
    // Reload button arms a build via ReloadArm → ArmModuleReload; unlike the F2
    // path we auto-accept, so the frame safepoint applies it without a keypress.
    private FadeRuntimeContext _pendingWebReload;
    private bool _webReloadAutoAccept;

 
    public Game1(ILaunchable fadeProgram, bool autoAcceptNewBuilds=false, bool testMode=false)
    {
        _testMode = testMode;
        _autoAcceptNewBuilds = autoAcceptNewBuilds;
        _fadeProgram = fadeProgram;



        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
#if BROWSER
        // Browser has no filesystem to serve XNBs from. Swap the default
        // ContentManager for one backed by a dict the page fills via
        // RegisterAsset() before LoadProgram. The base Game.Content setter
        // disposes the old one for us.
        Content = new BrowserContentManager(Services) { RootDirectory = "Content" };
        _browserContent = (BrowserContentManager)Content;
#endif
        IsMouseVisible = true;
    }

#if BROWSER
    // Cached strongly-typed handle to the BrowserContentManager that
    // Content was swapped to in the ctor. JS-bridged callers
    // (WebRuntime.MonoGame Index.razor.cs RegisterContent) reach the
    // RegisterAsset API through this property.
    private BrowserContentManager _browserContent;
    public BrowserContentManager BrowserContent => _browserContent;

    // Convenience for the JS bridge: register a single asset by name.
    public void RegisterAsset(string name, byte[] bytes) =>
        _browserContent?.RegisterAsset(name, bytes);

    // microui renders the browser debug UI in the canvas. Context +
    // renderer are owned by DebugUISystem (browser flavor) — see
    // DebugUISystem.Browser.cs. Game1 just initializes them and ticks
    // the per-frame render in Draw.
#endif

    protected override void Initialize()
    {
        // initialize calls Load Content
        base.Initialize();

        // Pick up an optional content builder the host registered in
        // Game.Services (the template does this in Debug desktop builds) so
        // ContentSystem can build/hot-reload assets in-process. Null otherwise.
        ContentSystem.ResolveContentBuilder(Services);

        ResetFade();

#if !BROWSER
        // Desktop: prefer a project-local Content/FadeSpriteBatchEffect.xnb
        // (compiled from a local FadeSpriteBatchEffect shader) so shader edits
        // hot-reload. Otherwise load the engine shader baked into this assembly
        // at build time, so a packaged consumer with no content pipeline still
        // gets the real Fade sprite effect.
        if (HasLocalContent("FadeSpriteBatchEffect"))
        {
            _customSpriteEffect = ContentWatcher.Watch<Effect>("FadeSpriteBatchEffect");
            _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
        }
        else
        {
            _usingEmbeddedSpriteEffect = true;
            _fadeEffect = new FadeSpriteEffect(LoadEmbeddedEffect("FadeSpriteBatchEffect"));
        }
        _spriteBatch = new SpriteBatch(GraphicsDevice, _fadeEffect);

        _imguiRenderer = new ImGuiRenderer(this);
        _imguiRenderer.RebuildFontAtlas();
#else
        // Browser: load the engine FadeSpriteBatchEffect baked into this
        // assembly (patched for KNI BlazorGL at build time — MGFX v10) so Fade's
        // custom sprite shader works on the web. Previously this fell back to
        // KNI's feature-less built-in SpriteEffect, which silently dropped the
        // per-sprite custom-effect features a bunch of sprite commands rely on.
        var fadeEffect = new FadeSpriteEffect(LoadEmbeddedEffect("FadeSpriteBatchEffect"));
        _spriteBatch = new SpriteBatch(GraphicsDevice, fadeEffect);

        // Browser debug UI flows through DebugRegistry → Playground
        // Tweakpane Inspector (provider-driven, HTML overlay). See
        // Debug/IDebugProvider.cs + the [JSInvokable] DebugListTypes
        // surface in WebRuntime.MonoGame/Pages/Index.razor.cs.
#endif

        // Cross-platform: register IDebugProvider instances so the
        // browser JS bridge (Pages/Index.razor.cs's JSInvokables) can
        // route generic inspector RPC calls — "list sprites", "get
        // sprite 5", "set sprite 5 position.X 42.5" — to the matching
        // game system. Registry replaces by TypeName so hot-reload
        // doesn't leak stale providers.
        Core.Debug.DebugRegistry.Register(new Core.Debug.MetadataDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.SpriteDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.TransformDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.TweenDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.ColliderDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.TextDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.SfxDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.TextureDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.RenderOutputDebugProvider());
        Core.Debug.DebugRegistry.Register(new Core.Debug.EffectDebugProvider());
    }

    private static DateTimeOffset _dbgTime;
    private static void StartTracking() => _dbgTime = DateTimeOffset.Now;

    private static void PrintTracking(string title)
    {
        Console.WriteLine($"{title} took {(DateTimeOffset.Now - _dbgTime).TotalMilliseconds}");
        _dbgTime = DateTimeOffset.Now;
    }

    public void Restart()
    {
        // UnloadContent();
        //
        // LoadContent();
        ResetFade();

    }

    // Public entry point for swapping in a newly-compiled Fade program at
    // runtime (browser hot-reload, desktop test runner). Sets the new
    // ILaunchable and flags a reload — the Update loop picks it up on the
    // next tick, mirroring how F1 reload + `requestReload` flow today.
    public void LoadProgram(ILaunchable program)
    {
        _fadeProgram = program;
        if (program is FadeRuntimeContext ctx)
        {
            GameReloader.SetBuild(ctx);
        }
        _reloadRequestedFromUi = true;
    }

    // Parent-driven state-preserving reload (Playground iframe Reload button).
    // Classifies the supplied build against the LIVE VM right now and, unless
    // it's PermanentlyRude, stashes it for the next frame safepoint to apply
    // live (state preserved). Distinct from LoadProgram, which does a FULL swap
    // (rebuilds the VM, resets state). Returns the verdict; rudeReason is set
    // when the edit can't apply live (the caller should offer a full Run).
    public FadeBasic.Virtual.HotReload.Verdict ArmModuleReload(FadeRuntimeContext ctx, string source, out string rudeReason)
    {
        var verdict = _moduleReloader.ArmAndClassify(ctx, source);
        rudeReason = _moduleReloader.RudeReason;
        Console.WriteLine($"[module-reload:web] arm verdict={verdict} enabled={_moduleReloader.IsEnabled}"
            + (verdict == FadeBasic.Virtual.HotReload.Verdict.PermanentlyRude ? $" rude={rudeReason}" : "")
            + " — commit is driven per-frame via TryCommitPending until it lands");
        if (verdict == FadeBasic.Virtual.HotReload.Verdict.PermanentlyRude)
        {
            // Never applies live — don't leave it pending (no blue bar, no
            // wasted per-frame reclassify). The caller restarts via Run.
            _pendingWebReload = null;
            _webReloadAutoAccept = false;
        }
        else
        {
            _pendingWebReload = ctx;
            _webReloadAutoAccept = true;
        }
        return verdict;
    }

#if BROWSER
    // Flip the game in/out of test mode at runtime. Desktop sets these via
    // ctor + LaunchOptions env vars at boot; the browser doesn't have that
    // option, so we expose a single atomic setter the page calls before
    // invoking a test through MonoGameTestHost.
    //
    //  - `_testMode` controls the Update-loop's dequeue-and-run branch.
    //  - `_options.debug` controls whether the DebugSession arms breakpoint
    //    / pause handling. Tests-with-debug need this set; test-only-run
    //    doesn't, but leaving it on is harmless because no debug client
    //    will issue pauses.
    //  - `suppressExitOnProgramEnd` keeps `_debugSession` alive across
    //    test-VM completions; without it, the first finished test would
    //    SendExitedMessage and drop the debugger.
    //
    // When the page wants to resume normal play, it calls
    // SetTestMode(false) and then LoadProgram(userSource) — ResetFade
    // re-evaluates `_options.debug` against `_testMode`, and the dequeue
    // branch is skipped on each tick.
    public void SetTestMode(bool enabled, bool withDebug = false)
    {
        _testMode = enabled;
        _testModeWithDebug = withDebug;
        if (_options != null)
        {
            // Force-enable debug on opt-in; never force-off — a previously
            // armed Run session shouldn't have its debug surface stripped
            // just because we flipped into test mode.
            if (withDebug) _options.debug = true;
        }
        if (_debugSession != null)
        {
            _debugSession.suppressExitOnProgramEnd = enabled;
        }
    }
#endif

    bool IsNewBuildAvailable()
    {
        return GameReloader.LatestBuild != null && GameReloader.LatestBuild != _fadeProgram;
    }
    
    public void ResetFade(Action<VirtualMachine> customize=null)
    {

        // var x = Content.GetRootDirectoryFullPath();
        // Console.WriteLine("CONTENT DIR: " + x);
        // Console.WriteLine("CURR DIR: " + GameReloader.GetRoot());
        // Environment.Exit(0);
        // _vm = GameReloader.LatestMachine;
        // if (_vm == null)
        var oldVm = _vm;
        {
            _vm = new VirtualMachine(_fadeProgram.Bytecode)
            {
                hostMethods = HostMethodTable.FromCommandCollection(_fadeProgram.CommandCollection)
            };
        }
        customize?.Invoke(_vm);

        // Bind the module reloader to the newly-built VM. Baseline source comes
        // from the running program's SourceMap (desktop runtime context); a
        // no-op if the program can't provide source.
        _moduleReloader.Bind(_vm, _fadeProgram.CommandCollection, _fadeProgram as FadeRuntimeContext);

        // LaunchOptions had a static initializer that allocated a TCP port
        // for DAP — System.Net.Sockets is unavailable in WASM, so touching
        // LaunchOptions crashed the type permanently in browser. The local
        // FadeBasic source (ProjectReferenced from this csproj's browser
        // TFM) wraps the allocation in try/catch, so DefaultOptions stays
        // usable in WASM. We still skip StartServer in browser — there's
        // no socket; the JS-bridged DebugBridge wraps StartDebugging
        // directly instead.
        _options = LaunchOptions.DefaultOptions;

        // Enable debug for normal runs and for test-debug runs.
        // Plain test runs (withDebug=false) stay non-debug so the Execute2
        // path runs without breakpoint overhead.
        if (!_testMode || _testModeWithDebug)
        {
            _options.debug = true;
            _options.debugWaitForConnection = false;
        }

        if (_debugSession != null)
        {
            _debugSession.Restart(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection);
#if BROWSER
            // Restart() resets debuggerSaidHello=0 + debuggerReset=1,
            // which would make the next StartDebugging enter a
            // Thread.Sleep wait loop expecting a TCP-side PROTO_HELLO.
            // Browser has no socket; re-mark connected so the wait
            // short-circuits.
            ((BrowserDebugSession)_debugSession).MarkConnected();
#endif
        }
        else
        {
#if BROWSER
            // Browser uses a subclass that exposes the queues + helpers
            // for the JS bridge (Pages/Index.Debug.cs).
            _debugSession = new BrowserDebugSession(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection,
                _options, "Fade.Mono");
#else
            _debugSession = new DebugSession(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection, _options,
                "Fade.Mono");
#endif
            // Tests run multiple programs through one debug session and rely on
            // Restart() between them; auto-EXITED at end-of-program would drop
            // the debugger before the next test's Restart fires.
            _debugSession.suppressExitOnProgramEnd = _testMode;
#if !BROWSER
            // Desktop: open a real DAP socket. Browser uses JS-bridged
            // tick/drain (Pages/Index.razor.cs's DebugBridge surface) and
            // never opens a socket — no StartServer needed.
            if (_options.debug)
            {
                _debugSession.StartServer();
            }
#endif
        }
#if !BROWSER
        DebugUISystem.debugSession = _debugSession;
        DebugUISystem.commandCollection = _fadeProgram.CommandCollection;
        DebugUISystem.isNewBuildAvailable = IsNewBuildAvailable;
        DebugUISystem.requestReload = () => _reloadRequestedFromUi = true;
#endif

        StartTracking();
        GameSystem.ResetAll();
        PrintTracking("Reset All Systems");

        ContentSystem.BuildContent();
        
        GameSystem.game = this;
        GameSystem.graphicsDeviceManager = _graphics;
        TransformSystem.GetTransformIndex(0, out _, out _); // create a blank index-0 
        RenderSystem.SetMainRenderSize(1920, 1080);
        TextureSystem.GetTextureIndex(0, out var pixelIndex, out var pixelTex);
        pixelTex.descriptor = new TextureDescriptor(); // TODO: maybe add a frame dev?
        pixelTex.SetComputedTexture(_pixel);
        TextureSystem.textures[pixelIndex] = pixelTex;

        FileLog.WriteLine("Finsihed resetting...");
    }
    

    // KNI's Game.OnExiting takes (object, EventArgs); upstream MonoGame
    // ships a (object, ExitingEventArgs) overload. Both compile against the
    // same logical signature when guarded.
#if !BROWSER
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        Content.Dispose();
        // In test mode the debug session's auto-EXITED at end-of-program is
        // suppressed (so individual tests don't drop the debugger), so emit
        // it explicitly now that the whole test run is wrapping up.
        // ShutdownServer below waits for outboundMessages to drain, so the
        // client reliably receives this before the socket closes.
        if (_testMode) _debugSession?.SendExitedMessage();
        _debugSession?.ShutdownServer();
        Console.WriteLine("Game exited...");
        base.OnExiting(sender, args);
    }
#endif
    // Browser: KNI's Game.OnExiting has a different signature than
    // upstream MonoGame's and there's no window to close anyway — JS owns
    // shutdown via the rAF loop returning. Skip the override entirely.

    protected override void LoadContent()
    {
#if !BROWSER
        ContentWatcher = new ContentWatcher(Content);
        ContentWatcher.Init();
#endif

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[]{Color.White});
        GizmoSystem.pixelTexture = _pixel;
    }

    private bool _justReloaded = false;
#if !BROWSER
    private WatchedAsset<Effect> _customSpriteEffect;
    private FadeSpriteEffect _fadeEffect;
    // True when the sprite effect came from the baked-in resource rather than a
    // live-watched local Content/FadeSpriteBatchEffect.xnb — skip hot-reload.
    private bool _usingEmbeddedSpriteEffect;
#endif
    // Holds loaded embedded content (e.g. the baked sprite shader) alive;
    // disposing a ContentManager unloads its assets, so this is never disposed.
    private ContentManager _embeddedContentManager;

#if !BROWSER
    // A project-local Content/<name>.xnb (built from a local shader of the same
    // name) overrides the baked engine copy and is hot-reloaded. ContentWatcher
    // resolves against the title content root (AppContext.BaseDirectory/Content).
    private static bool HasLocalContent(string assetName) =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "Content", assetName + ".xnb"));
#endif

    // Loads an Effect from an XNB baked into this assembly as an embedded
    // resource (LogicalName "<assetName>.xnb", produced by the
    // BakeEngineSpriteEffect build target). Works on desktop and KNI browser.
    private Effect LoadEmbeddedEffect(string assetName)
    {
        var asm = typeof(Game1).Assembly;
        var resourceName = assetName + ".xnb";
        if (asm.GetManifestResourceInfo(resourceName) == null)
        {
            resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n == assetName + ".xnb"
                                  || n.EndsWith("." + assetName + ".xnb", StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"Baked content '{assetName}.xnb' not found in {asm.GetName().Name}. " +
                    "Ensure the BakeEngineSpriteEffect build target ran.");
        }

        var manager = (EmbeddedResourceContentManager)(_embeddedContentManager ??=
            new EmbeddedResourceContentManager(Services, asm));
        manager.Map(assetName, resourceName);
        return manager.Load<Effect>(assetName);
    }

    // ContentManager that serves Load<T>(assetName) from embedded-resource
    // streams instead of files. Never disposed (see _embeddedContentManager).
    private sealed class EmbeddedResourceContentManager : ContentManager
    {
        private readonly Assembly _assembly;
        private readonly System.Collections.Generic.Dictionary<string, string> _resourceByAsset = new();

        public EmbeddedResourceContentManager(IServiceProvider services, Assembly assembly)
            : base(services) => _assembly = assembly;

        public void Map(string assetName, string resourceName) =>
            _resourceByAsset[assetName] = resourceName;

        protected override Stream OpenStream(string assetName)
        {
            if (!_resourceByAsset.TryGetValue(assetName, out var resourceName))
                throw new ContentLoadException($"No embedded resource mapped for asset '{assetName}'.");
            return _assembly.GetManifestResourceStream(resourceName)
                ?? throw new ContentLoadException($"Embedded resource '{resourceName}' missing.");
        }
    }
    private VirtualRuntimeException _fatal;
    private Task<int?> _t;
    private bool _testMode;
    private bool _testModeWithDebug;

    protected override void Update(GameTime gameTime)
    {
#if !BROWSER
        // Escape to quit only makes sense on desktop. Browser closes the tab.
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) )
        {
            Exit();
        }
#endif

        // TODO: need to let the current test run to completion.
        if (_testMode)
        {
            if (currentTest == null)
            {
                if (queuedTests.TryDequeue(out var nextTest))
                {
                    FileLog.WriteLine("DEQUEED TEST");
                    _justReloaded = true;
                    _reloadRequestedFromUi = false;
                    _fadeProgram = nextTest.ctx.Launchable;

                    FileLog.WriteLine("FadeProgram: " + nextTest.ctx.Launchable.Bytecode.Length);
                    currentTest = nextTest;
                    ResetFade(vm =>
                    {
                        vm.instructionIndex = nextTest.ctx.Entry.entryPointAddress;
                        // Without this, ASSERT_FAIL ops take the
                        // "main-program execution" branch in
                        // VirtualMachine.Execute2 (line 1436+), which
                        // treats the assert as a hard runtime error and
                        // *never sets* vm.assertionFailure — the result
                        // collector below then reports the test as
                        // passed. The test-execution branch (line 1408+)
                        // populates vm.assertionFailure with the
                        // sourceText / reason / call-stack we need.
                        vm.isTestExecution = true;
                    });
                    return;
                }
                if (allTestsDone)
                {
                    Quit();
                    return;
                }
                // wait for a test to start. 
                return;
            }
            else
            {
                // is the current test cancelled?
                if (currentTest.ct.IsCancellationRequested)
                {
                    FileLog.WriteLine("TRYING TO CANCEL EARLY");
                    currentTest.source.TrySetCanceled(currentTest.ct);
                    currentTest = null;
                    return;
                }
            }
        }
        
   
        var f1Down = Keyboard.GetState().IsKeyDown(Keys.F1);
        if ((!_justReloaded && f1Down) || _reloadRequestedFromUi)
        {
            if (GameReloader.LatestBuild != null)
            {
                _fadeProgram = GameReloader.LatestBuild;
            }

            _justReloaded = true;
            _reloadRequestedFromUi = false;

            Restart();
            return;
        }

        if (_justReloaded && !f1Down)
        {
            _fatal = null;
            _justReloaded = false;
        }


        GameSystem.currentFrameNumber++;
        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

#if !BROWSER
        // when ImGui is capturing keyboard/mouse, feed blank state to the game's InputSystem
        var io = ImGui.GetIO();
        if (io.WantCaptureKeyboard)
            keyState = default;
        if (io.WantCaptureMouse)
            mouseState = default;
#endif

        InputSystem.ApplyNewMouse(ref mouseState, ref keyState);

        TweenSystem.currentTime = AudioInstanceSystem.currentTime = gameTime.TotalGameTime.TotalMilliseconds;
        TweenSystem.ProcessTweens();
        AudioInstanceSystem.HandleAudio();
        TextureSystem.RefreshTextures();

        // RefreshEffects branches internally — the !BROWSER side runs
        // ContentWatcher.TryRefreshAsset against the file-system watcher;
        // the BROWSER side drains BrowserContentManager.ConsumeReloadedAssets
        // and re-Content.Load's any user effect whose source the playground
        // just re-pushed. Both need to fire every frame, so the call lives
        // outside the `#if !BROWSER` block. Previously it was wrapped on
        // browser too, which meant the reloaded-asset set never drained:
        // BrowserContentManager.UnregisterAsset had already disposed the
        // old Effect instance via reflection-eviction, but the RuntimeEffect
        // slot still pointed at that disposed reference — every subsequent
        // frame's SpriteBatch.End() threw ObjectDisposedException('effect').
        //
        // The `fadeFx` parameter is currently unused inside RefreshEffects,
        // and `_fadeEffect` itself is desktop-only (wrapped in
        // `#if !BROWSER`), so the browser call passes null — kept as a
        // parameter for now in case the desktop path needs it later.
#if !BROWSER
        RenderSystem.RefreshEffects(_fadeEffect);

        if (!_usingEmbeddedSpriteEffect && ContentWatcher.TryRefreshAsset(ref _customSpriteEffect))
        {
            _fadeEffect?.Dispose(); // get rid of old one?
            _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
            _spriteBatch.ResetEffect(_fadeEffect);
        }
#else
        RenderSystem.RefreshEffects(null);
#endif

        GameSystem.latestTime = gameTime;
#if !BROWSER
        DebugUISystem.renderer = _imguiRenderer;
#endif
        if (_vm.instructionIndex >= _vm.program.Length)
        {
            // if we are testing, then mark the current test as complete. 
            if (currentTest != null)
            {
                FileLog.WriteLine("CURRENT TEST IS NOW OVER");
               
                currentTest.result.passed = true; // TODO;

                if (_vm.assertionFailure != null)
                {
                    currentTest.result.passed = false;
                    
                    currentTest.result.failureInstructionIndex = _vm.assertionFailure.instructionIndex;
                    currentTest.result.failureSourceText = _vm.assertionFailure.sourceText;
                    currentTest.result.failureMessage = "Failed Assert: " + _vm.assertionFailure.sourceText;
                    var map = new IndexCollection(_fadeProgram.DebugData.statementTokens);
                    if (map.TryFindClosestTokenBeforeIndex(_vm.assertionFailure.instructionIndex, out var token))
                    {
                        var runtime = GameReloader.LatestRuntime;
                        if (runtime?.SourceMap != null)
                        {
                            var local = runtime.SourceMap.GetOriginalLocation(token.token);
                            currentTest.result.failureMessage += Environment.NewLine + "\t" +
                                                                 $"source location: {local.fileName} - {local.startLine}:{local.startChar}";
                        }
                    }

                }
                
                currentTest.source.SetResult();
                currentTest = null;
                return; // loop around
            }
            else
            {
                Quit();
            }
        }

        
        { // handle debug ui drawing...
            // GraphicsDevice.SetRenderTarget(RenderSystem.dbgBuffer);
            // GraphicsDevice.Clear(Color.Transparent);

            // Browser uses the same queue+state surface as desktop — only
            // Render() (ImGui rendering) is desktop-only. See
            // DebugUISystem.Browser.cs; EndDebug there serializes the queue
            // and posts to the parent Playground for Tweakpane rendering.
            DebugUISystem.StartDebug();
        }
        
        if (_fatal == null)
        {


            try
            {
                // Both desktop and browser route through DebugSession now.
                // _options.debug is true unless in test mode. Browser pulls
                // the JS-bridged debug surface in Pages/Index.razor.cs;
                // desktop has its DAP socket. The StartDebugging path
                // handles breakpoints, pause, step, and ticking when no
                // debug state is active (just keeps ticking).
                if (_options.debug)
                {
                    _debugSession._vm.isSuspendRequested = false;
                    while (!_debugSession._vm.isSuspendRequested && _vm.instructionIndex < _vm.program.Length)
                    {
#if BROWSER
                        // Budgeted call so the outer while inside StartDebugging
                        // *always* returns within ~1000 spins, even while it's
                        // spin-waiting on a paused state. Without a budget,
                        // StartDebugging spins forever calling ReadMessage —
                        // and on WASM's single thread, JS can't deliver the
                        // REQUEST_PLAY/STEP message that would resume it.
                        _debugSession.StartDebugging(1000);
                        // When the session is paused (hit breakpoint, manual
                        // pause, step landed), break so the rAF tick returns
                        // to JS — the next frame's TickDotNet will re-enter
                        // and pick up any inbound debug messages. Desktop
                        // doesn't need this because StartDebugging sits on
                        // a thread that receives socket data concurrently.
                        if (_debugSession.IsPaused) break;
#else
                        _debugSession.StartDebugging();
#endif
                    }
                }
                else
                {
#if BROWSER
                    // Cooperative-pump integration: prompt$ and wait ms
                    // route through FadeBasic.Sdk.CooperativePump
                    // (wired by WebRuntime.MonoGame's Index page). Skip
                    // the per-frame VM tick while the pump is waiting
                    // on a host reply or a wait-ms deadline. The canvas
                    // keeps rendering the last frame either way.
                    if (FadeBasic.Sdk.CooperativePump.IsBusyWaiting())
                    {
                        // Skip this frame's VM tick — but do not return,
                        // so transform/render systems below still run.
                    }
                    else
#endif
                    {
                        _vm.isSuspendRequested = false;
                        while (!_vm.isSuspendRequested && _vm.instructionIndex < _vm.program.Length)
                        {
                            _vm.Execute2();
                        }
                    }
                }

                // Frame safepoint — runs after EITHER the debug or non-debug tick.
                // (The fish game runs with _options.debug = true, so the module
                // reload MUST live outside the non-debug branch.) Arm/classify a
                // newer build; F2 applies the state-preserving reload live.
                {
                    var f2Now = Keyboard.GetState().IsKeyDown(Keys.F2);
                    var reloadCommitted = false;
                    // A parent-armed web reload (Playground) is driven EVERY frame
                    // via TryCommitPending — it re-classifies + applies as soon as
                    // the VM leaves the edited statement (drains like the web path),
                    // instead of relying on SyncPoint's one-shot arm-time verdict.
                    if (_webReloadAutoAccept && _pendingWebReload != null)
                    {
                        if (_moduleReloader.TryCommitPending())
                        {
                            // Committed live — realign the "current program" AND
                            // GameReloader's latest-build pointers to the reloaded
                            // build. Without syncing GameReloader, IsNewBuildAvailable()
                            // stays true (LatestBuild != _fadeProgram) → a spurious RED
                            // "press F1 to restart" border, and the F2 fallback branch
                            // below would re-arm the pre-reload build.
                            _fadeProgram = _pendingWebReload;
                            GameReloader.SetBuild(_pendingWebReload);
                            _pendingWebReload = null;
                            _webReloadAutoAccept = false;
                            reloadCommitted = true;
                        }
                        else if (!_moduleReloader.HasPendingReload)
                        {
                            // Resolved without a commit (no-op diff / dropped) —
                            // stop retrying every frame.
                            _pendingWebReload = null;
                            _webReloadAutoAccept = false;
                        }
                    }
                    // F2 file-watcher path (desktop / standalone) — unchanged.
                    else if (_moduleReloader.SyncPoint(GameReloader.LatestRuntime, f2Now && !_f2WasDown)
                             && GameReloader.LatestBuild != null)
                    {
                        _fadeProgram = GameReloader.LatestBuild;
                        reloadCommitted = true;
                    }
                    _f2WasDown = f2Now;

                    // Keep an attached debugger alive across the reload: rebind it
                    // to the new program on the SAME (reloaded-in-place) VM, so
                    // state is preserved and breakpoints re-verify — the exact
                    // Restart handshake F1 uses (see ResetFade), minus the VM
                    // rebuild. REV_REQUEST_RESTART drives the client to re-send
                    // breakpoints; MarkConnected clears the re-HELLO gate in-browser.
                    if (reloadCommitted && _options.debug && _debugSession != null)
                    {
                        var newDbg = _moduleReloader.CurrentDebugData ?? _fadeProgram.DebugData;
#if BROWSER
                        // Browser (Playground): rebind AND preserve the paused state
                        // so an accepted reload doesn't resume a paused program.
                        ((BrowserDebugSession)_debugSession).RestartPreservingPause(_vm, newDbg, _fadeProgram.CommandCollection);
#else
                        // Desktop: F2 reload is used while running; plain Restart
                        // (resets to running) is fine. Pause-preservation on desktop
                        // would need RestartAfterReload from a newer Lang.Core.
                        _debugSession.Restart(_vm, newDbg, _fadeProgram.CommandCollection);
#endif
                        Console.WriteLine("[module-reload] debug session rebound (attached, breakpoints re-verifying)");
                    }
                }
            }
            catch (VirtualRuntimeException ex)
            {
                var map = new IndexCollection(_fadeProgram.DebugData.statementTokens);
                if (map.TryFindClosestTokenBeforeIndex(ex.Error.insIndex, out var token))
                {
                    if (GameReloader.LatestRuntime != null)
                    {
                        var local = GameReloader.LatestRuntime.SourceMap.GetOriginalLocation(token.token);
                        Console.Error.WriteLine(
                            $"source location: {local.fileName} - {local.startLine+1}:{local.startChar}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"need to reload with source for exact area. raw={token.token.raw}");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unknown source location");
                }
                Console.Error.WriteLine(ex.Message);
                _fatal = ex;
            }
        }


        DebugUISystem.EndDebug();
        TransformSystem.CalculateTransforms();

        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_vm.instructionIndex <= 4) return; // the VM hasn't started yet; so don't draw anything...

        // the final image will be stored in the mainBuffer...
        // RenderSystem.RenderAllStages(_spriteBatch);
        RenderSystem.RenderAll2(_spriteBatch);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        var screenEffect = RenderSystem.screenEffectIndex > -1
            ? RenderSystem.effects[RenderSystem.screenEffectIndex].effect
            : null;
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Immediate,
            blendState: BlendState.Opaque,
            samplerState: SamplerState.PointClamp,
            effect: screenEffect
            );
        
        _spriteBatch.Draw(RenderSystem.mainBuffer, RenderSystem.mainBufferPosition, null, Color.White, 0f, Vector2.Zero, RenderSystem.mainBufferScale, SpriteEffects.None, 0);
        _spriteBatch.End();

        // Gizmos draw in world-space (mainBuffer pixels) but to the
        // back-buffer after the screen-effect composite, so debug overlays
        // aren't part of the post-process and always render on top.
        GizmoSystem.Render(_spriteBatch, RenderSystem.mainBufferPosition, RenderSystem.mainBufferScale);

        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
#if !BROWSER
        DebugUISystem.Render();
#endif
#if !BROWSER
        // Blue border = a state-preserving module reload is ready (press F2).
        // Red border  = a new build is available but can't apply live (rude edit
        //               / not source-carrying) — press F1 for a full restart.
        // Desktop/standalone only: these are F1/F2 key affordances. In the
        // browser (Playground) reload is driven by the editor's Reload button —
        // the border would just be meaningless chrome around the canvas.
        Color? borderColor =
            _moduleReloader.IsModuleReloadReady ? Color.Blue :
            IsNewBuildAvailable() ? Color.Red :
            (Color?)null;
        if (borderColor.HasValue)
        {
            var c = borderColor.Value;
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, 20), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 20, 20, GraphicsDevice.Viewport.Height - 40), c);
            _spriteBatch.Draw(_pixel, new Rectangle(GraphicsDevice.Viewport.Width - 20, 20, 20, GraphicsDevice.Viewport.Height - 40), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, GraphicsDevice.Viewport.Height - 20, GraphicsDevice.Viewport.Width, 20), c);
        }
#endif

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    public void Quit()
    {
        FileLog.WriteLine("TRYING TO QUIT");
#if !BROWSER
        Exit();
#endif
        // Browser: there's no window to close. The JS rAF loop owns the
        // game lifecycle — leaving the VM idle is enough; the next
        // LoadProgram call will swap in new bytecode.
    }

}