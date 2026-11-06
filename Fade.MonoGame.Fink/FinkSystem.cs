using Fade.MonoGame.Contracts;
using Fade.MonoGame.Core;
using Fade.MonoGame.Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Fade;

namespace Fade.MonoGame.Fink;

/// <summary>
/// Loads Fink atlases and plays their clips on sprites. Register one as an IFadeHostSystem; it
/// advances every playing sprite once a frame, before the program's frame, so what the program
/// sees and changes is this frame's picture.
///
/// An atlas is ordinary engine state underneath: one texture (the palette indices, with a frame
/// per picture), one palette texture and one effect, under ids from a range the program is
/// unlikely to use (see <see cref="IdBase"/>). Sprites are the program's own; playing a clip on one
/// sets its texture, effect, frame, origin and texcoord1.x (the palette row) every frame.
/// </summary>
public sealed class FinkSystem : FadeHostSystem
{
    /// <summary>Texture and effect ids for atlases start here: well away from a program's own.</summary>
    public const int IdBase = 1_900_000_000;

    internal sealed class Atlas
    {
        public int Id;
        public string ContentPath = "";
        public string? SourceJson;
        public FinkManifest Manifest = new();
        public int IndexTexture, PaletteTexture, Effect;
        /// <summary>Texture ids of the data maps, in manifest order.</summary>
        public List<int> MapTextures = new();
        public int ExpectedStamp;
        public FileSystemWatcher? Watcher;
        public volatile bool Changed;
        public Dictionary<int, FinkAsset> Assets = new();
        /// <summary>
        /// For 9-slice assets: the first of nine texture frames (the pieces, row by row) for each
        /// picture as one of the asset's frames places it.
        /// </summary>
        public Dictionary<(int asset, int cell, int x, int y), int> SliceFrames = new();
        public int EmptyFrame;
    }

    internal sealed class Anim
    {
        public int Sprite;
        public Atlas Atlas = null!;
        public FinkClip Clip = null!;
        public int Index;
        public double Ms;
        public int Direction = 1;
        public bool Playing = true;
        public bool Done;
        public float Speed = 1;
        public int PaletteRow;
        /// <summary>
        /// When set, this point of the asset's canvas (0..1 each way) sits at the sprite's
        /// position instead of each frame's pivot.
        /// </summary>
        public bool AnchorSet;
        public float AnchorX = 0.5f, AnchorY = 0.5f;
        /// <summary>A 9-slice asset's size, in its own pixels (before the sprite's scale); 0 = as drawn.</summary>
        public float SizeW, SizeH;
        /// <summary>The 8 extra sprites a stretched 9-slice is drawn with (the sprite itself is the top-left corner).</summary>
        public int[]? Helpers;

        public FinkFrame? Frame => Clip == null || Clip.Frames.Count == 0 ? null : Clip.Frames[Math.Clamp(Index, 0, Clip.Frames.Count - 1)];

        /// <summary>The point of the asset's canvas that is at the sprite's position, this frame.</summary>
        public (float x, float y) Pivot()
        {
            Atlas.Assets.TryGetValue(Clip.Asset, out var asset);
            var f = Frame;
            if (AnchorSet || f == null) return (AnchorX * (asset?.Width ?? 0), AnchorY * (asset?.Height ?? 0));
            return (f.Px, f.Py);
        }
    }

    internal static readonly List<Atlas> Atlases = new();
    internal static readonly Dictionary<int, (Atlas atlas, FinkClip clip)> Clips = new();
    internal static readonly Dictionary<int, Anim> Anims = new();
    /// <summary>Settings made before a clip was played (palette, anchor, speed) wait here.</summary>
    internal static readonly Dictionary<int, Anim> Pending = new();

    // ---------------------------------------------------------------- loading

