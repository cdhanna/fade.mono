using System;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Returns the next action ID that is not in use yet.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveActionNextId">reserve action id</see>
    /// instead, which actually claims the ID. If you already know which ID you want, skip both and
    /// call <see cref="BindActionKey">bind action key</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next action ID, and then bind a key to it:
    /// <code>
    /// ` load a font so we can show when the action fires
    /// font 1, "font"
    ///
    /// jumpAction = free action id(jumpAction)
    /// bind action key jumpAction, scanCode("Space")
    ///
    /// DO
    ///   IF action down(jumpAction) = 1
    ///     text 1, 460, 190, 1, "Jump!"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">Receives the next available action ID.</param>
    /// <returns>The next available action ID (not yet reserved).</returns>
    /// <seealso cref="ReserveActionNextId">reserve action id</seealso>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    [FadeBasicCommand("free action id")]
    public static int GetFreeActionNextId(ref int actionId)
    {
        actionId = ActionSystem.highestActionId + 1;
        return actionId;
    }

    /// <summary>
    /// <para>Claims the next available action ID.</para>
    /// <para>The action has nothing bound to it yet, so it never fires until you give it a key with
    /// <see cref="BindActionKey">bind action key</see> or a controller button with
    /// <see cref="BindActionButton">bind action button</see>.</para>
    /// </summary>
    /// <remarks>
    /// An action is a thing the player can do, like "jump" or "open the menu", kept separate from
    /// the keys and buttons that do it. The game asks about the action with
    /// <see cref="IsActionDown">action down</see>, and never has to know whether the player is on
    /// a keyboard or a controller.
    ///
    /// Reserve your actions once at startup, keep the IDs in variables, and bind inputs to them.
    /// If you would rather pick your own IDs, you can skip this command and use the numbers
    /// directly, because binding to an ID that does not exist yet creates it.
    /// </remarks>
    /// <example>
    /// Reserve two actions and give each one a key and a controller button:
    /// <code>
    /// ` load a font so we can show which action is held
    /// font 1, "font"
    ///
    /// reserve action id(jumpAction)
    /// reserve action id(shootAction)
    ///
    /// bind action key jumpAction, scanCode("Space")
    /// bind action button jumpAction, padCode("A")
    /// bind action key shootAction, scanCode("Z")
    /// bind action button shootAction, padCode("X")
    ///
    /// DO
    ///   IF action down(jumpAction) = 1
    ///     text 1, 460, 190, 1, "Jumping"
    ///   ENDIF
    ///   IF action down(shootAction) = 1
    ///     text 2, 460, 210, 1, "Shooting"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">Receives the reserved action ID.</param>
    /// <returns>The newly reserved action ID.</returns>
    /// <seealso cref="GetFreeActionNextId">free action id</seealso>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="IsActionDown">action down</seealso>
    [FadeBasicCommand("reserve action id")]
    public static int ReserveActionNextId(ref int actionId)
    {
        GetFreeActionNextId(ref actionId);
        ActionSystem.GetActionIndex(actionId, out _, out _);
        return actionId;
    }

    /// <summary>
    /// <para>Makes a keyboard key trigger an action.</para>
    /// <para>An action can have as many keys and controller buttons bound to it as you like, and
    /// any one of them triggers it.</para>
    /// </summary>
    /// <remarks>
    /// Call this once during setup for each key. Binding the same key to the same action twice
    /// does nothing, and one key can be bound to several different actions.
    ///
    /// To let the player use a controller as well, bind a button to the same action with
    /// <see cref="BindActionButton">bind action button</see>. To start an action over with no
    /// bindings, for a rebinding screen, use <see cref="ClearAction">clear action</see>.
    /// </remarks>
    /// <example>
    /// Move a sprite with either the arrow keys or WASD:
    /// <code>
    /// ` load the ghost texture and create a sprite for it
    /// texture 1, "ghost"
    /// sprite 1, 160, 120, 1
    /// px = 160
    /// speed = 3
    ///
    /// reserve action id(leftAction)
    /// reserve action id(rightAction)
    ///
    /// ` two keys each, and either one works
    /// bind action key leftAction, scanCode("Left")
    /// bind action key leftAction, scanCode("A")
    /// bind action key rightAction, scanCode("Right")
    /// bind action key rightAction, scanCode("D")
    ///
    /// DO
    ///   px = px - action down(leftAction) * speed
    ///   px = px + action down(rightAction) * speed
    ///   position sprite 1, px, 120
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">The ID of the action. It is created if it does not exist yet.</param>
    /// <param name="scanCode">The scan code of the key. Use <see cref="ScanCode">scanCode</see> to convert a name like <c>"Space"</c> to its code.</param>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="ClearAction">clear action</seealso>
    /// <seealso cref="IsActionDown">action down</seealso>
    /// <seealso cref="IsNewActionDown">new action down</seealso>
    /// <seealso cref="ReserveActionNextId">reserve action id</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    [FadeBasicCommand("bind action key")]
    public static void BindActionKey(int actionId, int scanCode)
    {
        ActionSystem.AddBinding(actionId, false, scanCode);
    }

    /// <summary>
    /// <para>Makes a controller button trigger an action.</para>
    /// <para>The triggers and the directions of the sticks count as buttons too, so they can be
    /// bound the same way.</para>
    /// </summary>
    /// <remarks>
    /// Call this once during setup for each button. An action can have several buttons, so
    /// binding both <c>"LeftTrigger"</c> and <c>"RightTrigger"</c> lets the player use either.
    /// Binding <c>"DPadLeft"</c> and <c>"LeftThumbstickLeft"</c> lets them steer with the pad or
    /// the stick.
    ///
    /// A trigger or a stick counts as pressed once it has travelled about halfway. It is fine to
    /// bind buttons when no controller is plugged in. They just never fire.
    ///
    /// Give the same action a key as well with <see cref="BindActionKey">bind action key</see>,
    /// and the game works on both devices without checking which one is in use.
    /// </remarks>
    /// <example>
    /// Fire with the space bar, or with either trigger on a controller:
    /// <code>
    /// ` load a font so we can show when the action fires
    /// font 1, "font"
    ///
    /// reserve action id(fireAction)
    /// bind action key fireAction, scanCode("Space")
    /// bind action button fireAction, padCode("LeftTrigger")
    /// bind action button fireAction, padCode("RightTrigger")
    ///
    /// shots = 0
    /// DO
    ///   ` one shot per press, no matter which input did it
    ///   IF new action down(fireAction) = 1
    ///     shots = shots + 1
    ///   ENDIF
    ///   text 1, 460, 190, 1, "Shots: " + str$(shots)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">The ID of the action. It is created if it does not exist yet.</param>
    /// <param name="padCode">The code of the controller button. Use <see cref="PadCode">padCode</see> to convert a name like <c>"A"</c> to its code.</param>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    /// <seealso cref="ClearAction">clear action</seealso>
    /// <seealso cref="IsActionDown">action down</seealso>
    /// <seealso cref="IsNewActionDown">new action down</seealso>
    /// <seealso cref="PadCode">padCode</seealso>
    [FadeBasicCommand("bind action button")]
    public static void BindActionButton(int actionId, int padCode)
    {
        ActionSystem.AddBinding(actionId, true, padCode);
    }

    /// <summary>
    /// <para>Removes every key and controller button from an action.</para>
    /// <para>The action still exists afterwards, but it never fires until something is bound to it again.</para>
    /// </summary>
    /// <remarks>
    /// This is the first half of rebinding. Clear the action, and then bind the new inputs with
    /// <see cref="BindActionKey">bind action key</see> and
    /// <see cref="BindActionButton">bind action button</see>. There is no command to remove a
    /// single binding, so to change one input, clear the action and bind the full set again.
    /// </remarks>
    /// <example>
    /// Press R to move the jump action from the space bar to the up arrow:
    /// <code>
    /// ` load a font so we can show when the action fires
    /// font 1, "font"
    ///
    /// reserve action id(jumpAction)
    /// bind action key jumpAction, scanCode("Space")
    ///
    /// DO
    ///   IF new key down(scanCode("R")) = 1
    ///     ` throw away the old key, and bind the new one
    ///     clear action jumpAction
    ///     bind action key jumpAction, scanCode("Up")
    ///   ENDIF
    ///
    ///   IF action down(jumpAction) = 1
    ///     text 1, 460, 190, 1, "Jump!"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">The ID of the action to clear.</param>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="IsActionDown">action down</seealso>
    [FadeBasicCommand("clear action")]
    public static void ClearAction(int actionId)
    {
        ActionSystem.GetActionIndex(actionId, out _, out var action);
        action.bindings.Clear();
    }

    /// <summary>
    /// <para>Returns <c>1</c> while any key or controller button bound to the action is held down.</para>
    /// <para>This fires every frame the input is held. Use
    /// <see cref="IsNewActionDown">new action down</see> if you only want the initial press.</para>
    /// </summary>
    /// <remarks>
    /// This is the action version of <see cref="IsKeyPressed">key down</see>. Instead of asking
    /// about one key, it asks about everything bound to the action with
    /// <see cref="BindActionKey">bind action key</see> and
    /// <see cref="BindActionButton">bind action button</see>, so the same line of code works for
    /// a keyboard and a controller.
    ///
    /// Good for continuous things like movement, or a modifier that is held while another input
    /// is pressed. An action with nothing bound to it always returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Hold shift, or the right shoulder button, to sprint:
    /// <code>
    /// ` load a font so we can draw the runner
    /// font 1, "font"
    ///
    /// reserve action id(sprintAction)
    /// bind action key sprintAction, scanCode("LeftShift")
    /// bind action button sprintAction, padCode("RightShoulder")
    ///
    /// px = 0
    /// DO
    ///   IF action down(sprintAction) = 1
    ///     speed = 6
    ///   ELSE
    ///     speed = 2
    ///   ENDIF
    ///
    ///   px = px + speed
    ///   IF px &gt; 640
    ///     px = 0
    ///   ENDIF
    ///
    ///   text 1, px, 120, 1, "&gt;"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">The ID of the action.</param>
    /// <returns><c>1</c> while the action is held, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsNewActionDown">new action down</seealso>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("action down")]
    public static bool IsActionDown(int actionId)
    {
        return ActionSystem.IsDown(actionId);
    }

    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame an action is pressed.</para>
    /// <para>After that it returns <c>0</c> until every input bound to the action has been let go
    /// and one is pressed again.</para>
    /// </summary>
    /// <remarks>
    /// This is the action version of <see cref="IsNewKeyPressed">new key down</see>. Use it for
    /// one-shot things like confirming a menu, jumping, or moving a cursor one step.
    ///
    /// The press belongs to the action, not to each input. If the player is already holding one
    /// input for the action and presses a second, that is not a new press. This matters for stick
    /// directions, which are bound like buttons: pushing the stick past halfway is one press, and
    /// it has to come back before it can press again.
    /// </remarks>
    /// <example>
    /// Step through a menu with the arrow keys, the d-pad, or the left stick:
    /// <code>
    /// ` load a font so we can draw the menu
    /// font 1, "font"
    ///
    /// reserve action id(upAction)
    /// reserve action id(downAction)
    /// bind action key upAction, scanCode("Up")
    /// bind action button upAction, padCode("DPadUp")
    /// bind action button upAction, padCode("LeftThumbstickUp")
    /// bind action key downAction, scanCode("Down")
    /// bind action button downAction, padCode("DPadDown")
    /// bind action button downAction, padCode("LeftThumbstickDown")
    ///
    /// menuIndex = 0
    /// menuCount = 3
    ///
    /// DO
    ///   ` one step per press
    ///   IF new action down(upAction) = 1
    ///     menuIndex = menuIndex - 1
    ///     IF menuIndex &lt; 0
    ///       menuIndex = menuCount - 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   IF new action down(downAction) = 1
    ///     menuIndex = menuIndex + 1
    ///     IF menuIndex &gt;= menuCount
    ///       menuIndex = 0
    ///     ENDIF
    ///   ENDIF
    ///
    ///   FOR i = 0 TO menuCount - 1
    ///     IF i = menuIndex
    ///       text i + 1, 20, 40 + i * 20, 1, "&gt; Option " + str$(i)
    ///     ELSE
    ///       text i + 1, 20, 40 + i * 20, 1, "  Option " + str$(i)
    ///     ENDIF
    ///   NEXT i
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="actionId">The ID of the action.</param>
    /// <returns><c>1</c> on the frame the action went from released to pressed.</returns>
    /// <seealso cref="IsActionDown">action down</seealso>
    /// <seealso cref="BindActionKey">bind action key</seealso>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new action down")]
    public static bool IsNewActionDown(int actionId)
    {
        return ActionSystem.IsNewDown(actionId);
    }

    /// <summary>
    /// <para>Converts the name of a controller button to its integer code.</para>
    /// <para>Pass the result to <see cref="BindActionButton">bind action button</see>,
    /// <see cref="IsPadButtonPressed">pad button down</see>, or
    /// <see cref="IsNewPadButtonPressed">new pad button down</see>.</para>
    /// </summary>
    /// <remarks>
    /// The names follow the layout of an Xbox controller, and other controllers are mapped onto
    /// it by position. Upper and lower case do not matter.
    ///
    /// The face buttons are <c>"A"</c>, <c>"B"</c>, <c>"X"</c>, and <c>"Y"</c>. The small buttons
    /// in the middle are <c>"Back"</c> and <c>"Start"</c>. On top are <c>"LeftShoulder"</c>,
    /// <c>"RightShoulder"</c>, <c>"LeftTrigger"</c>, and <c>"RightTrigger"</c>. The d-pad is
    /// <c>"DPadUp"</c>, <c>"DPadDown"</c>, <c>"DPadLeft"</c>, and <c>"DPadRight"</c>. Clicking a
    /// stick in is <c>"LeftStick"</c> or <c>"RightStick"</c>.
    ///
    /// Pushing a stick is a button as well: <c>"LeftThumbstickUp"</c>,
    /// <c>"LeftThumbstickDown"</c>, <c>"LeftThumbstickLeft"</c>, <c>"LeftThumbstickRight"</c>,
    /// and the same four for <c>"RightThumbstick"</c>. A stick only ever counts as pushed in
    /// one direction at a time, the one it leans toward the most, and only once it is past
    /// halfway. So a push to the right that drifts a little upward is still just "right".
    ///
    /// You typically call this once during setup, rather than converting the name every frame.
    /// The program stops with an error if the name is not a button.
    /// </remarks>
    /// <example>
    /// Look up a button once, and then check it every frame:
    /// <code>
    /// ` load a font so we can show the button state
    /// font 1, "font"
    /// aButton = padCode("A")
    ///
    /// DO
    ///   IF pad button down(aButton) = 1
    ///     text 1, 460, 190, 1, "A is held"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="button">The name of the button, like <c>"A"</c>, <c>"Back"</c>, or <c>"LeftTrigger"</c>.</param>
    /// <returns>The integer code for the given button.</returns>
    /// <seealso cref="BindActionButton">bind action button</seealso>
    /// <seealso cref="IsPadButtonPressed">pad button down</seealso>
    /// <seealso cref="IsNewPadButtonPressed">new pad button down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    [FadeBasicCommand("padCode")]
    public static int PadCode(string button)
    {
        return (int)Enum.Parse<Buttons>(button, true);
    }

    /// <summary>
    /// <para>Returns <c>1</c> while a controller button is held down.</para>
    /// <para>This reads the controller directly. If the same thing should also work from the
    /// keyboard, bind an action and use <see cref="IsActionDown">action down</see> instead.</para>
    /// </summary>
    /// <remarks>
    /// This is the controller version of <see cref="IsKeyPressed">key down</see>. Get the code
    /// for a button with <see cref="PadCode">padCode</see>.
    ///
    /// The game listens to one controller, which is the first one that is plugged in. If there is
    /// no controller, or the game window is not the one in front, every button reads as <c>0</c>.
    /// A trigger or a stick direction counts as held once it has travelled about halfway, and a
    /// stick only counts in the one direction it leans toward the most. That suits menus and
    /// grids, but it means a stick cannot be read as a diagonal this way.
    /// </remarks>
    /// <example>
    /// Move a sprite with the d-pad:
    /// <code>
    /// ` load the ghost texture and create a sprite for it
    /// texture 1, "ghost"
    /// sprite 1, 160, 120, 1
    /// px = 160
    /// py = 120
    /// speed = 3
    ///
    /// ` look up the four directions once
    /// padUp = padCode("DPadUp")
    /// padDown = padCode("DPadDown")
    /// padLeft = padCode("DPadLeft")
    /// padRight = padCode("DPadRight")
    ///
    /// DO
    ///   py = py - pad button down(padUp) * speed
    ///   py = py + pad button down(padDown) * speed
    ///   px = px - pad button down(padLeft) * speed
    ///   px = px + pad button down(padRight) * speed
    ///
    ///   position sprite 1, px, py
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="padCode">The code of the button. Use <see cref="PadCode">padCode</see> to convert a name to its code.</param>
    /// <returns><c>1</c> while the button is held, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsNewPadButtonPressed">new pad button down</seealso>
    /// <seealso cref="PadCode">padCode</seealso>
    /// <seealso cref="IsPadConnected">pad connected</seealso>
    /// <seealso cref="IsActionDown">action down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("pad button down")]
    public static bool IsPadButtonPressed(int padCode)
    {
        return InputSystem.IsPadButtonDown(ref InputSystem.gamePadState, (Buttons)padCode);
    }

    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame a controller button is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the button is still held.</para>
    /// </summary>
    /// <remarks>
    /// This is the controller version of <see cref="IsNewKeyPressed">new key down</see>. It reads
    /// the controller directly, so if the same thing should also work from the keyboard, bind an
    /// action and use <see cref="IsNewActionDown">new action down</see> instead.
    ///
    /// It works for the triggers and the stick directions too. Pushing a stick past halfway is a
    /// press, and it has to come back toward the middle before it can be pressed again.
    /// </remarks>
    /// <example>
    /// Press Start to toggle a pause screen:
    /// <code>
    /// ` load a font so we can show the pause state
    /// font 1, "font"
    /// startButton = padCode("Start")
    /// paused = 0
    ///
    /// DO
    ///   IF new pad button down(startButton) = 1
    ///     IF paused = 0
    ///       paused = 1
    ///     ELSE
    ///       paused = 0
    ///     ENDIF
    ///   ENDIF
    ///
    ///   IF paused = 1
    ///     text 1, 550, 280, 1, "PAUSED"
    ///   ELSE
    ///     text 1, 550, 280, 1, "Playing..."
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="padCode">The code of the button. Use <see cref="PadCode">padCode</see> to convert a name to its code.</param>
    /// <returns><c>1</c> on the frame the button went from released to pressed.</returns>
    /// <seealso cref="IsPadButtonPressed">pad button down</seealso>
    /// <seealso cref="PadCode">padCode</seealso>
    /// <seealso cref="IsNewActionDown">new action down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new pad button down")]
    public static bool IsNewPadButtonPressed(int padCode)
    {
        return InputSystem.IsPadButtonDown(ref InputSystem.gamePadState, (Buttons)padCode)
               && !InputSystem.IsPadButtonDown(ref InputSystem.oldGamePadState, (Buttons)padCode);
    }

    /// <summary>
    /// <para>Returns <c>1</c> if a controller is plugged in.</para>
    /// <para>This is not the way to decide which button pictures to show. A controller can be
    /// plugged in while the player uses the keyboard, so use
    /// <see cref="GetInputDevice">input device</see> for that.</para>
    /// </summary>
    /// <remarks>
    /// The answer is refreshed every frame, so it notices a controller being plugged in or pulled
    /// out while the game runs. It reads <c>0</c> while the game window is not the one in front,
    /// because the game does not listen to the controller then.
    ///
    /// You do not need to check this before using
    /// <see cref="IsPadButtonPressed">pad button down</see> or an action. Without a controller,
    /// those just read <c>0</c>.
    /// </remarks>
    /// <example>
    /// Show whether a controller is plugged in:
    /// <code>
    /// ` load a font so we can show the status
    /// font 1, "font"
    ///
    /// DO
    ///   IF pad connected() = 1
    ///     text 1, 460, 190, 1, "Controller ready"
    ///   ELSE
    ///     text 1, 460, 190, 1, "No controller"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if a controller is plugged in, <c>0</c> otherwise.</returns>
    /// <seealso cref="GetInputDevice">input device</seealso>
    /// <seealso cref="IsPadButtonPressed">pad button down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("pad connected")]
    public static bool IsPadConnected()
    {
        return InputSystem.gamePadState.IsConnected;
    }

    /// <summary>
    /// <para>Returns which device the player used most recently: <c>0</c> for the keyboard and mouse, <c>1</c> for a controller.</para>
    /// <para>Use this to decide whether on-screen prompts should show keys or controller buttons.</para>
    /// </summary>
    /// <remarks>
    /// The device changes when something is pressed on the other one. Pressing a key or clicking
    /// a mouse button moves it to <c>0</c>. Pressing a button, squeezing a trigger, or pushing a
    /// stick on a controller moves it to <c>1</c>. Moving the mouse does not count, so bumping
    /// the desk does not take the prompts away from someone holding a controller.
    ///
    /// It starts at <c>0</c>, and it does not change just because a controller is plugged in. To
    /// redraw prompts only when they need it, check
    /// <see cref="IsNewInputDevice">new input device</see>, which is <c>1</c> on the frame the
    /// device changes.
    /// </remarks>
    /// <example>
    /// Show a prompt that matches the device the player is using:
    /// <code>
    /// ` load a font so we can show the prompt
    /// font 1, "font"
    ///
    /// DO
    ///   IF input device() = 1
    ///     text 1, 460, 190, 1, "Press A to jump"
    ///   ELSE
    ///     text 1, 460, 190, 1, "Press Space to jump"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>0</c> for the keyboard and mouse, <c>1</c> for a controller.</returns>
    /// <seealso cref="IsNewInputDevice">new input device</seealso>
    /// <seealso cref="IsPadConnected">pad connected</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("input device")]
    public static int GetInputDevice()
    {
        return InputSystem.lastDevice;
    }

    /// <summary>
    /// <para>Returns <c>1</c> on the frame the player switches between the keyboard and a controller.</para>
    /// <para>On every other frame it returns <c>0</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is the moment to swap on-screen prompts from keys to controller buttons, or back.
    /// Read <see cref="GetInputDevice">input device</see> to find out which one the player
    /// switched to.
    ///
    /// It never fires on the first frame, so set up your prompts once at startup as well.
    /// </remarks>
    /// <example>
    /// Count how many times the player has switched device:
    /// <code>
    /// ` load a font so we can show the count
    /// font 1, "font"
    /// switches = 0
    ///
    /// DO
    ///   IF new input device() = 1
    ///     switches = switches + 1
    ///   ENDIF
    ///   text 1, 460, 190, 1, "Device " + str$(input device()) + ", switched " + str$(switches) + " times"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the device changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="GetInputDevice">input device</seealso>
    /// <seealso cref="IsPadConnected">pad connected</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new input device")]
    public static bool IsNewInputDevice()
    {
        return InputSystem.lastDevice != InputSystem.oldLastDevice;
    }
}
