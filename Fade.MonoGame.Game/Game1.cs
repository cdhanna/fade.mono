using System;
using System.Diagnostics;
using System.Threading.Tasks;

using FadeBasic.Launch;
using FadeBasic.Sdk;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

using Mouse = Microsoft.Xna.Framework.Input.Mouse;

namespace Fade.MonoGame.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private ILaunchable _fadeProgram;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private VirtualMachine _vm;
    private DebugSession _debugSession;
    private LaunchOptions _options;
    public ContentWatcher ContentWatcher;

    private Texture2D _pixel;
    private bool _autoAcceptNewBuilds;

 
    public Game1(ILaunchable fadeProgram, bool autoAcceptNewBuilds=false)
    {
        _autoAcceptNewBuilds = autoAcceptNewBuilds;
        _fadeProgram = fadeProgram;

        
        
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // initialize calls Load Content
        base.Initialize();
        
        ResetFade();
        
        
        _customSpriteEffect = ContentWatcher.Watch<Effect>("FadeSpriteBatchEffect");
        _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
        _spriteBatch = new SpriteBatch(GraphicsDevice, _fadeEffect);

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

    bool IsNewBuildAvailable()
    {
        return GameReloader.LatestBuild != null && GameReloader.LatestBuild != _fadeProgram;
    }
    
    public void ResetFade()
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

        _options = LaunchOptions.DefaultOptions;
        if (_options.debug)
        {
            if (_debugSession != null)
            {
                _debugSession.Restart(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection);
            }
            else
            {
                _debugSession = new DebugSession(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection, _options,
                    "Fade.Mono");
                _debugSession.StartServer();
            }
            
        }
        
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

    }
    

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        Content.Dispose();
        _debugSession?.ShutdownServer();
        Console.WriteLine("Game exited...");
        base.OnExiting(sender, args);
    }

    // protected override void OnExiting(object sender, EventArgs args)
    // {
    // }

    protected override void LoadContent()
    {
        ContentWatcher = new ContentWatcher(Content);
        ContentWatcher.Init();
        
        
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[]{Color.White});

      
        // TODO: use this.Content to load your game content here
    }

    private bool _justReloaded = false;
    private WatchedAsset<Effect> _customSpriteEffect;
    private FadeSpriteEffect _fadeEffect;
    private VirtualRuntimeException _fatal;
    private Task<int?> _t;

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) )
        {
            Exit();
        }

        
   
        if (!_justReloaded && Keyboard.GetState().IsKeyDown(Keys.F1))
        {
            if (GameReloader.LatestBuild != null)
            {
                _fadeProgram = GameReloader.LatestBuild;
            }

            _justReloaded = true;

            Restart();
            return;
        }

        if (_justReloaded && Keyboard.GetState().IsKeyUp(Keys.F1))
        {
            _fatal = null;
            _justReloaded = false;
        }


        GameSystem.currentFrameNumber++;
        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        InputSystem.ApplyNewMouse(ref mouseState, ref keyState);

        TweenSystem.currentTime = AudioInstanceSystem.currentTime = gameTime.TotalGameTime.TotalMilliseconds;
        TweenSystem.ProcessTweens();
        AudioInstanceSystem.HandleAudio();
        TextureSystem.RefreshTextures();
        RenderSystem.RefreshEffects(_fadeEffect);

        if (ContentWatcher.TryRefreshAsset(ref _customSpriteEffect))
        {
            _fadeEffect?.Dispose(); // get rid of old one?
            _fadeEffect = new FadeSpriteEffect(_customSpriteEffect.Asset);
            _spriteBatch.ResetEffect(_fadeEffect);
        }
        
        GameSystem.latestTime = gameTime;
        if (_vm.instructionIndex >= _vm.program.Length)
        {
            Exit();
        }

        if (_fatal == null)
        {


            try
            {
                if (_options.debug)
                {
                    _debugSession._vm.isSuspendRequested = false;
                    while (!_debugSession._vm.isSuspendRequested && _vm.instructionIndex < _vm.program.Length)
                    {
                        _debugSession.StartDebugging();
                    }
                }
                else
                {
                    _vm.isSuspendRequested = false;
                    while (!_vm.isSuspendRequested && _vm.instructionIndex < _vm.program.Length)
                    {
                        _vm.Execute2();
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
        Exit();
    }

}