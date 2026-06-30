// Remaining IDebugProvider implementations — one per game system that
// the inspector panel can surface. All providers follow the same
// pattern as SpriteDebugProvider: Schema declares which fields exist,
// ListIds enumerates the live entities, Snapshot returns a JSON-
// serializable dict, Apply mutates the system's array in place.
//
// Things deferred until v2:
//   • Texture / RenderOutput image previews. The inspector panel needs
//     a way to display arbitrary Texture2Ds; the C# providers expose
//     dimensions + path metadata only for now.
//   • Live shader-parameter editing (Effect provider).
//   • Resource-ID combo pickers — sprite/text/collider providers
//     expose imageId/anchorTransformId as plain ints; we'd ideally
//     show a dropdown of valid IDs but Tweakpane's combo widget
//     needs a static list and these dictionaries change at runtime.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Fade.MonoGame.Core.Debug;

// ── Transform ─────────────────────────────────────────────────────────

public sealed class TransformDebugProvider : IDebugProvider
{
    public string TypeName => "transform";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "position", Type = "vec2",  Label = "position",
                         Min = -10000f, Max = 10000f },
        new DebugField { Path = "scale",    Type = "vec2",  Label = "scale",
                         Min = -100f,   Max = 100f },
        new DebugField { Path = "angle",    Type = "float", Label = "angle",
                         Min = -3.14159f, Max = 3.14159f },
        new DebugField { Path = "parentIndex",    Type = "int", Label = "parentIndex",    ReadOnly = true },
        new DebugField { Path = "referenceCount", Type = "int", Label = "referenceCount", ReadOnly = true },
    };

    public IEnumerable<int> ListIds()
    {
        for (var i = 0; i < TransformSystem.transformCount; i++)
            yield return TransformSystem.transforms[i].id;
    }

    public object Snapshot(int id)
    {
        TransformSystem.GetTransformIndex(id, out _, out var t);
        return new Dictionary<string, object>
        {
            ["position"]       = new[] { t.position.X, t.position.Y },
            ["scale"]          = new[] { t.scale.X, t.scale.Y },
            ["angle"]          = t.angle,
            ["parentIndex"]    = t.parentIndex,
            ["referenceCount"] = t.referenceCount,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        TransformSystem.GetTransformIndex(id, out var idx, out var t);
        switch (path)
        {
            case "position.X": t.position.X = value.GetSingle(); break;
            case "position.Y": t.position.Y = value.GetSingle(); break;
            case "scale.X":    t.scale.X    = value.GetSingle(); break;
            case "scale.Y":    t.scale.Y    = value.GetSingle(); break;
            case "angle":      t.angle      = value.GetSingle(); break;
            default: return false;
        }
        TransformSystem.transforms[idx] = t;
        return true;
    }
}

// ── Tween (mostly read-only — tweens are driven by their own system) ──

public sealed class TweenDebugProvider : IDebugProvider
{
    public string TypeName => "tween";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "isPlaying",    Type = "bool",   Label = "playing",  ReadOnly = true },
        new DebugField { Path = "interpolator", Type = "float",  Label = "progress", ReadOnly = true,
                         Min = 0f, Max = 1f },
        new DebugField { Path = "currValue",    Type = "float",  Label = "value",    ReadOnly = true },
        new DebugField { Path = "startValue",   Type = "float",  Label = "start",    ReadOnly = true },
        new DebugField { Path = "endValue",     Type = "float",  Label = "end",      ReadOnly = true },
        new DebugField { Path = "type",         Type = "string", Label = "easing",   ReadOnly = true },
        new DebugField { Path = "executionType",Type = "string", Label = "mode",     ReadOnly = true },
    };

    public IEnumerable<int> ListIds()
    {
        for (var i = 0; i < TweenSystem.tweenCount; i++) yield return TweenSystem.tweens[i].id;
    }

    public object Snapshot(int id)
    {
        TweenSystem.GetTweenIndex(id, out _, out var t);
        return new Dictionary<string, object>
        {
            ["isPlaying"]     = t.isPlaying,
            ["interpolator"]  = t.interpolator,
            ["currValue"]     = t.currValue,
            ["startValue"]    = t.startValue,
            ["endValue"]      = t.endValue,
            ["type"]          = t.type.ToString(),
            ["executionType"] = t.executionType.ToString(),
        };
    }

    public bool Apply(int id, string path, JsonElement value) => false; // read-only for now
}

// ── Collider ──────────────────────────────────────────────────────────