    /// <summary>Loads an atlas by its content path (e.g. "Textures/sprites"). Returns its id (1 or more), or 0.</summary>
    internal static int Load(string contentPath, int expectedStamp)
    {
        contentPath = contentPath.Replace('\\', '/').Trim('/');
        var atlas = Atlases.FirstOrDefault(a => string.Equals(a.ContentPath, contentPath, StringComparison.OrdinalIgnoreCase));
        if (atlas == null)
        {
            atlas = new Atlas { Id = Atlases.Count + 1, ContentPath = contentPath };
            int b = IdBase + atlas.Id * 64;
            (atlas.IndexTexture, atlas.PaletteTexture, atlas.Effect) = (b, b + 1, b + 2);
            Atlases.Add(atlas);
        }
        atlas.ExpectedStamp = expectedStamp;

        // a load is a fresh start for this atlas (after F1 the sprites are new ones)
        foreach (var id in Anims.Where(kv => kv.Value.Atlas == atlas).Select(kv => kv.Key).ToList()) Anims.Remove(id);

        if (!ReadManifest(atlas)) return 0;
        var dir = contentPath.Contains('/') ? contentPath[..contentPath.LastIndexOf('/')] + "/" : "";
        FadeMonoGameCommands.LoadTexture(atlas.IndexTexture, dir + Path.GetFileNameWithoutExtension(atlas.Manifest.Image));
        FadeMonoGameCommands.LoadTexture(atlas.PaletteTexture, dir + Path.GetFileNameWithoutExtension(atlas.Manifest.Palette));
        FadeMonoGameCommands.LoadEffect(atlas.Effect, dir + atlas.Manifest.Name + "_palette");
        atlas.MapTextures.Clear();
        for (int i = 0; i < atlas.Manifest.Maps.Count && i < 60; i++)
        {
            int id = atlas.IndexTexture + 4 + i;
            FadeMonoGameCommands.LoadTexture(id, dir + Path.GetFileNameWithoutExtension(atlas.Manifest.Maps[i].Image));
            atlas.MapTextures.Add(id);
        }
        RegisterFrames(atlas);
        Watch(atlas);
        return atlas.Id;
    }

