using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        ResetFade();

#if !BROWSER
        // Desktop: live-watch the bundled FadeSpriteBatchEffect.fx so shader
        // edits hot-reload. Browser uses KNI's built-in SpriteEffect — see
        // the #else branch below.
        _customSpriteEffect = ContentWatcher.Watch<Effect>("FadeSpriteBatchEffect");
        _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
        _spriteBatch = new SpriteBatch(GraphicsDevice, _fadeEffect);

        _imguiRenderer = new ImGuiRenderer(this);
        _imguiRenderer.RebuildFontAtlas();
#else
        // Browser: KNI ships its own default sprite shader (`SpriteEffect`)
        // for stock SpriteBatch. We re-use that bytecode by wrapping it in
        // FadeSpriteEffect, which only needs an Effect with a
        // "MatrixTransform" parameter — which SpriteEffect has. FadeSpriteBatch
        // can then render normally with the default vertex transform +
        // texture sample, just no Fade per-sprite custom-effect features
        // until Phase 3 lands the dxc + spirv-cross shader pipeline.
        var defaultSpriteEffect = new SpriteEffect(GraphicsDevice);
        var fadeEffect = new FadeSpriteEffect(defaultSpriteEffect);
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
#endif
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

#if !BROWSER
        RenderSystem.RefreshEffects(_fadeEffect);

        if (ContentWatcher.TryRefreshAsset(ref _customSpriteEffect))
        {
            _fadeEffect?.Dispose(); // get rid of old one?
            _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
            _spriteBatch.ResetEffect(_fadeEffect);
        }
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
        if (IsNewBuildAvailable())
        {
            // a silly indicator that a new build is ready
            _spriteBatch.Draw(_pixel, Vector2.Zero, null, Color.Red, 0f, Vector2.Zero, 20, SpriteEffects.None, 0);
            
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, 20), Color.Red);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 20, 20, GraphicsDevice.Viewport.Height - 40), Color.Red);
            _spriteBatch.Draw(_pixel, new Rectangle(GraphicsDevice.Viewport.Width - 20, 20, 20, GraphicsDevice.Viewport.Height - 40), Color.Red);
            _spriteBatch.Draw(_pixel, new Rectangle(0, GraphicsDevice.Viewport.Height - 20, GraphicsDevice.Viewport.Width, 20), Color.Red);
        }
        
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