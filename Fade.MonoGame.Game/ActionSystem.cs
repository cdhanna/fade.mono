using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Core;

/// <summary>
/// One physical input that triggers an action: a keyboard key, or a controller button.
/// </summary>
public struct ActionBinding
{
    public bool isPad;

    // A Keys value, or a Buttons value when isPad is set.
    public int code;
}

/// <summary>
/// Something the player can do, like "jump", separate from whichever keys and buttons do it.
/// </summary>
public class InputAction
{
    public int id;
    public List<ActionBinding> bindings = new List<ActionBinding>();
}

public static class ActionSystem
{
    public static List<InputAction> actions = new List<InputAction>();
    private static Dictionary<int, int> _actionMap = new Dictionary<int, int>();
    public static int highestActionId;

    public static void Reset()
    {
        actions.Clear();
        _actionMap.Clear();
        highestActionId = 0;
    }

    public static void GetActionIndex(int actionId, out int index, out InputAction action)
    {
        if (!_actionMap.TryGetValue(actionId, out index))
        {
            highestActionId = actionId > highestActionId ? actionId : highestActionId;
            index = _actionMap[actionId] = actions.Count;
            action = new InputAction
            {
                id = actionId
            };
            actions.Add(action);
        }
        else
        {
            action = actions[index];
        }
    }

    public static void AddBinding(int actionId, bool isPad, int code)
    {
        GetActionIndex(actionId, out _, out var action);

        // Binding the same thing twice changes nothing.
        for (var i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isPad == isPad && action.bindings[i].code == code) return;
        }

        action.bindings.Add(new ActionBinding
        {
            isPad = isPad,
            code = code
        });
    }

    public static bool IsDown(int actionId)
    {
        GetActionIndex(actionId, out _, out var action);
        return IsDown(action, ref InputSystem.keyboardState, ref InputSystem.gamePadState);
    }

    /// <summary>
    /// The action is down this frame, and none of its bindings were down on the last one. So holding
    /// one binding and then pressing a second does not count as a new press.
    /// </summary>
    public static bool IsNewDown(int actionId)
    {
        GetActionIndex(actionId, out _, out var action);
        return IsDown(action, ref InputSystem.keyboardState, ref InputSystem.gamePadState)
               && !IsDown(action, ref InputSystem.oldKeyboardState, ref InputSystem.oldGamePadState);
    }

    private static bool IsDown(InputAction action, ref KeyboardState keyboard, ref GamePadState pad)
    {
        for (var i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isPad)
            {
                if (InputSystem.IsPadButtonDown(ref pad, (Buttons)binding.code)) return true;
            }
            else
            {
                if (keyboard.IsKeyDown((Keys)binding.code)) return true;
            }
        }

        return false;
    }
}