public sealed class ColliderDebugProvider : IDebugProvider
{
    public string TypeName => "collider";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "position", Type = "vec2", Label = "position",
                         Min = -10000f, Max = 10000f },
        new DebugField { Path = "size",     Type = "vec2", Label = "size",
                         Min = 0f, Max = 10000f },
        new DebugField { Path = "targetTransformId", Type = "int", Label = "targetTransformId",
                         ReferenceType = "transform",
                         Min = 0f, Max = 100000f },
        new DebugField { Path = "computedPosition", Type = "vec2", Label = "computed pos", ReadOnly = true },
        new DebugField { Path = "computedSize",     Type = "vec2", Label = "computed size", ReadOnly = true },
        // Gizmo overlay — see SpriteDebugProvider for the rationale; same
        // pattern, different GizmoSystem dictionary.
        new DebugField { Path = "gizmo",          Type = "bool",  Label = "gizmo" },
        new DebugField { Path = "gizmoColor",     Type = "color", Label = "gizmo color" },
        new DebugField { Path = "gizmoThickness", Type = "float", Label = "gizmo thickness",
                         Min = 0f, Max = 8f },
    };

    public IEnumerable<int> ListIds()
    {
        for (var i = 0; i < CollisionSystem.AabbsCount; i++)
            yield return CollisionSystem.aabbs[i].id;
    }

    public object Snapshot(int id)
    {
        CollisionSystem.GetColliderIndex(id, out _, out var c);
        var hasGizmo = GizmoSystem.colliderGizmos.TryGetValue(id, out var giz);
        return new Dictionary<string, object>
        {
            ["position"]          = new[] { c.position.X, c.position.Y },
            ["size"]              = new[] { c.size.X, c.size.Y },
            ["targetTransformId"] = c.targetTransformId,
            ["computedPosition"]  = new[] { c.computedPosition.X, c.computedPosition.Y },
            ["computedSize"]      = new[] { c.computedSize.X, c.computedSize.Y },
            ["gizmo"]             = hasGizmo,
            ["gizmoColor"]        = DebugColor.Pack(hasGizmo ? giz.color : GizmoSystem.DefaultColor),
            ["gizmoThickness"]    = hasGizmo ? giz.thickness : GizmoSystem.DefaultThickness,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        CollisionSystem.GetColliderIndex(id, out var idx, out var c);
        switch (path)
        {
            case "position.X": c.position.X = value.GetSingle(); break;
            case "position.Y": c.position.Y = value.GetSingle(); break;
            case "size.X":     c.size.X     = value.GetSingle(); break;
            case "size.Y":     c.size.Y     = value.GetSingle(); break;
            case "targetTransformId": c.targetTransformId = value.GetInt32(); break;
            // Gizmo controls — mirror `enable/disable collider gizmo` +
            // `set collider gizmo color/thickness`.
            case "gizmo":
                if (value.GetBoolean()) {
                    if (!GizmoSystem.colliderGizmos.ContainsKey(id)) {
                        GizmoSystem.colliderGizmos[id] = new ColliderGizmo {
                            color = GizmoSystem.DefaultColor,
                            thickness = GizmoSystem.DefaultThickness,
                        };
                    }
                } else {
                    GizmoSystem.colliderGizmos.Remove(id);
                }
                return true;
            case "gizmoColor": {
                if (!GizmoSystem.colliderGizmos.TryGetValue(id, out var g)) {
                    g = new ColliderGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.color = DebugColor.Unpack(value.GetInt32());
                GizmoSystem.colliderGizmos[id] = g;
                return true;
            }
            case "gizmoThickness": {
                if (!GizmoSystem.colliderGizmos.TryGetValue(id, out var g)) {
                    g = new ColliderGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.thickness = value.GetSingle();
                GizmoSystem.colliderGizmos[id] = g;
                return true;
            }
            default: return false;
        }
        CollisionSystem.aabbs[idx] = c;
        return true;
    }
}

// ── Text sprite ───────────────────────────────────────────────────────

public sealed class TextDebugProvider : IDebugProvider
{
    public string TypeName => "text";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "text",     Type = "string", Label = "text" },
        new DebugField { Path = "color",    Type = "color",  Label = "color" },
        new DebugField { Path = "position", Type = "vec2",   Label = "position",
                         Min = -10000f, Max = 10000f },
        new DebugField { Path = "scale",    Type = "vec2",   Label = "scale",
                         Min = -100f, Max = 100f },
        new DebugField { Path = "hidden",   Type = "bool",   Label = "hidden" },
        new DebugField { Path = "zOrder",   Type = "int",    Label = "z-order",
                         Min = -1000f, Max = 1000f },
        new DebugField { Path = "fontId",   Type = "int",    Label = "fontId",
                         ReferenceType = "texture", ReadOnly = true },
        new DebugField { Path = "dropShadowEnabled", Type = "bool", Label = "drop shadow" },
        // Gizmo overlay — see SpriteDebugProvider for the rationale; same
        // pattern, different GizmoSystem dictionary.
        new DebugField { Path = "gizmo",          Type = "bool",  Label = "gizmo" },
        new DebugField { Path = "gizmoColor",     Type = "color", Label = "gizmo color" },
        new DebugField { Path = "gizmoThickness", Type = "float", Label = "gizmo thickness",
                         Min = 0f, Max = 8f },
    };

    public IEnumerable<int> ListIds()
    {
        for (var i = 0; i < TextSystem.textSpriteCount; i++)
            yield return TextSystem.textSprites[i].sprite.id;
    }

    public object Snapshot(int id)
    {
        TextSystem.GetTextSpriteIndex(id, out _, out var ts);
        var hasGizmo = GizmoSystem.textGizmos.TryGetValue(id, out var giz);
        return new Dictionary<string, object>
        {
            ["text"]    = ts.text ?? "",
            ["color"]   = DebugColor.Pack(ts.sprite.color),
            ["position"]= new[] { ts.sprite.position.X, ts.sprite.position.Y },
            ["scale"]   = new[] { ts.sprite.scale.X, ts.sprite.scale.Y },
            ["hidden"]  = ts.sprite.hidden,
            ["zOrder"]  = ts.sprite.zOrder,
            ["fontId"]  = ts.sprite.imageId,
            ["dropShadowEnabled"] = ts.dropShadowEnabled,
            ["gizmo"]         = hasGizmo,
            ["gizmoColor"]    = DebugColor.Pack(hasGizmo ? giz.color : GizmoSystem.DefaultColor),
            ["gizmoThickness"]= hasGizmo ? giz.thickness : GizmoSystem.DefaultThickness,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        TextSystem.GetTextSpriteIndex(id, out var idx, out var ts);
        switch (path)
        {
            case "text":               ts.text = value.GetString() ?? ""; break;
            case "position.X":         ts.sprite.position.X = value.GetSingle(); break;
            case "position.Y":         ts.sprite.position.Y = value.GetSingle(); break;
            case "scale.X":            ts.sprite.scale.X    = value.GetSingle(); break;
            case "scale.Y":            ts.sprite.scale.Y    = value.GetSingle(); break;
            case "hidden":             ts.sprite.hidden     = value.GetBoolean(); break;
            case "zOrder":             ts.sprite.zOrder     = value.GetInt32(); break;
            case "dropShadowEnabled":  ts.dropShadowEnabled = value.GetBoolean(); break;
            case "color":              ts.sprite.color = DebugColor.Unpack(value.GetInt32()); break;
            // Gizmo controls — mirror `enable/disable text gizmo` +
            // `set text gizmo color/thickness`.
            case "gizmo":
                if (value.GetBoolean()) {
                    if (!GizmoSystem.textGizmos.ContainsKey(id)) {
                        GizmoSystem.textGizmos[id] = new TextGizmo {
                            color = GizmoSystem.DefaultColor,
                            thickness = GizmoSystem.DefaultThickness,
                        };
                    }
                } else {
                    GizmoSystem.textGizmos.Remove(id);
                }
                return true;
            case "gizmoColor": {
                if (!GizmoSystem.textGizmos.TryGetValue(id, out var g)) {
                    g = new TextGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.color = DebugColor.Unpack(value.GetInt32());
                GizmoSystem.textGizmos[id] = g;
                return true;
            }
            case "gizmoThickness": {
                if (!GizmoSystem.textGizmos.TryGetValue(id, out var g)) {
                    g = new TextGizmo { color = GizmoSystem.DefaultColor, thickness = GizmoSystem.DefaultThickness };
                }
                g.thickness = value.GetSingle();
                GizmoSystem.textGizmos[id] = g;
                return true;
            }
            default: return false;
        }
        TextSystem.textSprites[idx] = ts;
        return true;
    }
}

// ── SFX instance ──────────────────────────────────────────────────────
// Sounds bind to their underlying SoundEffectInstance; Apply writes
// directly to the instance properties (Pitch/Pan/Volume/IsLooped).

public sealed class SfxDebugProvider : IDebugProvider
{
    public string TypeName => "sfx";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "state",   Type = "string", Label = "state",   ReadOnly = true },
        new DebugField { Path = "pitch",   Type = "float",  Label = "pitch",   Min = -1f, Max = 1f },
        new DebugField { Path = "pan",     Type = "float",  Label = "pan",     Min = -1f, Max = 1f },
        new DebugField { Path = "volume",  Type = "float",  Label = "volume",  Min = 0f,  Max = 1f },
        new DebugField { Path = "isLooped",Type = "bool",   Label = "looped" },
    };

    public IEnumerable<int> ListIds()
    {
        foreach (var s in AudioInstanceSystem.audioEffects) yield return s.id;
    }

    public object Snapshot(int id)
    {
        AudioInstanceSystem.GetAudioEffectIndex(id, out _, out var s);
        if (s.instance == null)
        {
            return new Dictionary<string, object>
            {
                ["state"] = "(no instance)",
                ["pitch"] = 0f, ["pan"] = 0f, ["volume"] = 0f, ["isLooped"] = false,
            };
        }
        return new Dictionary<string, object>
        {
            ["state"]    = s.instance.State.ToString(),
            ["pitch"]    = s.instance.Pitch,
            ["pan"]      = s.instance.Pan,
            ["volume"]   = s.instance.Volume,
            ["isLooped"] = s.instance.IsLooped,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        AudioInstanceSystem.GetAudioEffectIndex(id, out _, out var s);
        if (s.instance == null) return false;
        switch (path)
        {
            case "pitch":    s.instance.Pitch    = value.GetSingle(); return true;
            case "pan":      s.instance.Pan      = value.GetSingle(); return true;
            case "volume":   s.instance.Volume   = value.GetSingle(); return true;
            case "isLooped": s.instance.IsLooped = value.GetBoolean(); return true;
            default: return false;
        }
    }
}

// ── Texture (read-only metadata) ──────────────────────────────────────

public sealed class TextureDebugProvider : IDebugProvider
{
    public string TypeName => "texture";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "preview",    Type = "image",  Label = "preview", ReadOnly = true },
        new DebugField { Path = "width",      Type = "int",    Label = "width",   ReadOnly = true },
        new DebugField { Path = "height",     Type = "int",    Label = "height",  ReadOnly = true },
        new DebugField { Path = "format",     Type = "string", Label = "format",  ReadOnly = true },
        new DebugField { Path = "path",       Type = "string", Label = "path",    ReadOnly = true },
        new DebugField { Path = "frameCount", Type = "int",    Label = "frames",  ReadOnly = true },
    };

    public IEnumerable<int> ListIds()
    {
        foreach (var t in TextureSystem.textures) yield return t.id;
    }

    public object Snapshot(int id)
    {
        TextureSystem.GetTextureIndex(id, out _, out var rt);
        var tex = rt.texture;
        return new Dictionary<string, object>
        {
            ["preview"]    = TexturePreview.TryEncode(tex) ?? "",
            ["width"]      = tex?.Width ?? 0,
            ["height"]     = tex?.Height ?? 0,
            ["format"]     = tex?.Format.ToString() ?? "(not loaded)",
            ["path"]       = rt.descriptor.imageFilePath ?? "",
            ["frameCount"] = rt.descriptor.frames?.Count ?? 0,
        };
    }

    public bool Apply(int id, string path, JsonElement value) => false;

    // Surfaces the original asset path in the inspector's texture
    // dropdowns (sprite imageId, render-output targetTextureId, etc.)
    // — "Images/Player" reads much better than "texture #1". Returns
    // null when no path is registered so the picker falls back to the
    // numeric label.
    public string GetLabel(int id)
    {
        TextureSystem.GetTextureIndex(id, out var idx, out var rt);
        if (idx < 0) return null;
        var p = rt.descriptor.imageFilePath;
        return string.IsNullOrEmpty(p) ? null : p;
    }
}