    /// <summary>
    /// Finds and reads &lt;path&gt;.atlas.json. A Debug build reads the one in the project's Assets
    /// (so a re-export from Fink shows up live); otherwise the copy next to the built content.
    /// </summary>
    private static bool ReadManifest(Atlas atlas)
    {
        var rel = atlas.ContentPath + ".atlas.json";
        var candidates = new List<string>();
        var root = SafeRoot();
        if (!string.IsNullOrEmpty(root)) candidates.Add(Path.Combine(root, "Assets", rel));
        try { candidates.Add(Path.Combine(GameSystem.game.Content.RootDirectory is { } r && Path.IsPathRooted(r) ? r : Path.Combine(AppContext.BaseDirectory, GameSystem.game.Content.RootDirectory ?? "Content"), rel)); }
        catch { /* no game yet */ }
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Content", rel));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Content", rel));

        var file = candidates.FirstOrDefault(File.Exists);
        if (file == null)
        {
            Console.Error.WriteLine($"[fink] no atlas manifest for \"{atlas.ContentPath}\": looked for {string.Join(", ", candidates)}");
            return false;
        }
        try
        {
            atlas.Manifest = FinkManifest.Parse(File.ReadAllText(file));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[fink] couldn't read {file}: {e.Message}");
            return false;
        }
        atlas.SourceJson = !string.IsNullOrEmpty(root) && file.StartsWith(Path.Combine(root, "Assets"), StringComparison.OrdinalIgnoreCase) ? file : null;
        if (atlas.ExpectedStamp != 0 && atlas.ExpectedStamp != atlas.Manifest.Stamp)
            Console.Error.WriteLine($"[fink] atlas \"{atlas.Manifest.Name}\" was exported again since its fink_{atlas.Manifest.Name}.fbasic was compiled. " +
                                    "Rebuild (or F1) so the TAG_ and PAL_ constants match it.");

        atlas.Assets = atlas.Manifest.Assets.ToDictionary(a => a.Id);
        foreach (var key in Clips.Where(kv => kv.Value.atlas == atlas).Select(kv => kv.Key).ToList()) Clips.Remove(key);
        foreach (var clip in atlas.Manifest.Clips)
        {
            if (Clips.TryGetValue(clip.Id, out var other) && other.atlas != atlas)
                Console.Error.WriteLine($"[fink] clip {clip.Id} is in both \"{other.atlas.Manifest.Name}\" and \"{atlas.Manifest.Name}\"; using the latter.");
            Clips[clip.Id] = (atlas, clip);
        }
        // sprites already playing pick up the new version of their clip
        foreach (var anim in Anims.Values.Where(a => a.Atlas == atlas))
        {
            if (Clips.TryGetValue(anim.Clip.Id, out var c)) anim.Clip = c.clip;
            anim.Index = Math.Clamp(anim.Index, 0, Math.Max(0, anim.Clip.Frames.Count - 1));
        }
        return true;
    }

    private static string SafeRoot()
    {
        try { return GameReloader.GetRoot() ?? ""; }
        catch { return ""; }
    }

    /// <summary>
    /// A frame per picture, then a last, empty one for frames that show nothing, then (for 9-slice
    /// assets) nine more per picture: its pieces, cut where the slice lines cross it.
    /// </summary>
    private static void RegisterFrames(Atlas atlas)
    {
        TextureSystem.GetTextureIndex(atlas.IndexTexture, out _, out var tex);
        if (tex.texture == null) return; // failed to load; the engine has said why
        var cells = atlas.Manifest.Cells;
        atlas.EmptyFrame = cells.Count;
        atlas.SliceFrames.Clear();
        var pieces = new List<(int x, int y, int w, int h)>();
        foreach (var clip in atlas.Manifest.Clips)
        {
            if (!atlas.Assets.TryGetValue(clip.Asset, out var asset) || asset.Insets is not { } ins) continue;
            foreach (var f in clip.Frames)
            {
                if (f.Cell < 0 || f.Cell >= cells.Count || atlas.SliceFrames.ContainsKey((asset.Id, f.Cell, f.X, f.Y))) continue;
                atlas.SliceFrames[(asset.Id, f.Cell, f.X, f.Y)] = cells.Count + 1 + pieces.Count;
                var c = cells[f.Cell];
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++)
                    {
                        var (x0, x1) = Band(i, ins.l, asset.Width - ins.r, asset.Width, f.X, c.W);
                        var (y0, y1) = Band(j, ins.t, asset.Height - ins.b, asset.Height, f.Y, c.H);
                        pieces.Add(x1 > x0 && y1 > y0 ? (c.X + x0 - f.X, c.Y + y0 - f.Y, x1 - x0, y1 - y0) : (0, 0, 0, 0));
                    }
            }
        }
        FadeMonoGameCommands.SetTextureFrameCount(atlas.IndexTexture, cells.Count + 1 + pieces.Count);
        for (int i = 0; i < cells.Count; i++)
            FadeMonoGameCommands.SetTextureFrameRect(atlas.IndexTexture, i, cells[i].X, cells[i].Y, cells[i].W, cells[i].H);
        FadeMonoGameCommands.SetTextureFrameRect(atlas.IndexTexture, cells.Count, 0, 0, 0, 0);
        for (int k = 0; k < pieces.Count; k++)
            FadeMonoGameCommands.SetTextureFrameRect(atlas.IndexTexture, cells.Count + 1 + k, pieces[k].x, pieces[k].y, pieces[k].w, pieces[k].h);
    }

    /// <summary>
    /// Band <paramref name="i"/> of a 9-slice (0 before the first line, 1 between, 2 after), in the
    /// asset's canvas, cut to a picture placed at <paramref name="at"/> with <paramref name="size"/>.
    /// </summary>
    internal static (int from, int to) Band(int i, int line0, int line1, int full, int at, int size)
    {
        int a = i == 0 ? 0 : i == 1 ? line0 : line1, b = i == 0 ? line0 : i == 1 ? line1 : full;
        return (Math.Max(a, at), Math.Min(b, at + size));
    }

    /// <summary>In a Debug build, re-read the manifest when Fink exports again (the textures reload by themselves).</summary>
    private static void Watch(Atlas atlas)
    {
        atlas.Watcher?.Dispose();
        atlas.Watcher = null;
        if (atlas.SourceJson == null) return;
        try
        {
            var w = new FileSystemWatcher(Path.GetDirectoryName(atlas.SourceJson)!, Path.GetFileName(atlas.SourceJson))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };
            w.Changed += (_, _) => atlas.Changed = true;
            w.Created += (_, _) => atlas.Changed = true;
            w.Renamed += (_, _) => atlas.Changed = true;
            w.EnableRaisingEvents = true;
            atlas.Watcher = w;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[fink] can't watch {atlas.SourceJson}: {e.Message}");
        }
    }

    // ---------------------------------------------------------------- playing

    internal static bool Play(int sprite, int clipId)
    {
        if (!Clips.TryGetValue(clipId, out var found))
        {
            Console.Error.WriteLine($"[fink] no clip {clipId} in any loaded atlas (is the atlas loaded, and its fink_*.fbasic up to date?)");
            return false;
        }
        var anim = Anims.TryGetValue(sprite, out var a) ? a : Pending.Remove(sprite, out var p) ? p : new Anim { Sprite = sprite };
        anim.Atlas = found.atlas;
        anim.Clip = found.clip;
        anim.Index = 0;
        anim.Ms = 0;
        anim.Direction = 1;
        anim.Playing = true;
        anim.Done = false;
        Anims[sprite] = anim;
        Apply(anim);
        return true;
    }

    /// <summary>The sprite's animation state, made if needed (settings before the first play are kept).</summary>
    internal static Anim StateOf(int sprite) =>
        Anims.TryGetValue(sprite, out var a) ? a : Pending.TryGetValue(sprite, out var p) ? p : Pending[sprite] = new Anim { Sprite = sprite };

    public override void BeforeVmTick(double elapsedSeconds, double totalSeconds)
    {
        foreach (var atlas in Atlases)
        {
            if (atlas.Changed)
            {
                atlas.Changed = false;
                Thread.Sleep(30); // let Fink finish writing
                if (ReadManifest(atlas)) RegisterFrames(atlas);
            }
            SetEffectParameters(atlas);
        }

        foreach (var anim in Anims.Values)
        {
            if (anim.Playing && !anim.Done) Advance(anim, elapsedSeconds * 1000 * anim.Speed);
            Apply(anim);
        }
    }

    /// <summary>
    /// After the program's frame: stretched 9-slices again, so their helper sprites follow a
    /// sprite the program just moved. (The engine doesn't call this yet; until it does, the
    /// helpers of a moving panel trail by a frame.)
    /// </summary>
    public override void AfterVmTick(double elapsedSeconds, double totalSeconds)
    {
        foreach (var anim in Anims.Values)
            if (anim.Helpers != null && anim.SizeW > 0) Apply(anim);
    }

    /// <summary>Effects lose their parameters when they hot-reload, so these are set every frame.</summary>
    private static void SetEffectParameters(Atlas atlas)
    {
        RenderSystem.GetEffectIndex(atlas.Effect, out _, out var fx);
        var effect = fx.effect;
        if (effect == null) return;
        TextureSystem.GetTextureIndex(atlas.PaletteTexture, out _, out var pal);
        if (pal.texture != null) effect.Parameters["Palette"]?.SetValue(pal.texture);
        effect.Parameters["PaletteRows"]?.SetValue((float)Math.Max(1, atlas.Manifest.PaletteRows.Count));
    }

    private static void Advance(Anim anim, double ms)
    {
        var frames = anim.Clip.Frames;
        if (frames.Count == 0) return;
        anim.Ms += ms;
        int guard = 0;
        while (anim.Ms >= Math.Max(1, frames[anim.Index].Ms) && guard++ < 1000)
        {
            anim.Ms -= Math.Max(1, frames[anim.Index].Ms);
            int next = anim.Index + anim.Direction;
            switch (anim.Clip.Loop)
            {
                case "once":
                    if (next >= frames.Count) { anim.Done = true; anim.Ms = 0; return; }
                    break;
                case "pingpong":
                    if (frames.Count == 1) next = 0;
                    else if (next >= frames.Count || next < 0)
                    {
                        anim.Direction = -anim.Direction;
                        next = anim.Index + anim.Direction;
                    }
                    break;
                default:
                    if (next >= frames.Count) next = 0;
                    break;
            }
            anim.Index = next;
        }
    }

    /// <summary>Puts the current frame on the sprite: texture, effect, frame, origin, palette row.</summary>
    private static void Apply(Anim anim)
    {
        var frames = anim.Clip.Frames;
        if (frames.Count == 0) return;
        if (anim.SizeW > 0 && anim.SizeH > 0 && anim.Atlas.Assets.TryGetValue(anim.Clip.Asset, out var sliced) && sliced.Insets != null)
        {
            ApplySliced(anim, sliced);
            return;
        }
        HideHelpers(anim);
        var f = frames[Math.Clamp(anim.Index, 0, frames.Count - 1)];
        var cells = anim.Atlas.Manifest.Cells;
        SpriteSystem.GetSpriteIndex(anim.Sprite, out var index, out var sprite);
        sprite.imageId = anim.Atlas.IndexTexture;
        sprite.effectId = anim.Atlas.Effect;
        if (f.Cell >= 0 && f.Cell < cells.Count)
        {
            var cell = cells[f.Cell];
            sprite.currentFrame = f.Cell;
            // the pivot (or the anchor) stays put while trimmed pictures change size and place
            var (ax, ay) = anim.Pivot();
            sprite.origin = new Vector2((ax - f.X) / Math.Max(1, cell.W), (ay - f.Y) / Math.Max(1, cell.H));
        }
        else
        {
            sprite.currentFrame = cells.Count; // the empty frame
        }
        var t = sprite.texCoord1.tl;
        sprite.texCoord1 = new SpriteTexCoord1(new Vector4(anim.PaletteRow, t.Y, t.Z, t.W));
        SpriteSystem.sprites[index] = sprite;
    }

    /// <summary>
    /// A 9-slice asset at another size: nine pieces, the corners as drawn, the edges stretched one
    /// way, the middle both. The program's sprite is the top-left corner (never stretched, so its
    /// scale stays the program's); eight helper sprites copy everything else from it each frame.
    /// All nine turn about the same point, the pivot (or the anchor), so rotation works.
    /// Flipping isn't supported: each piece would flip on its own.
    /// </summary>
    private static void ApplySliced(Anim anim, FinkAsset asset)
    {
        var f = anim.Frame!;
        var (l, t, r, b) = asset.Insets!.Value;
        float W = asset.Width, H = asset.Height;
        float w = Math.Max(anim.SizeW, l + r), h = Math.Max(anim.SizeH, t + b);
        float kx = W - l - r > 0 ? (w - l - r) / (W - l - r) : 1, ky = H - t - b > 0 ? (h - t - b) / (H - t - b) : 1;
        float MapX(float x) => x <= l ? x : x >= W - r ? x - W + w : l + (x - l) * kx;
        float MapY(float y) => y <= t ? y : y >= H - b ? y - H + h : t + (y - t) * ky;
        var (px, py) = anim.AnchorSet ? (anim.AnchorX * w, anim.AnchorY * h) : (MapX(f.Px), MapY(f.Py));

        anim.Atlas.SliceFrames.TryGetValue((asset.Id, f.Cell, f.X, f.Y), out int first);
        bool has = f.Cell >= 0 && first > 0;
        var cell = has ? anim.Atlas.Manifest.Cells[f.Cell] : null;

        SpriteSystem.GetSpriteIndex(anim.Sprite, out var mainIndex, out var main);
        main.imageId = anim.Atlas.IndexTexture;
        main.effectId = anim.Atlas.Effect;
        var t1 = main.texCoord1.tl;
        main.texCoord1 = new SpriteTexCoord1(new Vector4(anim.PaletteRow, t1.Y, t1.Z, t1.W));
        anim.Helpers ??= NewHelpers();

        for (int k = 0; k < 9; k++)
        {
            int i = k % 3, j = k / 3;
            var (x0, x1) = has ? Band(i, l, asset.Width - r, asset.Width, f.X, cell!.W) : (0, 0);
            var (y0, y1) = has ? Band(j, t, asset.Height - b, asset.Height, f.Y, cell!.H) : (0, 0);
            float sx = i == 1 ? kx : 1, sy = j == 1 ? ky : 1;
            bool empty = x1 <= x0 || y1 <= y0 || sx <= 0 || sy <= 0;
            // where the piece's top-left lands, from the pivot, in the asset's pixels
            float dx = MapX(x0) - px, dy = MapY(y0) - py;
            int frame = empty ? anim.Atlas.EmptyFrame : first + k;
            var origin = empty ? Vector2.Zero : new Vector2(-dx / sx / (x1 - x0), -dy / sy / (y1 - y0));
            if (k == 0)
            {
                main.currentFrame = frame;
                main.origin = origin;
                continue;
            }
            int id = anim.Helpers[k - 1];
            SpriteSystem.GetSpriteIndex(id, out var hi, out var hs);
            CopyLook(main, ref hs, hi);
            hs.currentFrame = frame;
            hs.origin = origin;
            hs.scale = main.scale * new Vector2(sx, sy);
            SpriteSystem.sprites[hi] = hs;
        }
        SpriteSystem.sprites[mainIndex] = main;
    }

    /// <summary>
    /// Helper sprite ids: negative, counting down from -1, so they never meet a program's own, and
    /// small, because the engine sorts sprites by an id-based depth (0.001 * id / 500 on top of
    /// zOrder / 500): a large id would push them out of the depth range, where they aren't drawn.
    /// A thousand helpers shift depth by less than one zOrder step; same-order sprites can still
    /// cover a stretched panel's edges, so give a panel an order of its own.
    /// </summary>
    private static int _nextHelper = -1;

    private static int[] NewHelpers()
    {
        var ids = new int[8];
        for (int i = 0; i < 8; i++) ids[i] = _nextHelper--;
        return ids;
    }

    /// <summary>A helper looks like the sprite it helps: place, turn, color, order, outputs, shader, palette row.</summary>
    private static void CopyLook(Sprite main, ref Sprite h, int helperIndex)
    {
        h.hidden = main.hidden;
        h.imageId = main.imageId;
        h.effectId = main.effectId;
        h.position = main.position;
        h.rotation = main.rotation;
        h.color = main.color;
        h.texCoord1 = main.texCoord1;
        h.texCoord2 = main.texCoord2;
        h.stageIdFlags = main.stageIdFlags;
        h.anchorTransformId = main.anchorTransformId;
        h.effects = SpriteEffects.None;
        if (h.outputIdFlags != main.outputIdFlags)
        {
            bool first = true;
            int flags = 0;
            foreach (var o in RenderSystem.outputs)
            {
                if (!SpriteSystem.DoesFlagContainId(o.id, main.outputIdFlags)) continue;
                if (first) RenderSystem.SetSpriteToOutput(helperIndex, o.id, h.outputIdFlags);
                else RenderSystem.AddSpriteToOutput(helperIndex, o.id, flags);
                flags = SpriteSystem.AddIdToFlags(o.id, flags);
                first = false;
            }
            h.outputIdFlags = main.outputIdFlags;
        }
        if (h.zOrder != main.zOrder)
        {
            h.zOrder = main.zOrder;
            RenderSystem.MarkOutputsDirtyForFlags(h.outputIdFlags);
        }
    }

    /// <summary>Back to one sprite: the helpers hide (they're reused if it's stretched again).</summary>
    internal static void HideHelpers(Anim anim)
    {
        if (anim.Helpers == null) return;
        foreach (var id in anim.Helpers)
        {
            SpriteSystem.GetSpriteIndex(id, out var hi, out var hs);
            if (hs.hidden) continue;
            hs.hidden = true;
            SpriteSystem.sprites[hi] = hs;
        }
    }

    /// <summary>
    /// Where a named point of the sprite's asset is right now, in the same coordinates as the
    /// sprite's position: the frame's point relative to its pivot, through the sprite's scale,
    /// flip and rotation. Null when the sprite isn't playing a clip or the asset has no such point.
    /// </summary>
    internal static Vector2? PointOf(int spriteId, int pointId)
    {
        if (!Anims.TryGetValue(spriteId, out var anim) || anim.Frame is not { } f) return null;
        if (!anim.Atlas.Assets.TryGetValue(anim.Clip.Asset, out var asset) || asset.Points == null) return null;
        int k = asset.Points.FindIndex(p => p.Id == pointId);
        if (k < 0 || f.Pts == null || f.Pts.Length < k * 2 + 2) return null;
        var (px, py) = anim.Pivot();
        SpriteSystem.GetSpriteIndex(spriteId, out _, out var sprite);
        var local = new Vector2(f.Pts[k * 2] + 0.5f - px, f.Pts[k * 2 + 1] + 0.5f - py) * sprite.scale;
        if ((sprite.effects & SpriteEffects.FlipHorizontally) != 0) local.X = -local.X;
        if ((sprite.effects & SpriteEffects.FlipVertically) != 0) local.Y = -local.Y;
        if (sprite.rotation != 0)
        {
            float c = MathF.Cos(sprite.rotation), s = MathF.Sin(sprite.rotation);
            local = new Vector2(local.X * c - local.Y * s, local.X * s + local.Y * c);
        }
        return sprite.position + local;
    }

    // ---------------------------------------------------------------- engine hooks

    public override bool CanReload(ReloadKind kind, out string reason)
    {
        reason = null!;
        if (kind == ReloadKind.FullRestart)
        {
            // everything the engine held is gone; the program loads its atlases again
            Anims.Clear();
            Pending.Clear();
            _nextHelper = -1;
        }
        return true;
    }

    public override void Shutdown()
    {
        foreach (var a in Atlases) a.Watcher?.Dispose();
    }
}
