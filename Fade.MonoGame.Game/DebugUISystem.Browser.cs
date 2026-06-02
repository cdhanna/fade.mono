// Browser flavor of DebugUISystem. Mirrors the desktop queue+state shape
// from DebugUISystem.cs but instead of rendering with ImGui (no native
// WASM deps), it serializes the per-frame command queue and pushes it to
// the parent Playground over JS interop. The Playground side renders
// the commands as a Tweakpane panel and posts user interactions back
// here via ApplyChange — the values written into the same dicts the
// desktop side uses, so DebugUICommands.cs's TryGetPreviousXxx loops
// work unchanged.
//
// Lifecycle per frame (Game1.Update):
//   StartDebug()   → clear queue
//   fbasic runs    → Push commands + TryGetPreviousXxx reads dicts
//                    (dicts hold whatever ApplyChange wrote since the
//                    previous EndDebug)
//   EndDebug()     → serialize queue to JSON, hand off to FrameSink
//                    (which posts up to the parent window), then clear
//                    dicts so the next frame starts blank.
//
// FrameSink is wired by Pages/Index.razor.cs to call a JS function that
// postMessages the parent. ApplyChange is invoked by Index.razor.cs's
// [JSInvokable] surface in response to messages coming back from the
// parent.

#if BROWSER
using System;
using System.Collections.Generic;
using System.Text;

namespace Fade.MonoGame.Core
{
    public struct DebugUICommand
    {
        public int vmInstructionIndex;
        public DebugControlType type;
        public string label;

        public string argString;
        public int argInt;
        public float argFloat;

        public int ControlId => HashCode.Combine(
            string.IsNullOrEmpty(label) ? type.ToString() : label,
            vmInstructionIndex);
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

    public static partial class DebugUISystem
    {
        public static bool autoInspectorEnabled;

        // Same surface (and same semantics) as the desktop version.
        public static Queue<DebugUICommand> controls = new Queue<DebugUICommand>();
        public static Dictionary<int, bool> controlIdToBool = new Dictionary<int, bool>();
        public static Dictionary<int, int> controlIdToInt = new Dictionary<int, int>();
        public static Dictionary<int, float> controlIdToFloat = new Dictionary<int, float>();
        public static Dictionary<int, string> controlIdToString = new Dictionary<int, string>();

        // Wired by Pages/Index.razor.cs to push the per-frame JSON queue
        // out to the parent window. Null until JS interop is wired (the
        // boot stub frame ticks before Index.razor's OnAfterRender runs).
        public static Action<string> FrameSink;

        public static void Push(DebugUICommand control)
        {
            controls.Enqueue(control);
        }

        public static bool TryGetPreviousBool(DebugUICommand command)
        {
            return controlIdToBool.TryGetValue(command.ControlId, out var val) && val;
        }

        public static bool TryGetPreviousInt(DebugUICommand command, out int val)
        {
            return controlIdToInt.TryGetValue(command.ControlId, out val);
        }

        public static bool TryGetPreviousString(DebugUICommand command, out string val)
        {
            return controlIdToString.TryGetValue(command.ControlId, out val);
        }

        public static bool TryGetPreviousFloat(DebugUICommand command, out float val)
        {
            return controlIdToFloat.TryGetValue(command.ControlId, out val);
        }

        public static void StartDebug()
        {
            controls.Clear();
        }

        /// <summary>
        /// Bumped by the host (Index.razor.cs) whenever a new program
        /// loads. Sent to JS in each frame envelope so the panel can
        /// detect "the game restarted, wipe my state" without needing
        /// a separate event channel.
        /// </summary>
        public static int generation;

        /// <summary>
        /// Called by the host when a new program is about to run. Resets
        /// transient state (autoInspectorEnabled — desktop semantics is
        /// "fbasic source has to re-call enable debug inspector after a
        /// restart") and bumps the generation counter so the JS panel
        /// drops stale window folders + inspector state.
        /// </summary>
        public static void NotifyProgramReset()
        {
            autoInspectorEnabled = false;
            generation++;
        }

        public static void EndDebug()
        {
            // Always push a frame envelope when FrameSink is wired —
            // even when the queue is empty. Pushing every frame lets
            // the JS panel poll-free (it learns autoInspectorEnabled,
            // metadata, entity lists from the envelope each tick) and
            // detect resets via the generation counter.
            if (FrameSink != null)
            {
                try
                {
                    var envelope = BuildFrameEnvelope();
                    FrameSink(envelope);
                }
                catch (Exception e) { Console.Error.WriteLine("DebugUISystem.FrameSink threw: " + e); }
            }

            controlIdToBool.Clear();
            controlIdToInt.Clear();
            controlIdToFloat.Clear();
            controlIdToString.Clear();
        }

        // ── envelope serialization ────────────────────────────────
        // Shape per frame:
        //   {
        //     "gen": <int>,           // generation counter — JS uses
        //                              this to detect program resets
        //     "queue": [...],          // DebugUICommand queue (see
        //                              SerializeQueue for shape)
        //     "autoInspector": bool,   // `enable debug inspector` flag
        //     "metadata": {...} | null,// only when autoInspector
        //     "entities": {            // only when autoInspector
        //         "sprite": [1,2,3],
        //         "transform": [...],
        //         ...
        //     } | null
        //   }
        private static string BuildFrameEnvelope()
        {
            _sb.Clear();
            _sb.Append("{\"gen\":").Append(generation);
            _sb.Append(",\"queue\":");
            AppendQueue(_sb);
            _sb.Append(",\"autoInspector\":").Append(autoInspectorEnabled ? "true" : "false");
            if (autoInspectorEnabled)
            {
                _sb.Append(",\"metadata\":");
                AppendMetadata(_sb);
                _sb.Append(",\"entities\":");
                AppendEntityIds(_sb);
            }
            _sb.Append('}');
            return _sb.ToString();
        }

