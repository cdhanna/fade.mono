using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    
    [FadeBasicCommand("mouse x")]
    public static int GetMouseX()
    {
        var v = (float)InputSystem.mouseState.X;

        v -= RenderSystem.mainBufferPosition.X;

        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferWidth - RenderSystem.mainBufferPosition.X*2;

        v *= RenderSystem.mainBuffer.Width;

        return (int)v;

    }

    [FadeBasicCommand("mouse y")]
    public static int GetMouseY()
    {
        var v = (float)InputSystem.mouseState.Y;

        v -= RenderSystem.mainBufferPosition.Y;

        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferHeight - RenderSystem.mainBufferPosition.Y*2;

        v *= RenderSystem.mainBuffer.Height;

        return (int)v;
    }

    [FadeBasicCommand("left click")]
    public static bool IsLeftMouse()
    {
        return InputSystem.mouseState.LeftButton == ButtonState.Pressed;
    }
    
    [FadeBasicCommand("new left click")]
    public static bool IsNewLeftMouse()
    {
        return InputSystem.mouseState.LeftButton == ButtonState.Pressed && InputSystem.oldMouseState.LeftButton == ButtonState.Released;
    }
    
    [FadeBasicCommand("right click")]
    public static bool IsRightMouse()
    {
        return InputSystem.mouseState.RightButton == ButtonState.Pressed;
    }
    
    [FadeBasicCommand("upkey")]
    public static int upKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Up) ? 1 : 0;
    }
    [FadeBasicCommand("downkey")]
    public static int downKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Down) ? 1 : 0;
    }
    [FadeBasicCommand("rightKey")]
    public static int rightKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Right) ? 1 : 0;
    }
    [FadeBasicCommand("leftKey")]
    public static int leftKey()
    {
        if (InputSystem.keyboardState.IsKeyDown(Keys.Left))
        {
            
        }
        return InputSystem.keyboardState.IsKeyDown(Keys.Left) ? 1 : 0;
    }
    [FadeBasicCommand("spaceKey")]
    public static int spaceKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Space) ? 1 : 0;
    }

    
    
    [FadeBasicCommand("new upkey")]
    public static bool upKeyNew()
    {
        return IsNewKeyPressed((int)Keys.Up);
    }
    
    [FadeBasicCommand("new downkey")]
    public static bool downKeyNew()
    {        return IsNewKeyPressed((int)Keys.Down);

    }
    [FadeBasicCommand("new rightKey")]
    public static bool rightKeyNew()
    {        return IsNewKeyPressed((int)Keys.Right);

    }
    [FadeBasicCommand("new leftKey")]
    public static bool leftKeyNew()
    {        return IsNewKeyPressed((int)Keys.Left);

    }
    [FadeBasicCommand("new spaceKey")]
    public static bool spaceKeyNew()
    {        return IsNewKeyPressed((int)Keys.Space);

    }
    
    [FadeBasicCommand("new key down")]
    public static bool IsNewKeyPressed(int scanCode)
    {
        var keyDown = !InputSystem.oldKeyboardState.IsKeyDown((Keys)scanCode) && InputSystem.keyboardState.IsKeyDown((Keys)scanCode);
        return keyDown;
    }
    
    [FadeBasicCommand("key down")]
    public static bool IsKeyPressed(int scanCode)
    {
        var keyDown = InputSystem.keyboardState.IsKeyDown((Keys)scanCode);
        return keyDown;
    }
    
    [FadeBasicCommand("scanCode")]
    public static int ScanCode(string key)
    {
        var code = Enum.Parse<Keys>(key);
        return (int)code;
    }
    
}