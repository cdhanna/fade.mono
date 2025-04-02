using System;
using FadeBasic.Launch;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    protected override void OnExiting(object sender, EventArgs args)
    {
        _debugSession?.ShutdownServer();
        base.OnExiting(sender, args);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[]{Color.White});
        
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
        InputSystem.keyboardState = keyState;
        if (keyState.IsKeyDown(Keys.Left))
        {
            
        }
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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // TODO: Add your drawing code here

        _spriteBatch.Begin();
        SpriteSystem.DrawSprites(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}