// ── Render output ─────────────────────────────────────────────────────

public sealed class RenderOutputDebugProvider : IDebugProvider
{
    public string TypeName => "renderOutput";

    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        // Render-target preview at the top so the user can SEE what
        // the output is drawing each frame. Refreshes through the
        // entity-folder poll while the folder is expanded.
        new DebugField { Path = "preview",     Type = "image", Label = "preview", ReadOnly = true },
        new DebugField { Path = "clearColor",  Type = "color", Label = "clearColor" },
        new DebugField { Path = "clearTarget", Type = "bool",  Label = "clearTarget" },
        new DebugField { Path = "itemCount",   Type = "int",   Label = "items",       ReadOnly = true },
        new DebugField { Path = "targetTextureId", Type = "int", Label = "targetTexture", ReadOnly = true,
                         ReferenceType = "texture" },
        new DebugField { Path = "width",       Type = "int",   Label = "width",       ReadOnly = true },
        new DebugField { Path = "height",      Type = "int",   Label = "height",      ReadOnly = true },
    };

    public IEnumerable<int> ListIds()
    {
        foreach (var o in RenderSystem.outputs) yield return o.id;
    }

    public object Snapshot(int id)
    {
        RenderSystem.GetOutputIndex(id, out _, out var o);
        var hasTarget = o.target != null;
        return new Dictionary<string, object>
        {
            ["preview"]         = hasTarget ? (TexturePreview.TryEncode(o.target) ?? "") : "",
            ["clearColor"]      = DebugColor.Pack(o.clearColor),
            ["clearTarget"]     = o.clearTarget,
            ["itemCount"]       = o.orderedItems.Count,
            ["targetTextureId"] = o.targetTextureId,
            ["width"]           = hasTarget ? o.target.Width  : 0,
            ["height"]          = hasTarget ? o.target.Height : 0,
        };
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        RenderSystem.GetOutputIndex(id, out var idx, out var o);
        switch (path)
        {
            case "clearTarget": o.clearTarget = value.GetBoolean(); break;
            case "clearColor":   o.clearColor = DebugColor.Unpack(value.GetInt32()); break;
            default: return false;
        }
        RenderSystem.outputs[idx] = o;
        return true;
    }
}

