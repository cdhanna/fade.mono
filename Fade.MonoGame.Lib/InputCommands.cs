using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
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
}