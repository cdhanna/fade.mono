using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    // ── window ──────────────────────────────────────────────

    /// <summary>
    /// Opens a new debug window with the given title. Every widget command pushed between this and the matching <see cref="Debug_EndWindow">end debug window</see> renders inside the same panel section.
    ///
    /// Pair every call with <see cref="Debug_EndWindow">end debug window</see>, and run both inside your main game loop. The debug UI is immediate-mode — widgets only exist for frames where the commands actually execute, so as soon as you stop emitting a window, it disappears.
    /// </summary>
    /// <remarks>
    /// The whole debug system is built around the idea that you re-declare your UI every frame instead of constructing it once at boot. That makes it trivial to show different controls based on game state — wrap the window in an <c>IF</c> and it vanishes the moment the condition flips. It also means there's no "destroy widget" command; if you stop emitting a widget, it stops drawing.
    ///
    /// In the Playground and in browser exports, debug windows render as their own Tweakpane sections inside the "Debug UI" tab (or the overlay panel in standalone exports — open with <c>?debug=1</c> or call <c>fadeDebug.enable()</c> from the dev console). On desktop, they render as ImGui windows floating over the game canvas. The string you pass as <c>name</c> is what shows up as the section header.
    ///
    /// Layout commands like <see cref="Debug_BeginTree">begin debug tree</see> and <see cref="Debug_BeginTabBar">begin debug tab bar</see> can nest inside a window, but every begin needs its own matching end. Widget commands emitted outside any window are silently dropped — they have no place to go.
    ///
    /// Two different begin calls with the same title merge into one section, so you can re-open the same window from multiple parts of your code without worrying about duplicates. The auto-inspector (see <see cref="Debug_EnableInspector">enable debug inspector</see>) is a separate panel — it shows up alongside your custom windows, not inside them.
    /// </remarks>
    /// <example>
    /// A minimal debug window with a button and a slider:
    /// <code>
    /// score = 0
    /// speed = 50
    /// DO
    ///   begin debug window "Player"
    ///   debug label "score", str$(score)
    ///   IF debug button("reset score") = 1
    ///     score = 0
    ///   ENDIF
    ///   changed = debug int slider("speed", speed, 0, 100)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Conditionally show a window only while a flag is on:
    /// <code>
    /// showTweaks = 1
    /// DO
    ///   IF showTweaks = 1
    ///     begin debug window "Tweaks"
    ///     debug text "press the button to hide me"
    ///     IF debug button("hide") = 1
    ///       showTweaks = 0
    ///     ENDIF
    ///     end debug window
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The window title shown at the top of the section. Multiple windows with the same title merge into one section.</param>
    /// <seealso cref="Debug_EndWindow">end debug window</seealso>
    /// <seealso cref="Debug_Button">debug button</seealso>
    /// <seealso cref="Debug_Label">debug label</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("begin debug window")]
    public static void Debug_BeginWindow([FromVm] VirtualMachine vm, string name)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, label = name, type = DebugControlType.WINDOW_START
        });
    }

    /// <summary>
    /// Closes the debug window opened by the most recent <see cref="Debug_BeginWindow">begin debug window</see> call.
    ///
    /// Every <see cref="Debug_BeginWindow">begin debug window</see> needs a matching <see cref="Debug_EndWindow">end debug window</see>. Without one, downstream widgets stay inside the previous window forever (or worse, get dropped entirely).
    /// </summary>
    /// <remarks>
    /// Think of begin/end as a push/pop pair. Every widget you emit while a window is "open" belongs to that window. As soon as you call <see cref="Debug_EndWindow">end debug window</see>, the window closes and any further widget commands either get dropped (if you didn't open another window) or go into the next window you open.
    ///
    /// You don't pass the window name here — the system knows which window is open because of the order you called them in. If you nest debug commands inside other helper subroutines, make sure each subroutine's begins and ends balance, or you'll lose track of which window is current.
    /// </remarks>
    /// <example>
    /// Two windows in a single frame, each properly closed:
    /// <code>
    /// showGrid = 0
    /// DO
    ///   begin debug window "Stats"
    ///   debug label "mouse x", str$(mouse x())
    ///   end debug window
    ///
    ///   begin debug window "Tweaks"
    ///   changed = debug toggle("show grid", showGrid)
    ///   end debug window
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    [FadeBasicCommand("end debug window")]
    public static void Debug_EndWindow([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.WINDOW_END
        });
    }

    // ── layout ──────────────────────────────────────────────

    /// <summary>
    /// Tells the next widget to render on the same horizontal line as the previous one instead of starting a new row.
    ///
    /// Desktop only: the ImGui inspector arranges widgets in flow layout and honors this hint. The browser inspector stacks every widget vertically, so this command is a no-op there.
    /// </summary>
    /// <remarks>
    /// Use this when you want to put two related widgets side-by-side — say a label next to a small button, or three buttons across one row instead of three rows. Call it BETWEEN the two widget commands; it tells the second one where to land relative to the first.
    ///
    /// Only the very next widget is affected. After it renders, layout returns to the normal vertical stack. If you want three widgets on one row, you need two <see cref="Debug_SameLine">debug same line</see> calls — one before each follow-up widget.
    /// </remarks>
    /// <example>
    /// Two buttons side by side on the desktop inspector:
    /// <code>
    /// DO
    ///   begin debug window "Controls"
    ///   IF debug button("save") = 1
    ///     ` save logic
    ///   ENDIF
    ///   debug same line
    ///   IF debug button("load") = 1
    ///     ` load logic
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_Separator">debug separator</seealso>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    [FadeBasicCommand("debug same line")]
    public static void Debug_SameLine([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.SAME_LINE
        });
    }

    /// <summary>
    /// Draws a thin horizontal divider line between the previous widget and the next one.
    ///
    /// Visual only — separators don't take any input and don't change layout flow apart from a tiny vertical gap.
    /// </summary>
    /// <remarks>
    /// Use a separator to group related widgets inside the same window. Without one, a window full of toggles and sliders becomes a wall of rows; a couple of separators turn it into something scannable.
    ///
    /// Separators stack — calling this twice in a row gives you two divider lines with a small gap between them. There's no width parameter; the line spans the full width of the current window or section.
    /// </remarks>
    /// <example>
    /// Split a debug window into visual sections:
    /// <code>
    /// speed = 100
    /// accel = 50
    /// showGrid = 0
    /// DO
    ///   begin debug window "Tuning"
    ///   debug text "movement"
    ///   changed = debug int slider("speed", speed, 0, 200)
    ///   changed = debug int slider("accel", accel, 0, 200)
    ///
    ///   debug separator
    ///   debug text "rendering"
    ///   changed = debug toggle("show grid", showGrid)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    /// <seealso cref="Debug_BeginTree">begin debug tree</seealso>
    [FadeBasicCommand("debug separator")]
    public static void Debug_Separator([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.SEPARATOR
        });
    }

    /// <summary>
    /// Opens a collapsible tree section with the given label, and returns <c>1</c> while the user has it expanded.
    ///
    /// Always wrap the inner widgets in an <c>IF begin debug tree(...) = 1 ... ENDIF</c> block and call <see cref="Debug_EndTree">end debug tree</see> from inside that block. Calling the inner widgets when the tree is collapsed wastes time pushing commands the panel will skip; calling <see cref="Debug_EndTree">end debug tree</see> outside the IF would leave the tree stack unbalanced.
    /// </summary>
    /// <remarks>
    /// Trees are how you keep a busy debug window manageable. A typical pattern is one tree per subsystem — "Movement", "Rendering", "Audio" — so the user can collapse the ones they don't need and focus on a single area.
    ///
    /// The expanded state lives in the panel, not in your fbasic code. You don't have to track which trees are open; just call this every frame and the panel remembers what the user last clicked. After a Run/Stop cycle the panel even restores expansion across program restarts, so a debug session feels continuous.
    ///
    /// Trees can nest inside other trees, tabs, or windows, but every <see cref="Debug_BeginTree">begin debug tree</see> needs its own matching <see cref="Debug_EndTree">end debug tree</see>. If you forget the end, widgets after this tree get parented to the wrong place.
    /// </remarks>
    /// <example>
    /// Group movement controls under a collapsible tree:
    /// <code>
    /// speed = 50
    /// accel = 100
    /// DO
    ///   begin debug window "Tuning"
    ///   IF begin debug tree("movement") = 1
    ///     changed = debug int slider("speed", speed, 0, 200)
    ///     changed = debug int slider("accel", accel, 0, 200)
    ///     end debug tree
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The text shown on the tree header.</param>
    /// <returns><c>1</c> when the tree is currently expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_EndTree">end debug tree</seealso>
    /// <seealso cref="Debug_BeginTabBar">begin debug tab bar</seealso>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    [FadeBasicCommand("begin debug tree")]
    public static int Debug_BeginTree([FromVm] VirtualMachine vm, string label)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TREE_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Closes the tree section opened by the most recent <see cref="Debug_BeginTree">begin debug tree</see> call.
    ///
    /// Only call this when <see cref="Debug_BeginTree">begin debug tree</see> returned <c>1</c> — i.e. from inside the <c>IF</c> block that wraps the tree's contents. Calling it when the tree wasn't actually opened that frame would unbalance the begin/end stack.
    /// </summary>
    /// <remarks>
    /// Just like the window pair, this is a stack pop: it closes the tree most recently opened by <see cref="Debug_BeginTree">begin debug tree</see>. Trees nest, so the popped tree might not be the outermost one.
    /// </remarks>
    /// <example>
    /// Two nested trees — note each end matches the inner-most still-open begin:
    /// <code>
    /// bloomOn = 1
    /// vsync = 1
    /// DO
    ///   begin debug window "Settings"
    ///   IF begin debug tree("graphics") = 1
    ///     IF begin debug tree("post-fx") = 1
    ///       changed = debug toggle("bloom", bloomOn)
    ///       end debug tree
    ///     ENDIF
    ///     changed = debug toggle("vsync", vsync)
    ///     end debug tree
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_BeginTree">begin debug tree</seealso>
    [FadeBasicCommand("end debug tree")]
    public static void Debug_EndTree([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TREE_END
        });
    }

    /// <summary>
    /// Opens a tab bar with the given identifier. Returns <c>1</c> while the bar is active and rendering.
    ///
    /// Desktop only: the ImGui inspector renders tab bars as a row of clickable tabs. The browser inspector currently doesn't render tab bars yet, so on the browser side this looks like a no-op. Use <see cref="Debug_BeginTree">begin debug tree</see> for cross-platform grouping.
    /// </summary>
    /// <remarks>
    /// Tab bars are useful when you have several distinct workflows in the same debug window and only want one visible at a time — for example, a "Display" tab and an "Audio" tab inside a single "Settings" window. Each individual page goes inside its own <see cref="Debug_BeginTab">begin debug tab</see> / <see cref="Debug_EndTab">end debug tab</see> pair, all nested between this and <see cref="Debug_EndTabBar">end debug tab bar</see>.
    ///
    /// The <c>id</c> string is an identity hint for the panel — it's how the panel tracks which tab was last selected. It doesn't show up on screen; the visible labels come from the individual <see cref="Debug_BeginTab">begin debug tab</see> calls.
    /// </remarks>
    /// <example>
    /// Two-tab settings window:
    /// <code>
    /// volume# = 0.5
    /// invertMouse = 0
    /// DO
    ///   begin debug window "Settings"
    ///   IF begin debug tab bar("settings_tabs") = 1
    ///     IF begin debug tab("audio") = 1
    ///       changed = debug float slider("volume", volume#, 0.0, 1.0)
    ///       end debug tab
    ///     ENDIF
    ///     IF begin debug tab("input") = 1
    ///       changed = debug toggle("invert mouse", invertMouse)
    ///       end debug tab
    ///     ENDIF
    ///     end debug tab bar
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">A unique identifier for this tab bar, used by the panel to remember which tab was last selected.</param>
    /// <returns><c>1</c> when the tab bar is active, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_EndTabBar">end debug tab bar</seealso>
    /// <seealso cref="Debug_BeginTab">begin debug tab</seealso>
    /// <seealso cref="Debug_BeginTree">begin debug tree</seealso>
    [FadeBasicCommand("begin debug tab bar")]
    public static int Debug_BeginTabBar([FromVm] VirtualMachine vm, string id)
    {
        var command = new DebugUICommand
        {
            label = id,
            type = DebugControlType.TAB_BAR_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Closes the tab bar opened by the most recent <see cref="Debug_BeginTabBar">begin debug tab bar</see>.
    ///
    /// Desktop only — see the note on <see cref="Debug_BeginTabBar">begin debug tab bar</see>. Call this from inside the <c>IF begin debug tab bar(...) = 1</c> block, after every tab page inside has been closed.
    /// </summary>
    /// <remarks>
    /// Like the other end-* commands, this just pops the current tab bar off the stack. Every <see cref="Debug_BeginTabBar">begin debug tab bar</see> needs a matching <see cref="Debug_EndTabBar">end debug tab bar</see>, and every <see cref="Debug_BeginTab">begin debug tab</see> inside needs its own <see cref="Debug_EndTab">end debug tab</see> before this gets called.
    /// </remarks>
    /// <seealso cref="Debug_BeginTabBar">begin debug tab bar</seealso>
    /// <seealso cref="Debug_EndTab">end debug tab</seealso>
    [FadeBasicCommand("end debug tab bar")]
    public static void Debug_EndTabBar([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TAB_BAR_END
        });
    }

    /// <summary>
    /// Opens one tab inside a tab bar. Returns <c>1</c> while this is the active tab.
    ///
    /// Desktop only — see <see cref="Debug_BeginTabBar">begin debug tab bar</see>. Must be called between <see cref="Debug_BeginTabBar">begin debug tab bar</see> and <see cref="Debug_EndTabBar">end debug tab bar</see>, wrapped in <c>IF begin debug tab(...) = 1 ... ENDIF</c> with <see cref="Debug_EndTab">end debug tab</see> inside that block.
    /// </summary>
    /// <remarks>
    /// Each tab page is its own scope for widget commands. Only the contents of the active tab actually appear; the inactive tabs' inner widgets still get pushed (the begin/end discipline matters) but stay hidden.
    ///
    /// Tab pages can themselves contain trees, separators, or any other layout structure. Just don't put another tab bar directly inside a tab page without good reason — the UI gets visually noisy fast.
    /// </remarks>
    /// <example>
    /// See <see cref="Debug_BeginTabBar">begin debug tab bar</see> for a complete two-tab example.
    /// </example>
    /// <param name="label">The text shown on the tab's clickable header.</param>
    /// <returns><c>1</c> when this tab is the currently-selected one, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_EndTab">end debug tab</seealso>
    /// <seealso cref="Debug_BeginTabBar">begin debug tab bar</seealso>
    [FadeBasicCommand("begin debug tab")]
    public static int Debug_BeginTab([FromVm] VirtualMachine vm, string label)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TAB_ITEM_START,
            vmInstructionIndex = vm.instructionIndex,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Closes the tab page opened by the most recent <see cref="Debug_BeginTab">begin debug tab</see>.
    ///
    /// Desktop only — see <see cref="Debug_BeginTabBar">begin debug tab bar</see>. Call from inside the <c>IF begin debug tab(...) = 1</c> block.
    /// </summary>
    /// <remarks>
    /// Each <see cref="Debug_BeginTab">begin debug tab</see> needs a matching <see cref="Debug_EndTab">end debug tab</see> before either another tab opens or the surrounding <see cref="Debug_EndTabBar">end debug tab bar</see> closes the bar.
    /// </remarks>
    /// <seealso cref="Debug_BeginTab">begin debug tab</seealso>
    /// <seealso cref="Debug_EndTabBar">end debug tab bar</seealso>
    [FadeBasicCommand("end debug tab")]
    public static void Debug_EndTab([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            vmInstructionIndex = vm.instructionIndex, type = DebugControlType.TAB_ITEM_END
        });
    }

    // ── display ─────────────────────────────────────────────

    /// <summary>
    /// Renders a read-only "label: value" pair inside the current debug window.
    ///
    /// Both sides are plain strings. If you want to display a number, convert it first with <c>str$()</c> — there's no numeric overload.
    /// </summary>
    /// <remarks>
    /// This is the workhorse for showing live state. Compute the value every frame and pass it in; the panel will update the displayed text as the value changes. Common uses: showing FPS, the player's coordinates, an AI's current state name, the size of a list.
    ///
    /// The widget is read-only — there's no input. If you want the user to be able to change something, use <see cref="Debug_TextBox">debug textbox</see> for a string or one of the slider commands for a number.
    /// </remarks>
    /// <example>
    /// Show the player's position and current FPS:
    /// <code>
    /// px = 200
    /// py = 200
    /// DO
    ///   begin debug window "Status"
    ///   debug label "x", str$(px)
    ///   debug label "y", str$(py)
    ///   debug label "mouse x", str$(mouse x())
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text on the left side of the row.</param>
    /// <param name="value">The current value shown on the right side.</param>
    /// <seealso cref="Debug_Text">debug text</seealso>
    /// <seealso cref="Debug_TextBox">debug textbox</seealso>
    [FadeBasicCommand("debug label")]
    public static void Debug_Label([FromVm] VirtualMachine vm, string label, string value)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = label,
            argString = value,
            type = DebugControlType.LABEL,
            vmInstructionIndex = vm.instructionIndex
        });
    }

    /// <summary>
    /// Renders a single line of read-only text inside the current debug window. No "label" column — the text spans the whole row.
    ///
    /// Use this for free-form notes, section headers without dividers, or short status messages where the "label: value" shape of <see cref="Debug_Label">debug label</see> would feel forced.
    /// </summary>
    /// <remarks>
    /// Think of this as a one-line caption. It's good for headings like "tuning" or "advanced" above a group of widgets, especially in combination with <see cref="Debug_Separator">debug separator</see>.
    ///
    /// Multi-line strings render with their line breaks preserved.
    /// </remarks>
    /// <example>
    /// Use debug text as a section header above some sliders:
    /// <code>
    /// gravity# = 9.8
    /// friction# = 0.5
    /// DO
    ///   begin debug window "Tuning"
    ///   debug text "physics"
    ///   changed = debug float slider("gravity", gravity#, 0.0, 50.0)
    ///   changed = debug float slider("friction", friction#, 0.0, 1.0)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="text">The text to display.</param>
    /// <seealso cref="Debug_Label">debug label</seealso>
    /// <seealso cref="Debug_Separator">debug separator</seealso>
    [FadeBasicCommand("debug text")]
    public static void Debug_Text([FromVm] VirtualMachine vm, string text)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            argString = text,
            type = DebugControlType.TEXT,
            vmInstructionIndex = vm.instructionIndex
        });
    }

    // ── interactive controls ────────────────────────────────

    /// <summary>
    /// Renders a clickable button with the given text. Returns <c>1</c> on the frame the user clicked it, <c>0</c> every other frame.
    ///
    /// The button doesn't "stay pressed" — the return value pulses to <c>1</c> for exactly one frame on each click, just like <see cref="MouseClick">mouse click</see>. Wrap the click handler in an <c>IF debug button(...) = 1</c>.
    /// </summary>
    /// <remarks>
    /// Buttons are perfect for one-shot actions: reset a counter, reload a level, fire off a sound for testing. Pair the button with whatever logic should run when it fires.
    ///
    /// If you need a persistent on/off state, use <see cref="Debug_Toggle">debug toggle</see> instead — that's a checkbox that holds its value across frames.
    ///
    /// You can have multiple buttons in a window with different labels. Each one tracks its own click state independently.
    /// </remarks>
    /// <example>
    /// A button that resets a score counter:
    /// <code>
    /// score = 0
    /// DO
    ///   begin debug window "Player"
    ///   debug label "score", str$(score)
    ///   IF debug button("reset") = 1
    ///     score = 0
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Several actions sharing a row:
    /// <code>
    /// DO
    ///   begin debug window "Tools"
    ///   IF debug button("save") = 1
    ///     ` save logic
    ///   ENDIF
    ///   debug same line
    ///   IF debug button("load") = 1
    ///     ` load logic
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The text shown on the button.</param>
    /// <returns><c>1</c> on the frame the user clicked the button, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_Toggle">debug toggle</seealso>
    /// <seealso cref="Debug_SameLine">debug same line</seealso>
    [FadeBasicCommand("debug button")]
    public static int Debug_Button([FromVm] VirtualMachine vm, string name)
    {
        var command = new DebugUICommand
        {
            label = name,
            type = DebugControlType.BUTTON,
            vmInstructionIndex = vm.instructionIndex
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders a checkbox bound to an integer variable. The variable is updated in place to <c>0</c> (unchecked) or <c>1</c> (checked) whenever the user toggles it. Returns <c>1</c> on the frame the toggle changed.
    ///
    /// You own the underlying variable — make sure to pass it by reference. The widget reflects whatever value the variable holds when the command runs, so you can also flip it yourself in code and the checkbox follows along.
    /// </summary>
    /// <remarks>
    /// Use a toggle for any boolean preference: feature flags, debug overlay visibility, AI cheats. The variable persists between frames in your code, so the checkbox keeps its state without any extra bookkeeping.
    ///
    /// The return value is most useful if you only want to act on the click itself — for example, replaying a sound effect when the user just flipped the switch. If you just need the current state, read the variable directly instead.
    /// </remarks>
    /// <example>
    /// A checkbox that gates an overlay:
    /// <code>
    /// showHitboxes = 0
    /// DO
    ///   begin debug window "Debug"
    ///   changed = debug toggle("show hitboxes", showHitboxes)
    ///   end debug window
    ///
    ///   IF showHitboxes = 1
    ///     ` draw hitbox overlays
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The text shown next to the checkbox.</param>
    /// <param name="value">A variable holding the current state. <c>0</c> is unchecked, anything else is checked. Updated in place when the user clicks the checkbox.</param>
    /// <returns><c>1</c> on the frame the user changed the state, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_Button">debug button</seealso>
    /// <seealso cref="Debug_IntSlider">debug int slider</seealso>
    [FadeBasicCommand("debug toggle")]
    public static int Debug_Toggle([FromVm] VirtualMachine vm, string label, ref int value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.CHECKBOX,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders a single-line text input bound to a string variable. The variable is updated in place as the user types. Returns <c>1</c> on the frame the text changed.
    ///
    /// The user's edits land in your variable directly — pass it by reference and read it like any other string. The widget is a text input, not a multi-line editor; line breaks aren't supported in the value.
    /// </summary>
    /// <remarks>
    /// Text boxes are great for tunable strings — a debug-only name, a cheat-code entry field, a URL or path you're iterating on. Combine with <see cref="Debug_Button">debug button</see> for a "type, then commit" pattern: edit the string in the textbox, click the button to actually apply it.
    ///
    /// The <c>placeholder</c> text shows up in faded letters when the variable is empty, giving the user a hint about what to type. The <c>maxLength</c> caps how many characters they can enter; the default of <c>512</c> is generous for most debug uses.
    /// </remarks>
    /// <example>
    /// A textbox that drives a label and a re-fire button:
    /// <code>
    /// nameStr$ = ""
    /// greeting$ = ""
    /// DO
    ///   begin debug window "Greeter"
    ///   changed = debug textbox("name", nameStr$, "type a name", 64)
    ///   IF debug button("greet") = 1
    ///     greeting$ = "hello, " + nameStr$
    ///   ENDIF
    ///   debug label "last greeting", greeting$
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text next to the input.</param>
    /// <param name="value">A string variable holding the current value. Updated in place as the user types.</param>
    /// <param name="placeholder">Faded hint text shown when the value is empty. Pass <c>""</c> to skip.</param>
    /// <param name="maxLength">Maximum number of characters the user can type. Defaults to <c>512</c>.</param>
    /// <returns><c>1</c> on the frame the value changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_Label">debug label</seealso>
    /// <seealso cref="Debug_Button">debug button</seealso>
    [FadeBasicCommand("debug textbox")]
    public static int Debug_TextBox([FromVm] VirtualMachine vm, string label, ref string value, string placeholder = "", int maxLength = 512)
    {
        var ctrl = new DebugUICommand
        {
            label = label,
            type = DebugControlType.TEXTFIELD,
            vmInstructionIndex = vm.instructionIndex,
            argString = value,
        };
        DebugUISystem.Push(ctrl);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_STRING, argString = placeholder });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = maxLength });

        if (DebugUISystem.TryGetPreviousString(ctrl, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(ctrl) ? 1 : 0;
    }

    /// <summary>
    /// Renders an integer slider with the given range. The variable is updated in place as the user drags. Returns <c>1</c> on the frame the value changed.
    ///
    /// The variable is clamped to <c>[min, max]</c> on each update — there's no way for the user to push it outside the bounds you set. If you want an unbounded number input, use <see cref="Debug_DragInt">debug drag int</see> instead.
    /// </summary>
    /// <remarks>
    /// Sliders are the bread and butter of game-feel tuning. Speed, damage, jump height, enemy count — anything you'd want to dial in without recompiling. Set the min/max to a sensible range and the slider handle covers it visually so the user has a sense of where they are in the range.
    ///
    /// If you don't pass <c>min</c> and <c>max</c>, you get a 0–100 range by default. For tighter ranges (say, 1–10 enemies), set them explicitly.
    /// </remarks>
    /// <example>
    /// Two sliders driving enemy spawn rate and count:
    /// <code>
    /// spawnRate = 60
    /// enemyCount = 5
    /// DO
    ///   begin debug window "Spawner"
    ///   changed = debug int slider("spawn rate", spawnRate, 1, 240)
    ///   changed = debug int slider("enemy count", enemyCount, 1, 50)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The descriptive text next to the slider.</param>
    /// <param name="value">A variable holding the current value. Updated in place as the user drags.</param>
    /// <param name="min">The smallest value the slider can produce. Defaults to <c>0</c>.</param>
    /// <param name="max">The largest value the slider can produce. Defaults to <c>100</c>.</param>
    /// <returns><c>1</c> on the frame the value changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_FloatSlider">debug float slider</seealso>
    /// <seealso cref="Debug_DragInt">debug drag int</seealso>
    /// <seealso cref="Debug_Toggle">debug toggle</seealso>
    [FadeBasicCommand("debug int slider")]
    public static int Debug_IntSlider([FromVm] VirtualMachine vm, string name, ref int value, int min = 0, int max = 100)
    {
        var command = new DebugUICommand
        {
            label = name,
            type = DebugControlType.INT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = min });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_INT, argInt = max });
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders a float slider with the given range. The variable is updated in place as the user drags. Returns <c>1</c> on the frame the value changed.
    ///
    /// Same shape as <see cref="Debug_IntSlider">debug int slider</see> but for floating-point values. The variable is clamped to <c>[min, max]</c>. For an unbounded float input, use <see cref="Debug_DragFloat">debug drag float</see>.
    /// </summary>
    /// <remarks>
    /// Use this for any continuous quantity — friction, opacity, audio volume, animation speeds. Floats give you finer control than ints when the slider's range is small.
    ///
    /// The default range is <c>0.0</c> to <c>100.0</c>. For a normalised slider (volume, opacity, etc.) override min/max to <c>0.0</c> and <c>1.0</c>.
    /// </remarks>
    /// <example>
    /// Tune gravity and air friction:
    /// <code>
    /// gravity# = 9.8
    /// friction# = 0.05
    /// DO
    ///   begin debug window "Physics"
    ///   changed = debug float slider("gravity", gravity#, 0.0, 50.0)
    ///   changed = debug float slider("friction", friction#, 0.0, 1.0)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text next to the slider.</param>
    /// <param name="value">A variable holding the current value. Updated in place as the user drags.</param>
    /// <param name="min">The smallest value the slider can produce. Defaults to <c>0.0</c>.</param>
    /// <param name="max">The largest value the slider can produce. Defaults to <c>100.0</c>.</param>
    /// <returns><c>1</c> on the frame the value changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_IntSlider">debug int slider</seealso>
    /// <seealso cref="Debug_DragFloat">debug drag float</seealso>
    [FadeBasicCommand("debug float slider")]
    public static int Debug_FloatSlider([FromVm] VirtualMachine vm, string label, ref float value, float min = 0, float max = 100)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.FLOAT_SLIDER,
            vmInstructionIndex = vm.instructionIndex,
            argFloat = value,
        };
        DebugUISystem.Push(command);
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_FLOAT, argFloat = min });
        DebugUISystem.Push(new DebugUICommand { type = DebugControlType.ARG_FLOAT, argFloat = max });
        if (DebugUISystem.TryGetPreviousFloat(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders a number field that the user can drag left/right to change. No min/max — the value can go anywhere. Returns <c>1</c> on the frame the value changed.
    ///
    /// Unlike <see cref="Debug_IntSlider">debug int slider</see>, there's no bounded range. Use this when you don't know the right scale up front, or when you want the user to be able to type a number directly.
    /// </summary>
    /// <remarks>
    /// Drag-int is the "give me a number, I don't care what" widget. It feels like a slider but without the range, so it's the right pick for things like a frame counter, a debug instruction-pointer, or any tuning value where bounds would be misleading.
    ///
    /// On both desktop and the browser inspector, the user can also click the field and type a value directly. The drag interaction is just the quick way.
    /// </remarks>
    /// <example>
    /// A drag-int that controls a manual frame counter:
    /// <code>
    /// frame = 0
    /// DO
    ///   begin debug window "Stepper"
    ///   changed = debug drag int("frame", frame)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text next to the field.</param>
    /// <param name="value">A variable holding the current value. Updated in place as the user drags or types.</param>
    /// <returns><c>1</c> on the frame the value changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_IntSlider">debug int slider</seealso>
    /// <seealso cref="Debug_DragFloat">debug drag float</seealso>
    [FadeBasicCommand("debug drag int")]
    public static int Debug_DragInt([FromVm] VirtualMachine vm, string label, ref int value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.DRAG_INT,
            vmInstructionIndex = vm.instructionIndex,
            argInt = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders a float field the user can drag or type into. No min/max. Returns <c>1</c> on the frame the value changed.
    ///
    /// Float version of <see cref="Debug_DragInt">debug drag int</see>. Pick this when you have a continuous quantity with no clear bounds — a world coordinate, a delta time, an offset.
    /// </summary>
    /// <remarks>
    /// Use this freely when you're just trying to find the right value. The lack of bounds means you don't have to pre-decide a range — drag to explore, type to land on something specific.
    ///
    /// If you do know the range up front, <see cref="Debug_FloatSlider">debug float slider</see> is friendlier because the handle position gives you a visual sense of where in the range you are.
    /// </remarks>
    /// <example>
    /// Two drag-float fields used as world coordinates:
    /// <code>
    /// targetX# = 0.0
    /// targetY# = 0.0
    /// DO
    ///   begin debug window "Target"
    ///   changed = debug drag float("x", targetX#)
    ///   changed = debug drag float("y", targetY#)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text next to the field.</param>
    /// <param name="value">A variable holding the current value. Updated in place as the user drags or types.</param>
    /// <returns><c>1</c> on the frame the value changed, <c>0</c> otherwise.</returns>
    /// <seealso cref="Debug_FloatSlider">debug float slider</seealso>
    /// <seealso cref="Debug_DragInt">debug drag int</seealso>
    [FadeBasicCommand("debug drag float")]
    public static int Debug_DragFloat([FromVm] VirtualMachine vm, string label, ref float value)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.DRAG_FLOAT,
            vmInstructionIndex = vm.instructionIndex,
            argFloat = value,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousFloat(command, out var val))
        {
            value = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Renders an RGBA color swatch tied to a packed color integer. Returns <c>1</c> on the frame the user changed the color.
    ///
    /// The bound variable is the same packed color format produced by <see cref="Rgb">rgb</see> — so you can pass it directly to commands like <see cref="SetTextColor">color text</see>, <see cref="ColorSprite">color sprite</see>, or <see cref="ClsColor">cls</see>.
    /// </summary>
    /// <remarks>
    /// Color picker is how you visually pin down a tint, fade, or clear color while the game is running. Click the swatch to open a color picker; drag the saturation/value square or alpha slider; the bound variable updates on every change.
    ///
    /// Since the variable holds a packed color, you can also seed it with <see cref="Rgb">rgb</see> on the way in:
    /// <code>
    /// shade = rgb(255, 200, 100)
    /// </code>
    /// The picker comes up showing that exact color the first frame it renders.
    /// </remarks>
    /// <example>
    /// Tune a text-sprite's color live:
    /// <code>
    /// font 1, "font"
    /// text 1, 650, 380, 1, "HELLO"
    /// shade = rgb(255, 255, 255)
    /// DO
    ///   begin debug window "Text"
    ///   changed = debug color picker("color", shade)
    ///   end debug window
    ///   color text 1, shade
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="label">The descriptive text next to the swatch.</param>
    /// <param name="colorCode">A variable holding the packed RGBA color. Updated in place when the user picks a new color. Use <see cref="Rgb">rgb</see> to build initial values.</param>
    /// <returns><c>1</c> on the frame the user changed the color, <c>0</c> otherwise.</returns>
    /// <seealso cref="Rgb">rgb</seealso>
    /// <seealso cref="SetTextColor">color text</seealso>
    /// <seealso cref="ColorSprite">color sprite</seealso>
    [FadeBasicCommand("debug color picker")]
    public static int Debug_ColorPicker([FromVm] VirtualMachine vm, string label, ref int colorCode)
    {
        var command = new DebugUICommand
        {
            label = label,
            type = DebugControlType.COLOR_PICKER,
            vmInstructionIndex = vm.instructionIndex,
            argInt = colorCode,
        };
        DebugUISystem.Push(command);
        if (DebugUISystem.TryGetPreviousInt(command, out var val))
        {
            colorCode = val;
        }
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    // ── auto inspector ───────────────────────────────────────

    /// <summary>
    /// Turns on the built-in auto-inspector panel. The inspector shows performance metadata plus a live, expandable list of every sprite, transform, tween, collider, text, texture, sfx instance, and render output in the game.
    ///
    /// Call this once at startup (or whenever you want the inspector visible). The state persists until <see cref="Debug_DisableInspector">disable debug inspector</see> is called or the program is restarted — but a fresh program run resets it back to off, matching how `enable gizmos` and similar debug toggles behave.
    /// </summary>
    /// <remarks>
    /// The auto-inspector is the easiest way to peek at your game's state without writing any custom widgets. It appears as its own section in the Debug UI panel (or the overlay in standalone exports), alongside any <see cref="Debug_BeginWindow">begin debug window</see> windows you've created. You can expand each entity to see its fields and edit them live — change a sprite's position, flip a tween's progress, even retint colors.
    ///
    /// The inspector is purely a viewer/editor of existing state — it doesn't add or destroy game objects. If you want the inspector's controls embedded inside one of your own debug windows (so it's part of a combined panel), use <see cref="Debug_Inspector">debug inspector</see> instead.
    ///
    /// The gizmo overlay (sprite/collider/text outlines) is tied to per-entity state set by <see cref="EnableSpriteGizmo">enable sprite gizmo</see> etc., not to the inspector itself — turning the inspector off doesn't hide the gizmos.
    /// </remarks>
    /// <example>
    /// Enable the inspector at startup and inspect a sprite as it moves:
    /// <code>
    /// enable debug inspector
    ///
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// DO
    ///   position sprite 1, mouse x(), mouse y()
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_DisableInspector">disable debug inspector</seealso>
    /// <seealso cref="Debug_Inspector">debug inspector</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    [FadeBasicCommand("enable debug inspector")]
    public static void Debug_EnableInspector()
    {
        DebugUISystem.autoInspectorEnabled = true;
    }

    /// <summary>
    /// Turns off the auto-inspector panel.
    ///
    /// Doesn't affect custom debug windows or per-entity gizmo overlays — those keep running. Only the inspector section disappears.
    /// </summary>
    /// <remarks>
    /// Useful for shipping builds where you want the inspector code path available (in case you need to flip it back on for support) but hidden by default. Pair with <see cref="Debug_EnableInspector">enable debug inspector</see> to build a "developer mode" toggle.
    /// </remarks>
    /// <example>
    /// Toggle the inspector with a key press:
    /// <code>
    /// inspectorOn = 1
    /// iKey = scanCode("I")
    /// enable debug inspector
    /// DO
    ///   IF new key down(iKey) = 1
    ///     IF inspectorOn = 1
    ///       disable debug inspector
    ///       inspectorOn = 0
    ///     ELSE
    ///       enable debug inspector
    ///       inspectorOn = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("disable debug inspector")]
    public static void Debug_DisableInspector()
    {
        DebugUISystem.autoInspectorEnabled = false;
    }

    // ── resource browsers ────────────────────────────────────

    /// <summary>
    /// Embeds a scrollable list of every live sprite inside the current debug window. Each entry expands to show that sprite's fields, just like in the auto-inspector.
    ///
    /// Desktop only: the ImGui inspector renders the embedded browsers as collapsible lists. The browser inspector currently shows entity browsers only inside the auto-inspector panel — use <see cref="Debug_EnableInspector">enable debug inspector</see> there.
    /// </summary>
    /// <remarks>
    /// The browser commands are how you build a single combined debug panel that mixes your own controls with the engine's per-entity introspection. Drop a <see cref="Debug_BrowseSprites">debug browse sprites</see> into a window of your own and you've got a sprite browser sitting right next to your custom widgets, no separate inspector window required.
    ///
    /// All browsers refresh live each frame as entities are created or destroyed. If you want just one specific sprite's inspector instead of the whole list, use <see cref="Debug_Sprite">debug sprite</see>.
    /// </remarks>
    /// <example>
    /// A debug window that combines a sprite list with a manual reset button:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// DO
    ///   begin debug window "Sprites"
    ///   IF debug button("hide first") = 1
    ///     hide sprite 1
    ///   ENDIF
    ///   debug separator
    ///   debug browse sprites
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_Sprite">debug sprite</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    /// <seealso cref="Debug_BrowseTextures">debug browse textures</seealso>
    [FadeBasicCommand("debug browse sprites")]
    public static void Debug_BrowseSprites([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_sprites", type = DebugControlType.BROWSER_SPRITE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every loaded shader effect inside the current debug window. Each entry expands to show that effect's editable shader parameters.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, the same list shows up under the auto-inspector's "Effects" section.
    /// </summary>
    /// <remarks>
    /// Effects are loaded shader programs. The browser entry exposes whatever parameters the shader has declared — a per-shader fields list, so a bloom shader and an outline shader expose entirely different controls.
    ///
    /// To focus on one specific effect, use <see cref="Debug_Effect">debug effect</see>.
    /// </remarks>
    /// <seealso cref="Debug_Effect">debug effect</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    /// <seealso cref="LoadEffect">load effect</seealso>
    [FadeBasicCommand("debug browse effects")]
    public static void Debug_BrowseEffects([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_effects", type = DebugControlType.BROWSER_EFFECT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every transform inside the current debug window. Each entry expands to show position, scale, rotation, and parent linkage.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, transforms appear under the auto-inspector's "Transforms" section.
    /// </summary>
    /// <remarks>
    /// Transforms drive sprite/text/collider positions when those entities are anchored to one via <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>. Use the browser to spot wrong parent chains or unexpected rotations.
    /// </remarks>
    /// <seealso cref="Debug_Transform">debug transform</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse transforms")]
    public static void Debug_BrowseTransforms([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_transforms", type = DebugControlType.BROWSER_TRANSFORM,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every active tween inside the current debug window. Each entry shows progress, start/end values, easing curve, and play state.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, tweens appear under the auto-inspector's "Tweens" section.
    /// </summary>
    /// <remarks>
    /// Tween entries are mostly read-only — they're a window into what the tween system is doing right now. The progress value updates each frame, so a tween that's stuck or skipped will be obvious at a glance.
    /// </remarks>
    /// <seealso cref="Debug_Tween">debug tween</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse tweens")]
    public static void Debug_BrowseTweens([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_tweens", type = DebugControlType.BROWSER_TWEEN,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every collider inside the current debug window. Each entry expands to show position, size, target transform, and computed world bounds.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, colliders appear under the auto-inspector's "Colliders" section.
    /// </summary>
    /// <remarks>
    /// Pair this with <see cref="EnableColliderGizmo">enable collider gizmo</see> on the colliders you're tuning so you can see the bounding boxes drawn on the canvas while you adjust their numeric properties in the browser.
    /// </remarks>
    /// <seealso cref="Debug_Collider">debug collider</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse colliders")]
    public static void Debug_BrowseColliders([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_colliders", type = DebugControlType.BROWSER_COLLIDER,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every text sprite inside the current debug window. Each entry shows the text content, color, position, scale, font, drop shadow toggle, and gizmo controls.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, texts appear under the auto-inspector's "Texts" section.
    /// </summary>
    /// <remarks>
    /// Text browsing is especially handy when chasing typography issues — wrong font, bad anchor offsets, alpha-zero color. Every field is editable so you can dial in the right look without recompiling.
    /// </remarks>
    /// <seealso cref="Debug_TextSprite">debug text sprite</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse texts")]
    public static void Debug_BrowseTexts([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_texts", type = DebugControlType.BROWSER_TEXT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every active sfx instance inside the current debug window. Each entry shows the playback state plus volume / pitch / pan / loop controls.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, sfx instances appear under the auto-inspector's "Sfxs" section.
    /// </summary>
    /// <remarks>
    /// One row per playing sound. Adjust pitch / pan / volume live to find a good mix without restarting the game.
    /// </remarks>
    /// <seealso cref="Debug_Sfx">debug sfx</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse sfx")]
    public static void Debug_BrowseSfx([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_sfx", type = DebugControlType.BROWSER_SFX,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every registered texture inside the current debug window. Each entry shows a thumbnail plus width / height / format / asset path.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, textures appear under the auto-inspector's "Textures" section.
    /// </summary>
    /// <remarks>
    /// The thumbnail makes it easy to confirm that an asset actually loaded correctly — black squares are missing or mis-pathed textures, and the right side of the entry shows the path the engine resolved.
    /// </remarks>
    /// <seealso cref="Debug_Texture">debug texture</seealso>
    /// <seealso cref="Texture">texture</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse textures")]
    public static void Debug_BrowseTextures([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_textures", type = DebugControlType.BROWSER_TEXTURE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds a list of every render output inside the current debug window. Each entry shows a live preview plus the clear color and target-texture binding.
    ///
    /// Desktop only — see <see cref="Debug_BrowseSprites">debug browse sprites</see>. For the browser inspector, render outputs appear under the auto-inspector's "Render outputs" section.
    /// </summary>
    /// <remarks>
    /// Render outputs are the back-buffers your scene composites into. The browser shows a thumbnail of each output as it's being drawn, which makes layered render pipelines (e.g. depth pass + composite) easy to visualise without instrumentation.
    /// </remarks>
    /// <seealso cref="Debug_RenderOutput">debug render output</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug browse render outputs")]
    public static void Debug_BrowseRenderOutputs([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "browse_render_outputs", type = DebugControlType.BROWSER_RENDER_OUTPUT,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    // ── composite ───────────────────────────────────────────

    /// <summary>
    /// Embeds an interactive REPL console inside the current debug window. The console lets you type one-line expressions and run them against the live VM.
    ///
    /// Desktop only: the ImGui inspector renders the embedded console. The browser inspector doesn't host a REPL today, so this command is a no-op there.
    /// </summary>
    /// <remarks>
    /// The console is most useful when you want to poke at state without rebuilding — read a variable, set a flag, call a command directly. Drop it into a debug window so the console sits alongside your other tuning widgets.
    ///
    /// Output from the expression appears in the same console pane, scrollback-style.
    /// </remarks>
    /// <seealso cref="Debug_Inspector">debug inspector</seealso>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    [FadeBasicCommand("debug console")]
    public static void Debug_Console([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "console", type = DebugControlType.CONSOLE,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds the full auto-inspector view inside the current debug window — the same metadata + per-entity browsers <see cref="Debug_EnableInspector">enable debug inspector</see> shows as a standalone panel, but parented to whatever debug window you call this from.
    ///
    /// Desktop only: the ImGui inspector honors the embed and re-parents the inspector contents. The browser inspector currently keeps the auto-inspector in its own dedicated section — there a custom window that contains only this widget is silently dropped to avoid a duplicate inspector showing up.
    /// </summary>
    /// <remarks>
    /// Use this when you want a single combined debug panel: metadata, your tuning sliders, the entity browsers, all in the same window. The arrangement gives you a one-stop dashboard instead of two separate sections (one auto-inspector, one custom window).
    ///
    /// If you only want the metadata block or one specific browser, prefer the more focused commands — <see cref="Debug_Metadata">debug metadata</see>, <see cref="Debug_BrowseSprites">debug browse sprites</see>, etc.
    /// </remarks>
    /// <example>
    /// One window combining a custom tuning section with the full inspector:
    /// <code>
    /// gravity# = 9.8
    /// DO
    ///   begin debug window "Dev Panel"
    ///   debug text "tuning"
    ///   changed = debug float slider("gravity", gravity#, 0.0, 50.0)
    ///   debug separator
    ///   debug inspector
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    /// <seealso cref="Debug_Metadata">debug metadata</seealso>
    /// <seealso cref="Debug_BeginWindow">begin debug window</seealso>
    [FadeBasicCommand("debug inspector")]
    public static void Debug_Inspector([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "inspector", type = DebugControlType.INSPECTOR,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    /// <summary>
    /// Embeds the metadata block inside the current debug window — the same FPS, frame time, memory, and resource counts the auto-inspector's "Metadata" folder shows.
    ///
    /// Desktop only: the ImGui inspector honors the embed. The browser inspector keeps the metadata block exclusively inside its own auto-inspector pane.
    /// </summary>
    /// <remarks>
    /// Use this to pin the performance numbers in a custom dashboard window alongside your own widgets. Metadata is read-only except for the system-wide gizmo toggle, so you can also turn gizmos on/off from here.
    /// </remarks>
    /// <example>
    /// A debug window that combines metadata with a tweak:
    /// <code>
    /// showGrid = 0
    /// DO
    ///   begin debug window "Dev"
    ///   debug metadata
    ///   debug separator
    ///   changed = debug toggle("show grid", showGrid)
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="Debug_Inspector">debug inspector</seealso>
    /// <seealso cref="Debug_EnableInspector">enable debug inspector</seealso>
    [FadeBasicCommand("debug metadata")]
    public static void Debug_Metadata([FromVm] VirtualMachine vm)
    {
        DebugUISystem.Push(new DebugUICommand
        {
            label = "metadata", type = DebugControlType.COMPONENT_METADATA,
            vmInstructionIndex = vm.instructionIndex,
        });
    }

    // ── component inspectors ────────────────────────────────

    /// <summary>
    /// Embeds a per-entity inspector for one specific sprite inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only: the ImGui inspector renders the embedded sprite view. On the browser inspector, the same per-sprite controls live under the auto-inspector's "Sprites" section — use <see cref="Debug_BrowseSprites">debug browse sprites</see> or <see cref="Debug_EnableInspector">enable debug inspector</see> there.
    /// </summary>
    /// <remarks>
    /// Single-entity inspectors are useful when you have a hero object you're constantly tuning — the player sprite, a specific enemy, a UI element — and you want its controls front and center in your custom debug window without having to scroll through a full sprite browser.
    ///
    /// The returned <c>1</c>/<c>0</c> reflects the expansion state: when collapsed, the inspector hides its inner widgets and you can skip work that depended on them.
    /// </remarks>
    /// <example>
    /// Pin the player sprite's inspector in a dev window:
    /// <code>
    /// texture 1, "ghost"
    /// sprite 1, 200, 200, 1
    /// DO
    ///   begin debug window "Player"
    ///   IF debug sprite(1) = 1
    ///     ` inspector is open
    ///   ENDIF
    ///   end debug window
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to inspect. Must have been created with <see cref="Sprite">sprite</see>.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseSprites">debug browse sprites</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="EnableSpriteGizmo">enable sprite gizmo</seealso>
    [FadeBasicCommand("debug sprite")]
    public static int Debug_Sprite([FromVm] VirtualMachine vm, int spriteId)
    {
        var command = new DebugUICommand
        {
            label = "sprite", type = DebugControlType.COMPONENT_SPRITE,
            vmInstructionIndex = vm.instructionIndex, argInt = spriteId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific shader effect inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-effect controls live under the auto-inspector's "Effects" section.
    /// </summary>
    /// <remarks>
    /// Shader parameters are surfaced field-by-field, so this is the right widget for live-tweaking a post-process effect (bloom radius, outline thickness) while watching the result on the canvas.
    /// </remarks>
    /// <param name="effectId">The effect to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseEffects">debug browse effects</seealso>
    /// <seealso cref="LoadEffect">load effect</seealso>
    [FadeBasicCommand("debug effect")]
    public static int Debug_Effect([FromVm] VirtualMachine vm, int effectId)
    {
        var command = new DebugUICommand
        {
            label = "effect", type = DebugControlType.COMPONENT_EFFECT,
            vmInstructionIndex = vm.instructionIndex, argInt = effectId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific transform inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-transform controls live under the auto-inspector's "Transforms" section.
    /// </summary>
    /// <remarks>
    /// Useful when several sprites share an anchor transform — you can pin that transform's inspector in a custom window and watch position / rotation / scale propagate to every child without scrolling the full transforms list.
    /// </remarks>
    /// <param name="transformId">The transform to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseTransforms">debug browse transforms</seealso>
    /// <seealso cref="CreateTransform">transform</seealso>
    [FadeBasicCommand("debug transform")]
    public static int Debug_Transform([FromVm] VirtualMachine vm, int transformId)
    {
        var command = new DebugUICommand
        {
            label = "transform", type = DebugControlType.COMPONENT_TRANSFORM,
            vmInstructionIndex = vm.instructionIndex, argInt = transformId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific tween inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-tween controls live under the auto-inspector's "Tweens" section.
    /// </summary>
    /// <remarks>
    /// Tween inspectors are read-only views — they show the start/end values, the current progress, and the easing curve. Use them to confirm a tween is firing at all, or to spot a misconfigured duration.
    /// </remarks>
    /// <param name="tweenId">The tween to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseTweens">debug browse tweens</seealso>
    [FadeBasicCommand("debug tween")]
    public static int Debug_Tween([FromVm] VirtualMachine vm, int tweenId)
    {
        var command = new DebugUICommand
        {
            label = "tween", type = DebugControlType.COMPONENT_TWEEN,
            vmInstructionIndex = vm.instructionIndex, argInt = tweenId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific collider inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-collider controls live under the auto-inspector's "Colliders" section.
    /// </summary>
    /// <remarks>
    /// Pair this with <see cref="EnableColliderGizmo">enable collider gizmo</see> on the same id so you can see the collider's bounding box drawn on the canvas while you tune its position and size in the inspector.
    /// </remarks>
    /// <param name="colliderId">The collider to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseColliders">debug browse colliders</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="EnableColliderGizmo">enable collider gizmo</seealso>
    [FadeBasicCommand("debug collider")]
    public static int Debug_Collider([FromVm] VirtualMachine vm, int colliderId)
    {
        var command = new DebugUICommand
        {
            label = "collider", type = DebugControlType.COMPONENT_COLLIDER,
            vmInstructionIndex = vm.instructionIndex, argInt = colliderId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific text sprite inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-text controls live under the auto-inspector's "Texts" section.
    /// </summary>
    /// <remarks>
    /// Lets you live-edit a text sprite's content, color, and position — handy for iterating on UI text without recompiling. The text content field is editable too, so you can poke a different string in to check overflow behavior.
    /// </remarks>
    /// <param name="textId">The text sprite to inspect. Must have been created with the <c>text</c> command.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseTexts">debug browse texts</seealso>
    /// <seealso cref="EnableTextGizmo">enable text gizmo</seealso>
    [FadeBasicCommand("debug text sprite")]
    public static int Debug_TextSprite([FromVm] VirtualMachine vm, int textId)
    {
        var command = new DebugUICommand
        {
            label = "text", type = DebugControlType.COMPONENT_TEXT,
            vmInstructionIndex = vm.instructionIndex, argInt = textId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific sfx instance inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-sfx controls live under the auto-inspector's "Sfxs" section.
    /// </summary>
    /// <remarks>
    /// Use this to pin one playing sound's controls — pitch, pan, volume, loop — at the top of a dev window while you tune it. The inspector reflects the live state every frame, so you'll see the sound transition from playing to stopped naturally.
    /// </remarks>
    /// <param name="sfxId">The sfx instance to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseSfx">debug browse sfx</seealso>
    [FadeBasicCommand("debug sfx")]
    public static int Debug_Sfx([FromVm] VirtualMachine vm, int sfxId)
    {
        var command = new DebugUICommand
        {
            label = "sfx", type = DebugControlType.COMPONENT_SFX,
            vmInstructionIndex = vm.instructionIndex, argInt = sfxId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific texture inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-texture view lives under the auto-inspector's "Textures" section.
    /// </summary>
    /// <remarks>
    /// The texture inspector is read-only — width, height, format, asset path, plus a thumbnail. Drop this in next to a sprite's inspector to confirm the texture is the one you think it is.
    /// </remarks>
    /// <param name="textureId">The texture to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseTextures">debug browse textures</seealso>
    /// <seealso cref="Texture">texture</seealso>
    [FadeBasicCommand("debug texture")]
    public static int Debug_Texture([FromVm] VirtualMachine vm, int textureId)
    {
        var command = new DebugUICommand
        {
            label = "texture", type = DebugControlType.COMPONENT_TEXTURE,
            vmInstructionIndex = vm.instructionIndex, argInt = textureId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }

    /// <summary>
    /// Embeds an inspector for one specific render output inside the current debug window. Returns <c>1</c> while the inspector node is expanded.
    ///
    /// Desktop only — see <see cref="Debug_Sprite">debug sprite</see>. On the browser inspector, the same per-output view lives under the auto-inspector's "Render outputs" section.
    /// </summary>
    /// <remarks>
    /// Render outputs are the offscreen back-buffers you composite into. The inspector shows a live thumbnail plus the clear color and target-texture binding — useful for confirming a multi-pass pipeline is rendering to the buffers you expect.
    /// </remarks>
    /// <param name="outputId">The render output to inspect.</param>
    /// <returns><c>1</c> when the inspector is expanded, <c>0</c> when collapsed.</returns>
    /// <seealso cref="Debug_BrowseRenderOutputs">debug browse render outputs</seealso>
    [FadeBasicCommand("debug render output")]
    public static int Debug_RenderOutput([FromVm] VirtualMachine vm, int outputId)
    {
        var command = new DebugUICommand
        {
            label = "render_output", type = DebugControlType.COMPONENT_RENDER_OUTPUT,
            vmInstructionIndex = vm.instructionIndex, argInt = outputId,
        };
        DebugUISystem.Push(command);
        return DebugUISystem.TryGetPreviousBool(command) ? 1 : 0;
    }
}
