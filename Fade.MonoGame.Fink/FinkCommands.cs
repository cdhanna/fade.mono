using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Fink;

/// <summary>
/// Commands for Fink atlases. Clips and palette rows are numbers: use the TAG_ and PAL_
/// constants from the fink_&lt;atlas&gt;.fbasic that Fink generates.
/// </summary>
public partial class FadeFinkCommands
{
    /// <summary>
    /// <para>Loads a Fink atlas by its content path, and returns its id. Use the generated
    /// fink_load_&lt;atlas&gt;() function instead of calling this yourself: it passes the right
    /// path and stamp.</para>
    /// </summary>
    /// <param name="contentPath">The atlas's path under Assets, without an extension, e.g. "Textures/sprites".</param>
    /// <param name="stamp">The stamp the generated constants were made with (0 to skip the check).</param>
    /// <returns>The atlas id (1 or more), or 0 when it couldn't be loaded.</returns>
    [FadeBasicCommand("fink atlas load")]
    public static int LoadAtlas(string contentPath, int stamp) => FinkSystem.Load(contentPath, stamp);

    /// <summary>
    /// <para>Plays a clip on a sprite, from its first frame. The sprite's texture, effect, frame,
    /// origin and texcoord1.x are set by the clip from now on; position, scale, rotation, color,
    /// order and visibility stay yours.</para>
    /// </summary>
    /// <param name="spriteId">Any sprite (made with `sprite` first, for its position).</param>
    /// <param name="clipId">A TAG_ constant.</param>
    [FadeBasicCommand("play fink sprite")]
    public static void PlaySprite(int spriteId, int clipId) => FinkSystem.Play(spriteId, clipId);

    /// <summary>Plays a clip unless the sprite is already playing it (for calling every frame).</summary>
    [FadeBasicCommand("loop fink sprite")]
    public static void LoopSprite(int spriteId, int clipId)
    {
        if (FinkSystem.Anims.TryGetValue(spriteId, out var a) && a.Clip.Id == clipId && a.Playing && !a.Done) return;
        FinkSystem.Play(spriteId, clipId);
    }

    /// <summary>Stops the sprite's clip where it is. It keeps showing that frame.</summary>
    [FadeBasicCommand("stop fink sprite")]
    public static void StopSprite(int spriteId)
    {
        if (FinkSystem.Anims.TryGetValue(spriteId, out var a)) a.Playing = false;
    }

    /// <summary>Carries on a stopped clip.</summary>
    [FadeBasicCommand("resume fink sprite")]
    public static void ResumeSprite(int spriteId)
    {
        if (FinkSystem.Anims.TryGetValue(spriteId, out var a)) a.Playing = true;
    }

    /// <summary>
    /// <para>Stretches a 9-slice sprite (a panel, a button) to a size, in its drawing's pixels
    /// (the sprite's scale still applies on top). The corners stay as drawn, the edges stretch one
    /// way and the middle both. 0, 0 goes back to the size it was drawn at. Only assets given a
    /// 9-slice in Fink stretch; others ignore this.</para>
    /// <para>It's drawn with 8 extra sprites that copy this one's position, rotation, color, order
    /// and visibility every frame. Flipping isn't supported while stretched.</para>
    /// </summary>
    /// <param name="spriteId">A sprite playing a clip of a 9-slice asset.</param>
    /// <param name="width">Width in the drawing's pixels (at least its left and right insets).</param>
    /// <param name="height">Height in the drawing's pixels (at least its top and bottom insets).</param>
    [FadeBasicCommand("set fink sprite size")]
    public static void SetSpriteSize(int spriteId, float width, float height)
    {
        var a = FinkSystem.StateOf(spriteId);
        a.SizeW = Math.Max(0, width);
        a.SizeH = Math.Max(0, height);
    }

