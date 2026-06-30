// Cross-platform abstraction for inspector-style debug access to a
// game system (SpriteSystem, TransformSystem, etc.). Each system that
// wants to surface state to a debug UI implements one IDebugProvider.
//
// Why this exists: the JS-based debug inspector in the Playground
// (Tweakpane panel) needs to enumerate entities, read their fields,
// and write edits back — without the C# side having to grow one-off
// JSInvokable per inspector. Instead the Playground talks to a
// generic RPC ("get sprite 5", "set sprite 5 position.X to 42") and
// providers map that to their own system's storage.
//
// The same abstraction works in exported standalone games (the
// Tweakpane bundle is shipped with the export, talks to the same C#
// surface directly via IJSInProcessRuntime). Desktop builds can opt
// in later if/when we retire ImGui — for now desktop continues to
// access SpriteSystem.sprites[] directly.

using System.Collections.Generic;
using System.Text.Json;

namespace Fade.MonoGame.Core.Debug;

/// <summary>
/// Describes one editable/displayable field on an entity. Consumed by
/// the JS panel to decide which Tweakpane binding to create (color
/// picker vs slider vs checkbox) and what limits to apply.
/// </summary>
public sealed class DebugField
{
    /// <summary>Dotted path into the snapshot object, e.g. "position.X".</summary>
    public string Path { get; init; } = "";

    /// <summary>
    /// One of: "int", "float", "bool", "string", "vec2", "vec3", "color",
    /// "image". Drives the panel's widget choice (color → swatch picker,
    /// vec2 → pair of sliders, image → embedded thumbnail, etc.).
    /// </summary>
    public string Type { get; init; } = "string";

    /// <summary>
    /// When set, the panel renders the field as a dropdown sourced from
    /// ListIds() of the provider with this TypeName. Used for foreign-
    /// key fields like Sprite.imageId pointing into the Texture system.
    /// Only meaningful when Type == "int".
    /// </summary>
    public string ReferenceType { get; init; }

    /// <summary>User-facing label. Defaults to Path if empty.</summary>
    public string Label { get; init; } = "";

    /// <summary>Optional min/max for numeric types.</summary>
    public float? Min { get; init; }
    public float? Max { get; init; }

    /// <summary>If true, the panel renders the field as display-only.</summary>
    public bool ReadOnly { get; init; }
}

/// <summary>
/// One provider per game system that exposes inspector data. The
/// browser-side JS bridge (in Pages/Index.razor.cs) routes generic
/// "ListEntities"/"GetEntity"/"SetField" RPC calls to the matching
/// provider via DebugRegistry.
/// </summary>
public interface IDebugProvider
{
    /// <summary>Stable identifier — "sprite", "transform", "metadata".</summary>
    string TypeName { get; }

    /// <summary>Field schema. Lets the panel build widgets without hard-coding system layout.</summary>
    IReadOnlyList<DebugField> Schema { get; }

    /// <summary>
    /// Optional per-entity schema. Default: returns the static Schema.
    /// Providers with dynamic per-id field lists (e.g. EffectDebugProvider,
    /// where each shader has its own parameter set) override this to
    /// surface those fields. The panel prefers this over Schema when
    /// fetching field metadata for one entity.
    /// </summary>
    IReadOnlyList<DebugField> SchemaFor(int id) => Schema;

    /// <summary>
    /// Iterate the current entity IDs for this provider. Singleton-style
    /// providers (e.g. "metadata") return a single 0.
    /// </summary>
    IEnumerable<int> ListIds();

    /// <summary>
    /// Take a snapshot of the entity's current state. Return shape MUST
    /// match the Schema's Path entries — top-level keys are the leftmost
    /// path segment, nested objects for dotted paths. The result is JSON-
    /// serialized to the panel.
    /// </summary>
    object Snapshot(int id);

    /// <summary>
    /// Apply one field change. Path is the dotted Path from Schema;
    /// value is the JSON-decoded edit from the panel. Return true if
    /// the path was recognized + the change applied, false if the
    /// path was unknown or invalid (the panel will log + revert).
    /// </summary>
    bool Apply(int id, string path, JsonElement value);

    /// <summary>
    /// Optional human-readable label for an entity. Used by the
    /// inspector panel's reference-type dropdowns (e.g. the texture
    /// picker on a sprite's `imageId` field) to show a meaningful
    /// name like "Images/Player" instead of "texture #1".
    /// Return null/empty to fall back to the generic "<typeName> #<id>"
    /// format.
    /// </summary>
    string GetLabel(int id) => null;
}
