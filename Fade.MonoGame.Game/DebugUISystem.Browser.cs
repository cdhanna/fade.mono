// Browser-side stubs for the DebugUISystem surface that fbasic command
// code (Fade.MonoGame.Lib/DebugUICommands.cs) references. The real
// DebugUISystem renders an ImGui dev UI on desktop; the browser flavor
// (Phase 5 of mg.md, when we lift it into HTML dockview panels) hasn't
// landed yet, so the commands no-op for now. This keeps user fbasic source
// that uses `set ui slider` / `begin debug window` / etc. portable between
// desktop and browser builds — it just doesn't show anything in browser.

#if BROWSER
namespace Fade.MonoGame.Core
{
    public static class DebugUISystem
    {
        public static bool autoInspectorEnabled;

        public static void Push(DebugUICommand command) { /* no-op in browser */ }
        public static bool TryGetPreviousBool(DebugUICommand command) => false;
        public static bool TryGetPreviousInt(DebugUICommand command, out int value) { value = 0; return false; }
        public static bool TryGetPreviousFloat(DebugUICommand command, out float value) { value = 0f; return false; }
        public static bool TryGetPreviousString(DebugUICommand command, out string value) { value = string.Empty; return false; }
    }

    public struct DebugUICommand
    {
        public int vmInstructionIndex;
        public string label;
        public DebugControlType type;
        public int argInt;
        public float argFloat;
        public string argString;
        public uint colorCode;
        public object value;
    }

    public enum DebugControlType
    {
        WINDOW_START, WINDOW_END,
        SAME_LINE, SEPARATOR,
        TREE_START, TREE_END,
        TAB_BAR_START, TAB_BAR_END,
        TAB_ITEM_START, TAB_ITEM_END,
        BUTTON, CHECKBOX, COLOR_PICKER,
        DRAG_FLOAT, DRAG_INT,
        FLOAT_SLIDER, INT_SLIDER,
        LABEL, TEXT, TEXTFIELD,
        CONSOLE, INSPECTOR,
        ARG_FLOAT, ARG_INT, ARG_STRING,
        COMPONENT_COLLIDER, COMPONENT_EFFECT, COMPONENT_METADATA,
        COMPONENT_RENDER_OUTPUT, COMPONENT_SFX, COMPONENT_SPRITE,
        COMPONENT_TEXT, COMPONENT_TEXTURE, COMPONENT_TRANSFORM, COMPONENT_TWEEN,
        BROWSER_COLLIDER, BROWSER_EFFECT, BROWSER_RENDER_OUTPUT,
        BROWSER_SFX, BROWSER_SPRITE, BROWSER_TEXT,
        BROWSER_TEXTURE, BROWSER_TRANSFORM, BROWSER_TWEEN,
    }
}
#endif