    /// <summary>Lets go of the sprite: the clip stops changing it.</summary>
    [FadeBasicCommand("release fink sprite")]
    public static void ReleaseSprite(int spriteId)
    {
        if (FinkSystem.Anims.TryGetValue(spriteId, out var playing))
        {
            FinkSystem.HideHelpers(playing);
            FinkSystem.HidePieces(playing);
        }
        FinkSystem.Anims.Remove(spriteId);
        FinkSystem.Pending.Remove(spriteId);
    }

    /// <summary>
    /// <para>Shows a part (a PART_ constant) of the sprite's drawing. When the part is one of a
    /// slot's variants (faces, helmets), it's shown instead of the others in that slot. Parts are
    /// layer groups in Fink, drawn on top of each other in layer order; the choice stays when
    /// the sprite plays another clip, so equipment stays on through walk, run and jump.</para>
    /// <para>Each part is an extra sprite that copies this one's position, rotation, scale,
    /// flip, color, order and visibility. Two layered sprites that overlap should have different
    /// orders, or their parts can mix.</para>
    /// </summary>
    /// <param name="spriteId">Any sprite (settings made before a clip plays are kept).</param>
    /// <param name="partId">A PART_ constant.</param>
    [FadeBasicCommand("set fink part")]
    public static void SetPart(int spriteId, int partId)
    {
        var a = FinkSystem.StateOf(spriteId);
        a.HiddenParts.Remove(partId);
        if (FinkSystem.FindPart(partId) is { } part && !string.IsNullOrEmpty(part.Slot)) a.SlotChoice[part.Slot] = partId;
    }

    /// <summary>
    /// Shows (1) or hides (0) one part of the sprite's drawing: a cape, or a slot's variant
    /// (hiding it leaves that slot empty). Picking a part with set fink part shows it again.
    /// </summary>
    [FadeBasicCommand("set fink part visible")]
    public static void SetPartVisible(int spriteId, int partId, int visible)
    {
        var a = FinkSystem.StateOf(spriteId);
        if (visible != 0) a.HiddenParts.Remove(partId);
        else a.HiddenParts.Add(partId);
    }

    /// <summary>1 when the part (a PART_ constant) is drawn on the sprite now: not hidden, and the chosen variant of its slot.</summary>
    [FadeBasicCommand("fink part visible")]
    public static int PartVisible(int spriteId, int partId)
    {
        if (!FinkSystem.Anims.TryGetValue(spriteId, out var a) || !a.Atlas.Assets.TryGetValue(a.Clip.Asset, out var asset) || asset.Parts == null) return 0;
        int i = asset.Parts.FindIndex(p => p.Id == partId);
        return i >= 0 && FinkSystem.PartShown(a, asset, i) ? 1 : 0;
    }

    /// <summary>Draws the sprite with another palette row (a PAL_ constant).</summary>
    [FadeBasicCommand("set fink sprite palette")]
    public static void SetSpritePalette(int spriteId, int paletteRow) => FinkSystem.StateOf(spriteId).PaletteRow = paletteRow;

    /// <summary>How fast the clip plays: 1 as drawn, 2 twice as fast, 0.5 half.</summary>
    [FadeBasicCommand("set fink sprite speed")]
    public static void SetSpriteSpeed(int spriteId, float speed) => FinkSystem.StateOf(spriteId).Speed = Math.Max(0, speed);

    /// <summary>
    /// Pins a point of the drawing's canvas to the sprite's position, as a ratio (0.5, 1 is the
    /// bottom middle), instead of each frame's pivot from Fink. A negative x goes back to the pivot.
    /// </summary>
    [FadeBasicCommand("set fink sprite anchor")]
    public static void SetSpriteAnchor(int spriteId, float x, float y)
    {
        var a = FinkSystem.StateOf(spriteId);
        a.AnchorSet = x >= 0;
        a.AnchorX = x;
        a.AnchorY = y;
    }

