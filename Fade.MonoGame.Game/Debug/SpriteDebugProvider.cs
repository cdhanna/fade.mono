// Surfaces SpriteSystem.sprites to the inspector panel. The panel
// renders one Tweakpane folder per sprite (via the schema) and
// posts SetField calls when the user drags a slider or picks a
// color. Apply mutates SpriteSystem.sprites[index] in place so the
// next game frame draws the edited sprite.

using System.Collections.Generic;
using System.Text.Json;
using FadeBasic.Lib.Standard.Util;

namespace Fade.MonoGame.Core.Debug;

public sealed class SpriteDebugProvider : IDebugProvider
{
    public string TypeName => "sprite";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        // Render in approximately the order the desktop ImGui inspector
        // uses, so users moving between platforms see a familiar layout.
        new DebugField { Path = "color",             Type = "color",  Label = "color" },
        new DebugField { Path = "position",          Type = "vec2",   Label = "position",
                         Min = -10000f, Max = 10000f },
        new DebugField { Path = "scale",             Type = "vec2",   Label = "scale",
                         Min = -100f,   Max = 100f },
        new DebugField { Path = "origin",            Type = "vec2",   Label = "origin",
                         Min = -1f,     Max = 1f },
        new DebugField { Path = "rotation",          Type = "float",  Label = "rotation",
                         // -π to π — same range the ImGui slider uses.
                         Min = -3.14159f, Max = 3.14159f },
        new DebugField { Path = "hidden",            Type = "bool",   Label = "hidden" },
        new DebugField { Path = "zOrder",            Type = "int",    Label = "z-order",
                         Min = -1000f,  Max = 1000f },
        // ReferenceType wires these int fields to dropdowns sourced
        // from the matching provider's ListIds() — so "imageId" shows
        // a list of every Texture currently registered instead of a
        // raw int slider.
        // Label says "textureId" even though the underlying sprite
        // field is called imageId — the referenced provider IS
        // "texture", so the inspector label should match what the
        // user sees in the texture list. The Path stays "imageId"
        // so Apply still maps onto `s.imageId` without renaming the
        // engine-side field.
        new DebugField { Path = "imageId",           Type = "int",    Label = "textureId",
                         ReferenceType = "texture",
                         Min = 0f, Max = 100000f },
        new DebugField { Path = "effectId",          Type = "int",    Label = "effectId",
                         ReferenceType = "effect",
                         Min = 0f, Max = 100000f },
        new DebugField { Path = "anchorTransformId", Type = "int",    Label = "anchorTransformId",
                         ReferenceType = "transform",
                         Min = 0f, Max = 100000f },
        new DebugField { Path = "currentFrame",      Type = "int",    Label = "frame",
                         ReadOnly = true },
        // Live thumbnail of the bound texture, encoded as a base64
        // data: URL. Populated each Snapshot from the sprite's imageId
        // → TextureSystem lookup, or null when the sprite has no
        // texture loaded.
        new DebugField { Path = "preview",           Type = "image",  Label = "preview", ReadOnly = true },
        // Gizmo overlay controls. State lives in GizmoSystem.spriteGizmos
        // keyed by sprite id; presence in the dict IS the "enabled" bit.
        // Color round-trips through the same packed-int wire format as
        // fbasic's `rgb()`. Mirrors the imperative commands
        // `enable sprite gizmo` / `set sprite gizmo color` /
        // `set sprite gizmo thickness`.
        new DebugField { Path = "gizmo",          Type = "bool",  Label = "gizmo" },
        new DebugField { Path = "gizmoColor",     Type = "color", Label = "gizmo color" },
        new DebugField { Path = "gizmoThickness", Type = "float", Label = "gizmo thickness",
                         Min = 0f, Max = 8f },
    };

    public IEnumerable<int> ListIds()
    {
        // Iterate the live sprites slice — SpriteSystem.sprites is a
        // pre-allocated fixed-size pool; only indices [0, spriteCount)
        // are valid.
        for (var i = 0; i < SpriteSystem.spriteCount; i++)
            yield return SpriteSystem.sprites[i].id;
    }

    public object Snapshot(int id)
    {
        SpriteSystem.GetSpriteIndex(id, out _, out var s);

        // Resolve the bound texture for a small preview thumbnail.
        // Cheap-skipped when the sprite has no texture (imageId == 0
        // is the "no texture" sentinel for most code paths).
        string preview = null;
        if (s.imageId > 0)
        {
            try
            {
                TextureSystem.GetTextureIndex(s.imageId, out _, out var rt);
                preview = TexturePreview.TryEncode(rt.texture);
            }
            catch { /* texture not loaded yet — no preview */ }
        }

        // Tweakpane auto-detects widget types from the bound value's
        // shape: number → slider, bool → checkbox, [r,g,b,a] array →
        // color swatch (when we also tell it color via the schema),
        // [x,y] array → two-component number input. We pre-shape the
        // snapshot to match those expectations.
        var hasGizmo = GizmoSystem.spriteGizmos.TryGetValue(id, out var giz);
        return new Dictionary<string, object>
        {
            ["color"]             = DebugColor.Pack(s.color),
            ["position"]          = new[] { s.position.X, s.position.Y },
            ["scale"]             = new[] { s.scale.X, s.scale.Y },
            ["origin"]            = new[] { s.origin.X, s.origin.Y },
            ["rotation"]          = s.rotation,
            ["hidden"]            = s.hidden,
            ["zOrder"]            = s.zOrder,
            ["imageId"]           = s.imageId,
            ["effectId"]          = s.effectId,
            ["anchorTransformId"] = s.anchorTransformId,
            ["currentFrame"]      = s.currentFrame,
            ["preview"]           = preview ?? "",
            ["gizmo"]             = hasGizmo,
            ["gizmoColor"]        = DebugColor.Pack(hasGizmo ? giz.color : GizmoSystem.DefaultColor),
            ["gizmoThickness"]    = hasGizmo ? giz.thickness : GizmoSystem.DefaultThickness,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        SpriteSystem.GetSpriteIndex(id, out var idx, out var s);
        switch (path)
        {
            // Vec2 components arrive separately ("position.X", "position.Y")
            // because Tweakpane's number-pair binding fires per-component.
            case "position.X":          s.position.X = value.GetSingle(); break;
            case "position.Y":          s.position.Y = value.GetSingle(); break;
            case "scale.X":             s.scale.X    = value.GetSingle(); break;
            case "scale.Y":             s.scale.Y    = value.GetSingle(); break;
            case "origin.X":            s.origin.X   = value.GetSingle(); break;
            case "origin.Y":            s.origin.Y   = value.GetSingle(); break;
            case "rotation":            s.rotation   = value.GetSingle(); break;
            case "hidden":              s.hidden     = value.GetBoolean(); break;
            case "zOrder":              s.zOrder     = value.GetInt32(); break;
            case "imageId":             s.imageId    = value.GetInt32(); break;
            case "effectId":            s.effectId   = value.GetInt32(); break;
            case "anchorTransformId":   s.anchorTransformId = value.GetInt32(); break;
            // Color is shipped as a single packed RGBA int (same wire
            // format as fbasic's `rgb()` command — see DebugColor below).
            case "color":               s.color = DebugColor.Unpack(value.GetInt32()); break;
            // Gizmo controls. Mirror the imperative `enable/disable sprite
            // gizmo` + `set sprite gizmo color/thickness` semantics: toggling
            // on creates a default entry, toggling off removes it, and a
            // color/thickness edit on a not-yet-enabled sprite auto-creates
            // with defaults (so the user can dial in the look before flipping
            // the toggle, just like the fbasic commands do).
            case "gizmo":
                if (value.GetBoolean()) {
                    if (!GizmoSystem.spriteGizmos.ContainsKey(id)) {
                        GizmoSystem.spriteGizmos[id] = new SpriteGizmo {
                            color = GizmoSystem.DefaultColor,
                            thickness = GizmoSystem.DefaultThickness,
                        };
                    }
                } else {
                    GizmoSystem.spriteGizmos.Remove(id);
                }
                return true;
            case "gizmoColor": {
                if (!GizmoSystem.spriteGizmos.TryGetValue(id, out var g)) {
                    g = new SpriteGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.color = DebugColor.Unpack(value.GetInt32());
                GizmoSystem.spriteGizmos[id] = g;
                return true;
            }
            case "gizmoThickness": {
                if (!GizmoSystem.spriteGizmos.TryGetValue(id, out var g)) {
                    g = new SpriteGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.thickness = value.GetSingle();
                GizmoSystem.spriteGizmos[id] = g;
                return true;
            }
            default: return false;
        }
        SpriteSystem.sprites[idx] = s;
        return true;
    }
}

// Bridge between MonoGame's Microsoft.Xna.Framework.Color and the
// FadeBasic packed-int format. Delegates straight to ColorUtil so the
// debug-inspector wire format stays identical to what `rgb(r,g,b,a)`
// produces in user code — pack/unpack here and pack/unpack there are
// the same function.
internal static class DebugColor
{
    public static int Pack(Microsoft.Xna.Framework.Color c)
    {
        ColorUtil.PackColor(c.R, c.G, c.B, c.A, out var code);
        return code;
    }
    public static Microsoft.Xna.Framework.Color Unpack(int code)
    {
        ColorUtil.UnpackColor(code, out var r, out var g, out var b, out var a);
        return new Microsoft.Xna.Framework.Color(r, g, b, a);
    }
}
