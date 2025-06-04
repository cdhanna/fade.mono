using System;
using FadeBasic.Launch;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Extra;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using Microsoft.Xna.Framework.Input;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.Fade.SpriteBatch;

namespace Fade.MonoGame.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly ILaunchable _fadeProgram;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private VirtualMachine _vm;
    private DebugSession _debugSession;
    private LaunchOptions _options;
    public ContentWatcher ContentWatcher;

    private Texture2D _pixel;
    private SpriteFont _defaultFont;
    private Func<bool> _isNewBuildReady;
    private bool _autoAcceptNewBuilds;

    public Game1(ILaunchable fadeProgram, Func<bool> isNewBuildReady, bool autoAcceptNewBuilds=false)
    {
        _autoAcceptNewBuilds = autoAcceptNewBuilds;
        _isNewBuildReady = isNewBuildReady;
        _fadeProgram = fadeProgram;
        _vm = new VirtualMachine(_fadeProgram.Bytecode)
        {
            hostMethods = HostMethodTable.FromCommandCollection(_fadeProgram.CommandCollection)
        };
        _options = LaunchOptions.DefaultOptions;
        if (_options.debug)
        {
            _debugSession = new DebugSession(_vm, _fadeProgram.DebugData, _fadeProgram.CommandCollection, _options,
                "Fade.Mono");
            _debugSession.StartServer();
        }
        
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GameSystem.game = this;
        GameSystem.graphicsDeviceManager = _graphics;
        base.Initialize();
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
        
        
        var customSpriteEffect = Content.Load<Effect>("FadeSpriteBatchEffect");
        _spriteBatch = new SpriteBatch(GraphicsDevice, new FadeSpriteEffect(customSpriteEffect));

// https://www.youtube.com/watch?v=-5ELPrIJNvA TARGET RESOLUTION 

        //_testFx = ContentWatcher.Watch<Effect>("Fish/Shaders/ScreenEffect");

        
        
        _defaultFont = Content.Load<SpriteFont>("MyFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[]{Color.White});

        TransformSystem.GetTransformIndex(0, out _, out _); // create a blank index-0 

        RenderSystem.SetMainRenderSize(1920, 1080);
        RenderSystem.GetStageIndex(1, out _, out var stage);

        TextureSystem.GetTextureIndex(0, out var pixelIndex, out var pixelTex);
        pixelTex.descriptor = new TextureDescriptor(); // TODO: maybe add a frame dev?
        pixelTex.texture = _pixel;
        TextureSystem.textures[pixelIndex] = pixelTex;
        
        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape) )
        {
            Exit();
        }

        if (_autoAcceptNewBuilds && _isNewBuildReady())
        {
            Exit();
        }

        // if (ContentWatcher.TryRefreshAsset(ref _testFx))
        // {
        //     Console.WriteLine("fx changed!");
        // }

        GameSystem.currentFrameNumber++;
        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        InputSystem.ApplyNewMouse(ref mouseState, ref keyState);

        TweenSystem.currentTime = AudioInstanceSystem.currentTime = gameTime.TotalGameTime.TotalMilliseconds;
        TweenSystem.ProcessTweens();
        AudioInstanceSystem.HandleAudio();
        RenderSystem.RefreshEffects();
        GameSystem.latestTime = gameTime;
        
        if (_vm.instructionIndex >= _vm.program.Length)
        {
            Exit();
        }
        

        if (_options.debug)
        {
            // TODO: need to make debugger respond to 
            _debugSession._vm.isSuspendRequested = false;
            while (!_debugSession._vm.isSuspendRequested  && _vm.instructionIndex < _vm.program.Length)
            {
                _debugSession.StartDebugging(1);
            }
            
        }
        else
        {
            _vm.isSuspendRequested = false;
            while (!_vm.isSuspendRequested && _vm.instructionIndex < _vm.program.Length)
            {
                _vm.Execute2(1);
            }
        }

        TransformSystem.CalculateTransforms();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        
        // the final image will be stored in the mainBuffer...
        RenderSystem.RenderAllStages(_spriteBatch);

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


        if (_isNewBuildReady())
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