    /// <summary>Where a named point (a POINT_ constant) of the sprite's drawing is on screen: x.</summary>
    [FadeBasicCommand("fink sprite point x")]
    public static float SpritePointX(int spriteId, int pointId) => FinkSystem.PointOf(spriteId, pointId)?.X ?? 0;

    /// <summary>Where a named point (a POINT_ constant) of the sprite's drawing is on screen: y.</summary>
    [FadeBasicCommand("fink sprite point y")]
    public static float SpritePointY(int spriteId, int pointId) => FinkSystem.PointOf(spriteId, pointId)?.Y ?? 0;

    /// <summary>
    /// The texture id of one of an atlas's data maps (a MAP_ constant), for
    /// `set effect param texture`. 0 if there's no such map.
    /// </summary>
    [FadeBasicCommand("fink atlas map")]
    public static int AtlasMap(int atlasId, int map) =>
        FinkSystem.Atlases.FirstOrDefault(a => a.Id == atlasId) is { } atlas && map >= 0 && map < atlas.MapTextures.Count
            ? atlas.MapTextures[map] : 0;

    /// <summary>The texture id of an atlas's palette (256 colors per row), for your own shaders.</summary>
    [FadeBasicCommand("fink atlas palette")]
    public static int AtlasPalette(int atlasId) => FinkSystem.Atlases.FirstOrDefault(a => a.Id == atlasId)?.PaletteTexture ?? 0;

    /// <summary>The clip the sprite is playing (a TAG_ value), or 0.</summary>
    [FadeBasicCommand("fink sprite clip")]
    public static int SpriteClip(int spriteId) => FinkSystem.Anims.TryGetValue(spriteId, out var a) ? a.Clip.Id : 0;

    /// <summary>1 while the sprite's clip is running (not stopped, not finished).</summary>
    [FadeBasicCommand("fink sprite playing")]
    public static int SpritePlaying(int spriteId) => FinkSystem.Anims.TryGetValue(spriteId, out var a) && a.Playing && !a.Done ? 1 : 0;

    /// <summary>1 once a clip that plays once has reached its end.</summary>
    [FadeBasicCommand("fink sprite done")]
    public static int SpriteDone(int spriteId) => FinkSystem.Anims.TryGetValue(spriteId, out var a) && a.Done ? 1 : 0;

    /// <summary>Which frame of its clip the sprite is on, from 0.</summary>
    [FadeBasicCommand("fink sprite frame")]
    public static int SpriteFrame(int spriteId) => FinkSystem.Anims.TryGetValue(spriteId, out var a) ? a.Index : 0;

    /// <summary>Jumps to a frame of the sprite's clip.</summary>
    [FadeBasicCommand("set fink sprite frame")]
    public static void SetSpriteFrame(int spriteId, int frame)
    {
        if (!FinkSystem.Anims.TryGetValue(spriteId, out var a) || a.Clip.Frames.Count == 0) return;
        a.Index = Math.Clamp(frame, 0, a.Clip.Frames.Count - 1);
        a.Ms = 0;
        a.Done = false;
    }

    /// <summary>How many frames a clip has.</summary>
    [FadeBasicCommand("fink clip frames")]
    public static int ClipFrames(int clipId) => FinkSystem.Clips.TryGetValue(clipId, out var c) ? c.clip.Frames.Count : 0;

    /// <summary>How long a clip runs once through, in milliseconds.</summary>
    [FadeBasicCommand("fink clip ms")]
    public static int ClipMs(int clipId) => FinkSystem.Clips.TryGetValue(clipId, out var c) ? c.clip.Frames.Sum(f => f.Ms) : 0;

    /// <summary>How many palette rows (PAL_ values) an atlas has.</summary>
    [FadeBasicCommand("fink palette rows")]
    public static int PaletteRows(int atlasId) =>
        FinkSystem.Atlases.FirstOrDefault(a => a.Id == atlasId)?.Manifest.PaletteRows.Count ?? 0;
}
