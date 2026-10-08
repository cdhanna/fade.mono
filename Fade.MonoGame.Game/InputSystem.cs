using System;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Core;

public static class InputSystem
{
    public const int DEVICE_KEYBOARD = 0;
    public const int DEVICE_GAMEPAD = 1;

    // How far a stick or a trigger has to travel before it counts as a pressed button.
    public const float PAD_AXIS_THRESHOLD = .5f;

    public static KeyboardState keyboardState, oldKeyboardState;
    public static MouseState mouseState, oldMouseState;
    public static GamePadState gamePadState, oldGamePadState;

    // The device that most recently had something pressed on it. A controller that is plugged in
    // but sitting on the desk does not count, so this is what on-screen prompts should follow.
    public static int lastDevice, oldLastDevice;

    // Whether pressing escape closes the window. It does until a game says otherwise, because
    // that is the quickest way out of a program that is still being written.
    public static bool escapeQuits = true;

    // Every button that counts as "the player touched the controller".
    private static readonly Buttons[] _padButtons =
    {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
        Buttons.Back, Buttons.Start, Buttons.BigButton,
        Buttons.LeftShoulder, Buttons.RightShoulder,
        Buttons.LeftTrigger, Buttons.RightTrigger,
        Buttons.LeftStick, Buttons.RightStick,
        Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight,
        Buttons.LeftThumbstickUp, Buttons.LeftThumbstickDown, Buttons.LeftThumbstickLeft, Buttons.LeftThumbstickRight,
        Buttons.RightThumbstickUp, Buttons.RightThumbstickDown, Buttons.RightThumbstickLeft, Buttons.RightThumbstickRight,
    };

    public static void Reset()
    {
        keyboardState = default;
        oldKeyboardState = default;
        mouseState = default;
        oldMouseState = default;
        gamePadState = default;
        oldGamePadState = default;
        lastDevice = DEVICE_KEYBOARD;
        oldLastDevice = DEVICE_KEYBOARD;
        escapeQuits = true;
    }

    public static void ApplyNewMouse(ref MouseState next, ref KeyboardState nextKeyboard)
    {
        oldMouseState = mouseState;
        mouseState = next;

        oldKeyboardState = keyboardState;
        keyboardState = nextKeyboard;
    }

    /// <summary>
    /// Call once per frame, after <see cref="ApplyNewMouse"/>, because this is also where the last
    /// used device gets worked out from all three states.
    /// </summary>
    public static void ApplyNewGamePad(ref GamePadState next)
    {
        oldGamePadState = gamePadState;
        gamePadState = next;

        oldLastDevice = lastDevice;

        // Only a NEW press moves the device over. Otherwise holding a key while tapping a button
        // would flip to the controller and straight back again on the next frame.
        if (IsNewPadActivity())
        {
            lastDevice = DEVICE_GAMEPAD;
        }
        else if (IsNewKeyboardActivity() || IsNewMouseActivity())
        {
            lastDevice = DEVICE_KEYBOARD;
        }
    }

    /// <summary>
    /// The same as <see cref="GamePadState.IsButtonDown"/>, except that the stick directions and the
    /// triggers use <see cref="PAD_AXIS_THRESHOLD"/> instead of whatever the platform decided.
    /// </summary>
    public static bool IsPadButtonDown(ref GamePadState state, Buttons button)
    {
        switch (button)
        {
            case Buttons.LeftTrigger: return state.Triggers.Left > PAD_AXIS_THRESHOLD;
            case Buttons.RightTrigger: return state.Triggers.Right > PAD_AXIS_THRESHOLD;

            // A stick's Y axis points up.
            case Buttons.LeftThumbstickUp: return IsStickPushed(state.ThumbSticks.Left.Y, state.ThumbSticks.Left.X);
            case Buttons.LeftThumbstickDown: return IsStickPushed(-state.ThumbSticks.Left.Y, state.ThumbSticks.Left.X);
            case Buttons.LeftThumbstickLeft: return IsStickPushed(-state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y);
            case Buttons.LeftThumbstickRight: return IsStickPushed(state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y);
            case Buttons.RightThumbstickUp: return IsStickPushed(state.ThumbSticks.Right.Y, state.ThumbSticks.Right.X);
            case Buttons.RightThumbstickDown: return IsStickPushed(-state.ThumbSticks.Right.Y, state.ThumbSticks.Right.X);
            case Buttons.RightThumbstickLeft: return IsStickPushed(-state.ThumbSticks.Right.X, state.ThumbSticks.Right.Y);
            case Buttons.RightThumbstickRight: return IsStickPushed(state.ThumbSticks.Right.X, state.ThumbSticks.Right.Y);

            default: return state.IsButtonDown(button);
        }
    }

    // A stick is only ever pushed in ONE direction, the one it leans toward the most. If the two
    // axes were judged on their own, a sloppy push to the right would also press up a few frames
    // later, and anything that steps once per press would step twice.
    private static bool IsStickPushed(float along, float across)
    {
        return along > PAD_AXIS_THRESHOLD && along >= Math.Abs(across);
    }

    private static bool IsNewPadActivity()
    {
        for (var i = 0; i < _padButtons.Length; i++)
        {
            if (IsPadButtonDown(ref gamePadState, _padButtons[i])
                && !IsPadButtonDown(ref oldGamePadState, _padButtons[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNewKeyboardActivity()
    {
        return keyboardState != oldKeyboardState && keyboardState.GetPressedKeyCount() > 0;
    }

    // Moving the mouse does not count. A bump of the desk should not take the prompts away from
    // someone who is holding a controller.
    private static bool IsNewMouseActivity()
    {
        return (mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released)
               || (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released)
               || (mouseState.MiddleButton == ButtonState.Pressed && oldMouseState.MiddleButton == ButtonState.Released);
    }
}