// ── Effect (shader) — read-only parameter dump ────────────────────────

public sealed class EffectDebugProvider : IDebugProvider
{
    public string TypeName => "effect";

    // Static fields visible on every effect. Per-shader parameters
    // come in via SchemaFor(id) below — they vary per loaded effect
    // file so they can't live in the static Schema list.
    public IReadOnlyList<DebugField> Schema { get; } = new[]
    {
        new DebugField { Path = "name",       Type = "string", Label = "name",         ReadOnly = true },
        new DebugField { Path = "filePath",   Type = "string", Label = "file",         ReadOnly = true },
        new DebugField { Path = "paramCount", Type = "int",    Label = "param count",  ReadOnly = true },
    };

    // Path prefix used to encode shader-parameter access. We deliberately
    // avoid a dot here because dotted paths already have meaning (nested
    // vec2/color components). "param/" is unambiguous.
    const string ParamPrefix = "param/";

    public IReadOnlyList<DebugField> SchemaFor(int id)
    {
        // Static fields first, then one field per editable Single
        // (Float1-4) parameter on the shader. Texture/Matrix params
        // surface as read-only labels — full editing is doable but
        // we'd need a way to pick textures by id (matches the sprite
        // imageId combo work) which can come in a follow-up.
        var fields = new List<DebugField>(Schema);
        RenderSystem.GetEffectIndex(id, out _, out var fx);
        if (fx.effect == null) return fields;

        foreach (var p in fx.effect.Parameters)
        {
            switch (p.ParameterType)
            {
                case Microsoft.Xna.Framework.Graphics.EffectParameterType.Single:
                    switch (p.ColumnCount)
                    {
                        case 1:
                            fields.Add(new DebugField {
                                Path = ParamPrefix + p.Name, Type = "float",
                                Label = p.Name, Min = 0f, Max = 1f,
                            });
                            break;
                        case 2:
                            fields.Add(new DebugField {
                                Path = ParamPrefix + p.Name, Type = "vec2",
                                Label = p.Name, Min = 0f, Max = 1f,
                            });
                            break;
                        case 3:
                        case 4:
                            // Vec3/Vec4 — render as vec3 for the panel
                            // since Tweakpane vec4 maps poorly without
                            // a color hint. Vec4 params usually ARE
                            // colors; if the shader names them
                            // accordingly we could heuristically pick
                            // "color" instead.
                            fields.Add(new DebugField {
                                Path = ParamPrefix + p.Name, Type = "vec3",
                                Label = p.Name, Min = 0f, Max = 1f,
                            });
                            break;
                        default:
                            fields.Add(new DebugField {
                                Path = ParamPrefix + p.Name, Type = "string",
                                Label = p.Name, ReadOnly = true,
                            });
                            break;
                    }
                    break;
                default:
                    // Texture / matrix / etc. — read-only label.
                    fields.Add(new DebugField {
                        Path = ParamPrefix + p.Name, Type = "string",
                        Label = p.Name, ReadOnly = true,
                    });
                    break;
            }
        }
        return fields;
    }

