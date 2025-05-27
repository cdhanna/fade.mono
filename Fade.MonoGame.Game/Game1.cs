using System;
using FadeBasic.Launch;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;
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

    private Texture2D _pixel;
    private SpriteFont _defaultFont;

    public Game1(ILaunchable fadeProgram)
    {
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
        base.OnExiting(sender, args);
        
        _debugSession?.ShutdownServer();
        base.OnExiting(sender, args);
    }

    // protected override void OnExiting(object sender, EventArgs args)
    // {
    // }

    protected override void LoadContent()
    {

        var customSpriteEffect = Content.Load<Effect>("FadeSpriteBatchEffect");
        _spriteBatch = new SpriteBatch(GraphicsDevice, new FadeSpriteEffect(customSpriteEffect));

// https://www.youtube.com/watch?v=-5ELPrIJNvA TARGET RESOLUTION 


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
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        InputSystem.ApplyNewMouse(ref mouseState, ref keyState);

        TweenSystem.currentTime = gameTime.TotalGameTime.TotalMilliseconds;
        TweenSystem.ProcessTweens();

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
        
        // _spriteBatch.Begin();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(RenderSystem.mainBuffer, RenderSystem.mainBufferPosition, null, Color.White, 0f, Vector2.Zero, RenderSystem.mainBufferScale, SpriteEffects.None, 0);

        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
    
}