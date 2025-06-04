using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Game;

public static class InputSystem
{
    public static KeyboardState keyboardState, oldKeyboardState;
    public static MouseState mouseState, oldMouseState;

    public static void Reset()
    {
        keyboardState = default;
        oldKeyboardState = default;
        mouseState = default;
        oldMouseState = default;
    }
    
    public static void ApplyNewMouse(ref MouseState next, ref KeyboardState nextKeyboard)
    {
        oldMouseState = mouseState;
        mouseState = next;

        oldKeyboardState = keyboardState;
        keyboardState = nextKeyboard;
    }
}