    public IEnumerable<int> ListIds()
    {
        foreach (var e in RenderSystem.effects) yield return e.id;
    }

    public object Snapshot(int id)
    {
        RenderSystem.GetEffectIndex(id, out _, out var fx);
        var snap = new Dictionary<string, object>
        {
            ["name"]       = fx.effect?.Name ?? "(unloaded)",
            ["filePath"]   = fx.filePath ?? "",
            ["paramCount"] = fx.effect?.Parameters.Count ?? 0,
        };
        if (fx.effect == null) return snap;

        // Read current values for each parameter — these flow into the
        // dynamic fields built by SchemaFor.
        foreach (var p in fx.effect.Parameters)
        {
            var key = ParamPrefix + p.Name;
            if (p.ParameterType != Microsoft.Xna.Framework.Graphics.EffectParameterType.Single)
            {
                snap[key] = "<" + p.ParameterType + ">";
                continue;
            }
            // Matrices and array-typed params have ColumnCount values that
            // collide with vector dispatch but can't be read via the
            // Vector{N} accessors. Surface them as descriptive strings so
            // the debug panel renders SOMETHING (rather than crashing the
            // whole snapshot and zeroing every field). The user-facing
            // sprite-fx template uses `float4x4 MatrixTransform` which
            // lands here.
            if (p.ParameterClass == Microsoft.Xna.Framework.Graphics.EffectParameterClass.Matrix)
            {
                snap[key] = "<matrix" + p.RowCount + "x" + p.ColumnCount + ">";
                continue;
            }
            switch (p.ColumnCount)
            {
                case 1: snap[key] = p.GetValueSingle(); break;
                case 2: { var v = p.GetValueVector2(); snap[key] = new[] { v.X, v.Y }; break; }
                case 3: { var v = p.GetValueVector3(); snap[key] = new[] { v.X, v.Y, v.Z }; break; }
                case 4: { var v = p.GetValueVector4(); snap[key] = new[] { v.X, v.Y, v.Z, v.W }; break; }
                default: snap[key] = "(unsupported col " + p.ColumnCount + ")"; break;
            }
        }
        return snap;
    }