        private static void AppendQueue(StringBuilder sb)
        {
            sb.Append('[');
            var first = true;
            foreach (var c in controls)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":").Append(c.ControlId);
                sb.Append(",\"t\":").Append((int)c.type);
                AppendStringField(sb, ",\"l\":", c.label);
                AppendStringField(sb, ",\"s\":", c.argString);
                sb.Append(",\"i\":").Append(c.argInt);
                sb.Append(",\"f\":");
                AppendFloat(sb, c.argFloat);
                sb.Append('}');
            }
            sb.Append(']');
        }

        // Pull metadata via the registered MetadataDebugProvider. System.Text.Json
        // is already loaded by Index.razor.cs's other JSInvokables, so the
        // reflection cost is sunk; we use it here for the nested dict.
        private static void AppendMetadata(StringBuilder sb)
        {
            var p = Fade.MonoGame.Core.Debug.DebugRegistry.Get("metadata");
            if (p == null) { sb.Append("null"); return; }
            try
            {
                var snap = p.Snapshot(0);
                sb.Append(System.Text.Json.JsonSerializer.Serialize(snap, _envelopeJsonOpts));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[DebugUISystem] metadata snapshot threw: " + e.Message);
                sb.Append("null");
            }
        }

        // For each provider (except metadata), the list of currently-live
        // entity ids. The panel diffs these against its last-known set to
        // detect adds/removes without polling.
        private static void AppendEntityIds(StringBuilder sb)
        {
            sb.Append('{');
            var first = true;
            foreach (var typeName in Fade.MonoGame.Core.Debug.DebugRegistry.ListTypes())
            {
                if (typeName == "metadata") continue;
                var p = Fade.MonoGame.Core.Debug.DebugRegistry.Get(typeName);
                if (p == null) continue;
                if (!first) sb.Append(',');
                first = false;
                AppendStringField(sb, "", typeName);
                sb.Append(':').Append('[');
                var firstId = true;
                try
                {
                    foreach (var id in p.ListIds())
                    {
                        if (!firstId) sb.Append(',');
                        firstId = false;
                        sb.Append(id);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[DebugUISystem] {typeName}.ListIds threw: {e.Message}");
                }
                sb.Append(']');
            }
            sb.Append('}');
        }

        private static readonly System.Text.Json.JsonSerializerOptions _envelopeJsonOpts =
            new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

        // Hand-rolled JSON to keep this hot path allocation-light and
        // avoid pulling System.Text.Json into the trim graph just for
        // the queue body. Output shape (one object per command):
        //   { "id":<int>, "t":<int>, "l":"<label>", "s":"<str>",
        //     "i":<int>, "f":<float> }
        // 'id' is the ControlId so the Playground can echo it back via
        // ApplyChange without re-hashing label+vmIdx itself. Per-command
        // payloads stay 4-6 fields wide because the lib uses ARG_*
        // follow-up commands for min/max/placeholder etc.
        private static StringBuilder _sb = new StringBuilder(1024);

        private static void AppendStringField(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            if (value == null) { sb.Append("null"); return; }
            sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"':  sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (ch < 0x20)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void AppendFloat(StringBuilder sb, float value)
        {
            // NaN / Infinity aren't valid JSON. Substitute null so the
            // Playground parser doesn't choke; the slider just sees a
            // missing value and keeps whatever it had.
            if (float.IsNaN(value) || float.IsInfinity(value)) sb.Append("null");
            else sb.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        // Called by Pages/Index.razor.cs's [JSInvokable] in response to
        // tweakpane interactions in the parent. Kind values mirror the
        // controlIdToXxx dicts: 0=bool, 1=int, 2=float, 3=string.
        // valueJson is the raw string from JS — for bool we accept
        // "true"/"false"/"1"/"0", for numbers Invariant parse, for
        // strings the raw text. Button presses arrive as kind=0 with
        // value=true; sliders/drags as kind=1 or 2 plus an accompanying
        // kind=0 bool=true so DebugUICommands.cs's "did change" return
        // value goes true that frame.
        public const int KIND_BOOL = 0;
        public const int KIND_INT = 1;
        public const int KIND_FLOAT = 2;
        public const int KIND_STRING = 3;

        public static void ApplyChange(int ctrlId, int kind, string valueJson)
        {
            try
            {
                switch (kind)
                {
                    case KIND_BOOL:
                        controlIdToBool[ctrlId] = ParseBool(valueJson);
                        break;
                    case KIND_INT:
                        if (int.TryParse(valueJson, System.Globalization.NumberStyles.Integer,
                                System.Globalization.CultureInfo.InvariantCulture, out var i))
                        {
                            controlIdToInt[ctrlId] = i;
                            controlIdToBool[ctrlId] = true;
                        }
                        break;
                    case KIND_FLOAT:
                        if (float.TryParse(valueJson, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var f))
                        {
                            controlIdToFloat[ctrlId] = f;
                            controlIdToBool[ctrlId] = true;
                        }
                        break;
                    case KIND_STRING:
                        controlIdToString[ctrlId] = valueJson ?? string.Empty;
                        controlIdToBool[ctrlId] = true;
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("DebugUISystem.ApplyChange threw: " + e);
            }
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s == "1") return true;
            if (s == "0") return false;
            return string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
