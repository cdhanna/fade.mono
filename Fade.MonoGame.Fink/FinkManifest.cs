using System.Text.Json;

namespace Fade.MonoGame.Fink;

/// <summary>
/// The &lt;atlas&gt;.atlas.json Fink writes (format "fink-atlas", version 1, or 2 when an asset has parts). Everything the game
/// looks up is by number: clip ids (the TAG_ constants), cell indices, palette rows (PAL_).
/// Names are for people and error messages.
/// </summary>
public sealed class FinkManifest
{
    public string Format { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Changes whenever the manifest does; the generated constants carry the one they were made with.</summary>
    public int Stamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    /// <summary>The index texture's file name, next to the manifest.</summary>
    public string Image { get; set; } = "";
    /// <summary>The palette texture's file name, next to the manifest.</summary>
    public string Palette { get; set; } = "";
    public List<string> PaletteRows { get; set; } = new();
    /// <summary>Data maps: textures laid out like Image (MAP_ constants index this list).</summary>
    public List<FinkMap> Maps { get; set; } = new();
    public List<FinkCell> Cells { get; set; } = new();
    public List<FinkAsset> Assets { get; set; } = new();
    public List<FinkClip> Clips { get; set; } = new();

    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static FinkManifest Parse(string json)
    {
        var m = JsonSerializer.Deserialize<FinkManifest>(json, Json) ?? throw new InvalidDataException("empty manifest");
        if (m.Format != "fink-atlas") throw new InvalidDataException($"not a Fink atlas (format \"{m.Format}\")");
        if (m.Version > 2) throw new InvalidDataException($"made by a newer Fink (version {m.Version}); update Fade.MonoGame.Fink");
        return m;
    }
}

/// <summary>A picture in the atlas texture.</summary>
public sealed record FinkCell(int X, int Y, int W, int H);

/// <summary>An animation's canvas: frames are placed in it by their X/Y.</summary>
/// <param name="Slice">9-slice insets (left, top, right, bottom) when the asset stretches, else null.</param>
/// <param name="Parts">Its parts (layer groups drawn on top of each other), or null when it's one picture per frame.</param>
/// <param name="Pieces">What's stacked, bottom first: an index into Parts per piece, -1 for the base (always drawn).</param>
public sealed record FinkAsset(int Id, string Name, int Width, int Height, int Fps, List<FinkPoint>? Points, int[]? Slice = null,
    List<FinkPart>? Parts = null, int[]? Pieces = null)
{
    /// <summary>True when frames come in pieces (see <see cref="FinkFrame.Pcs"/>).</summary>
    public bool HasPieces => Parts is { Count: > 0 } && Pieces is { Length: > 0 };

    /// <summary>The insets, or null when there are none (or they're malformed).</summary>
    public (int l, int t, int r, int b)? Insets => Slice is { Length: 4 } s ? (s[0], s[1], s[2], s[3]) : null;
}

/// <summary>A part of an asset (the PART_ constant is its id). Parts with the same slot are variants: one shows.</summary>
public sealed record FinkPart(int Id, string Name, string Slot);

/// <summary>A named point of an asset (the POINT_ constant is its id).</summary>
public sealed record FinkPoint(int Id, string Name);

/// <summary>A data map: its file, and what's in each of R, G, B, A (a channel, normal.x, a number).</summary>
public sealed record FinkMap(string Name, string Image, string R, string G, string B, string A);

/// <summary>A tag, or an asset's whole timeline (empty name).</summary>
public sealed record FinkClip(int Id, int Asset, string Name, string Loop, List<FinkFrame> Frames);

/// <param name="Cell">Index into the cells; -1 shows nothing.</param>
/// <param name="X">Where the picture's left edge sits in the asset's canvas.</param>
/// <param name="Y">Where its top edge sits.</param>
/// <param name="Ms">How long it shows.</param>
/// <param name="Px">The pivot in the asset's canvas: what sits at the sprite's position.</param>
/// <param name="Py">The pivot's y.</param>
/// <param name="Pts">The asset's named points on this frame: x, y pairs in the order of its Points.</param>
/// <param name="Pcs">For an asset with parts: cell, x, y per piece, bottom first (cell -1: nothing).</param>
public sealed record FinkFrame(int Cell, int X, int Y, int Ms, int Px, int Py, int[]? Pts, int[]? Pcs = null);