    public bool Apply(int id, string path, JsonElement value)
    {
        if (path == null || !path.StartsWith(ParamPrefix)) return false;
        RenderSystem.GetEffectIndex(id, out _, out var fx);
        if (fx.effect == null) return false;

        // Split "param/diffuseColor.X" → ("diffuseColor", "X") so we can
        // address one component of a vec2/vec3 like the sprite provider.
        var rest = path.Substring(ParamPrefix.Length);
        var dotIdx = rest.IndexOf('.');
        var paramName = dotIdx >= 0 ? rest.Substring(0, dotIdx) : rest;
        var component = dotIdx >= 0 ? rest.Substring(dotIdx + 1) : null;

        var param = fx.effect.Parameters[paramName];
        if (param == null) return false;

        // Matrix params are read-only in the debug panel (Snapshot exposes
        // them as a placeholder string). Reject writes early so we don't
        // misroute through the column-count switch and corrupt the value.
        if (param.ParameterClass == Microsoft.Xna.Framework.Graphics.EffectParameterClass.Matrix)
            return false;

        try
        {
            switch (param.ColumnCount)
            {
                case 1:
                    param.SetValue(value.GetSingle());
                    return true;
                case 2: {
                    var v = param.GetValueVector2();
                    if (component == "X") v.X = value.GetSingle();
                    else if (component == "Y") v.Y = value.GetSingle();
                    else return false;
                    param.SetValue(v);
                    return true;
                }
                case 3: {
                    var v = param.GetValueVector3();
                    if (component == "X") v.X = value.GetSingle();
                    else if (component == "Y") v.Y = value.GetSingle();
                    else if (component == "Z") v.Z = value.GetSingle();
                    else return false;
                    param.SetValue(v);
                    return true;
                }
                case 4: {
                    var v = param.GetValueVector4();
                    if (component == "X") v.X = value.GetSingle();
                    else if (component == "Y") v.Y = value.GetSingle();
                    else if (component == "Z") v.Z = value.GetSingle();
                    else if (component == "W") v.W = value.GetSingle();
                    else return false;
                    param.SetValue(v);
                    return true;
                }
                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[debug] effect param set failed: " + e.Message);
            return false;
        }
    }
}
