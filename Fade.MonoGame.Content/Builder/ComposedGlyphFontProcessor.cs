using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Fade.MonoGame.Content;

/// <summary>
/// A font processor that draws the accented letters a font is missing, out of the font's own pieces.
///
/// A display font often has the accented letters of western Europe and nothing more: SuperCorn, the
/// fish game's font, has é and Ł but not Polish ą ć ę ń ś ź ż. A letter like that is a base letter and
/// a mark (Unicode says so: ć is c + U+0301). The marks are taken from letters the font does have, so
/// they are drawn the font's way and in its place:
///
///   acute (ć ń ś ź)      é minus e, for small letters; É minus E for capitals (higher up)
///   dot above (ż Ż)      i minus ı, moved up for capitals the same way the acute is
///   ogonek (ą ę Ą Ę)     the font's spacing ogonek ˛, hung off the bottom right of the letter
///
/// Every other character is built exactly as MonoGame's FontDescriptionProcessor builds it. A letter
/// the font has is never redrawn. A letter that needs a piece the font lacks is left out, as before,
/// so the build check (FLOC013) still says so.
/// </summary>
[ContentProcessor(DisplayName = "Sprite Font Description - Fade (composed glyphs)")]
public class ComposedGlyphFontProcessor : FontDescriptionProcessor
{
    private const char Acute = '́';
    private const char DotAbove = '̇';
    private const char Ogonek = '̨';

    // One alpha bitmap of a glyph, placed in line coordinates: x from the pen position, y from the top
    // of the line.
    private sealed class Ink
    {
        public int x, y, w, h;
        public byte[] a = Array.Empty<byte>();
        public byte At(int px, int py) => px < x || py < y || px >= x + w || py >= y + h ? (byte)0 : a[(py - y) * w + (px - x)];
    }

    public override SpriteFontContent Process(FontDescription input, ContentProcessorContext context)
    {
        // The characters to draw: wanted, missing from the font, and made of a base the font has plus
        // a mark we know how to place.
        var fontFile = FindFontFile(input);
        var have = fontFile == null ? null : TrueTypeCmap.Read(fontFile);
        var composed = new List<(char c, char baseChar, char mark)>();
        if (have != null)
        {
            foreach (var c in input.Characters.ToList())
            {
                if (have.Contains(c)) continue;
                var d = c.ToString().Normalize(NormalizationForm.FormD);
                if (d.Length != 2 || !have.Contains(d[0]) || (d[1] != Acute && d[1] != DotAbove && d[1] != Ogonek)) continue;
                composed.Add((c, d[0], d[1]));
                input.Characters.Remove(c); // a missing glyph would otherwise come out blank
            }
        }

        // Work on plain colors, and convert to the format that was asked for at the end.
        var wantedFormat = TextureFormat;
        TextureFormat = TextureProcessorOutputFormat.Color;
        var output = base.Process(input, context);
        TextureFormat = wantedFormat;

        if (composed.Count > 0)
            Compose(output, composed, have!, context);

        TextureProfile.ForPlatform(context.TargetPlatform).ConvertTexture(context, output.Texture, wantedFormat, true);
        return output;
    }

    private void Compose(SpriteFontContent font, List<(char c, char baseChar, char mark)> composed, HashSet<char> have, ContentProcessorContext context)
    {
        var bmp = (PixelBitmapContent<Color>)font.Texture.Faces[0][0];
        var map = font.CharacterMap;

        Ink? Glyph(char c)
        {
            var i = map.IndexOf(c);
            if (i < 0) return null;
            var r = font.Glyphs[i];
            var ink = new Ink { x = (int)Math.Round(font.Kerning[i].X), y = font.Cropping[i].Y, w = r.Width, h = r.Height, a = new byte[r.Width * r.Height] };
            for (var py = 0; py < r.Height; py++)
                for (var px = 0; px < r.Width; px++)
                    ink.a[py * r.Width + px] = bmp.GetPixel(r.X + px, r.Y + py).A;
            return Trim(ink);
        }

        // The rows of `accented` above the top of `plain`: the mark, where the font puts it.
        Ink? MarkAbove(char accented, char plain)
        {
            var a = Glyph(accented);
            var p = Glyph(plain);
            if (a == null || p == null) return null;
            var mark = new Ink { x = a.x, y = a.y, w = a.w, h = Math.Max(0, p.y - a.y - 0), a = Array.Empty<byte>() };
            if (mark.h <= 0) return null;
            mark.a = new byte[mark.w * mark.h];
            for (var py = 0; py < mark.h; py++)
                for (var px = 0; px < mark.w; px++)
                    mark.a[py * mark.w + px] = a.At(a.x + px, a.y + py);
            return Trim(mark);
        }

        var acuteSmall = MarkAbove('é', 'e');
        var acuteCap = MarkAbove('É', 'E');
        // An all-caps font's i has no dot, so the spacing dot ˙ stands in for one.
        var dotSmall = MarkAbove('i', 'ı') ?? (have.Contains('˙') ? Glyph('˙') : null);
        var ogonek = have.Contains('˛') ? Glyph('˛') : null;

        var made = new List<(char c, Ink ink, int baseIndex)>();
        foreach (var (c, baseChar, markChar) in composed)
        {
            var baseInk = Glyph(baseChar);
            var baseIndex = map.IndexOf(baseChar);
            if (baseInk == null) continue;
            var capital = char.IsUpper(baseChar);

            Ink? mark = markChar switch
            {
                Acute => capital ? acuteCap : acuteSmall,
                DotAbove => dotSmall,
                Ogonek => ogonek,
                _ => null,
            };
            if (mark == null)
            {
                context.Logger.LogWarning(null, null, "no way to draw '{0}': the font has no {1} to borrow", c, markChar == Ogonek ? "ogonek (˛)" : markChar == DotAbove ? "dot (i and ı, or ˙)" : "acute (é and É)");
                continue;
            }

            Ink placed;
            if (markChar == Ogonek)
            {
                // Hung off the bottom of the letter, flush with its right side, overlapping by a pixel.
                placed = Shift(mark, baseInk.x + baseInk.w - mark.w - mark.x, baseInk.y + baseInk.h - 1 - mark.y);
            }
            else
            {
                // Centered over the letter, the same gap above it as the font's own acute leaves over
                // e and E, so every mark above sits where the font's accents do.
                var gap = 1;
                if (capital && acuteCap != null) gap = Glyph('E')!.y - (acuteCap.y + acuteCap.h);
                else if (!capital && acuteSmall != null) gap = Glyph('e')!.y - (acuteSmall.y + acuteSmall.h);
                var dx = baseInk.x + (baseInk.w - mark.w) / 2 - mark.x;
                var dy = baseInk.y - gap - mark.h - mark.y;
                placed = Shift(mark, dx, dy);
            }

            made.Add((c, Union(baseInk, placed), baseIndex));
        }
        if (made.Count == 0) return;

        // A strip under the existing atlas for the new glyphs, left to right, a pixel apart.
        const int pad = 1;
        var x = pad;
        var y = bmp.Height + pad;
        var rowH = 0;
        var places = new List<Rectangle>();
        foreach (var (_, ink, _) in made)
        {
            if (x + ink.w + pad > bmp.Width) { x = pad; y += rowH + pad; rowH = 0; }
            places.Add(new Rectangle(x, y, ink.w, ink.h));
            x += ink.w + pad;
            rowH = Math.Max(rowH, ink.h);
        }
        var bigger = new PixelBitmapContent<Color>(bmp.Width, y + rowH + pad);
        for (var py = 0; py < bmp.Height; py++)
            for (var px = 0; px < bmp.Width; px++)
                bigger.SetPixel(px, py, bmp.GetPixel(px, py));

        // The atlas already holds premultiplied white (or white with alpha); write the new ink the same way.
        var premultiplied = PremultiplyAlpha;
        for (var i = 0; i < made.Count; i++)
        {
            var (c, ink, baseIndex) = made[i];
            var r = places[i];
            for (var py = 0; py < ink.h; py++)
                for (var px = 0; px < ink.w; px++)
                {
                    var alpha = ink.a[py * ink.w + px];
                    bigger.SetPixel(r.X + px, r.Y + py, premultiplied ? new Color(alpha, alpha, alpha, alpha) : new Color((byte)255, (byte)255, (byte)255, alpha));
                }

            // The same advance as the base letter: the mark never pushes the letters around it.
            var baseKern = font.Kerning[baseIndex];
            var total = baseKern.X + baseKern.Y + baseKern.Z;
            var kern = new Vector3(ink.x, ink.w, total - ink.x - ink.w);
            var crop = new Rectangle(0, ink.y, font.Cropping[baseIndex].Width, font.VerticalLineSpacing);

            // The character map must stay in order.
            var at = 0;
            while (at < map.Count && map[at] < c) at++;
            map.Insert(at, c);
            font.Glyphs.Insert(at, r);
            font.Cropping.Insert(at, crop);
            font.Kerning.Insert(at, kern);
        }

        font.Texture.Faces[0].Clear();
        font.Texture.Faces[0].Add(bigger);
        context.Logger.LogMessage("Drew {0} letter(s) the font does not have: {1}", made.Count, new string(made.Select(m => m.c).ToArray()));
    }

    private static Ink Shift(Ink ink, int dx, int dy) => new() { x = ink.x + dx, y = ink.y + dy, w = ink.w, h = ink.h, a = ink.a };

    private static Ink Union(Ink a, Ink b)
    {
        var x0 = Math.Min(a.x, b.x);
        var y0 = Math.Min(a.y, b.y);
        var x1 = Math.Max(a.x + a.w, b.x + b.w);
        var y1 = Math.Max(a.y + a.h, b.y + b.h);
        var u = new Ink { x = x0, y = y0, w = x1 - x0, h = y1 - y0 };
        u.a = new byte[u.w * u.h];
        for (var py = 0; py < u.h; py++)
            for (var px = 0; px < u.w; px++)
                u.a[py * u.w + px] = Math.Max(a.At(x0 + px, y0 + py), b.At(x0 + px, y0 + py));
        return u;
    }

    // Shrinks a bitmap to the pixels that have ink.
    private static Ink Trim(Ink ink)
    {
        int minX = ink.w, minY = ink.h, maxX = -1, maxY = -1;
        for (var py = 0; py < ink.h; py++)
            for (var px = 0; px < ink.w; px++)
                if (ink.a[py * ink.w + px] > 0)
                {
                    minX = Math.Min(minX, px); maxX = Math.Max(maxX, px);
                    minY = Math.Min(minY, py); maxY = Math.Max(maxY, py);
                }
        if (maxX < 0) return new Ink { x = ink.x, y = ink.y, w = 0, h = 0 };
        var t = new Ink { x = ink.x + minX, y = ink.y + minY, w = maxX - minX + 1, h = maxY - minY + 1 };
        t.a = new byte[t.w * t.h];
        for (var py = 0; py < t.h; py++)
            for (var px = 0; px < t.w; px++)
                t.a[py * t.w + px] = ink.a[(py + minY) * ink.w + px + minX];
        return t;
    }

    // The .spritefont names its font file without the extension, next to it; that is how the fish
    // game's fonts are found. A system font is not looked for: there would be nothing to compose with.
    private static string? FindFontFile(FontDescription input)
    {
        var dir = Path.GetDirectoryName(input.Identity?.SourceFilename ?? "") ?? "";
        foreach (var ext in new[] { "", ".ttf", ".otf" })
        {
            var path = Path.Combine(dir, input.FontName + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }
}

/// <summary>Reads which characters a TrueType font maps (its cmap table: format 4 or 12).</summary>
public static class TrueTypeCmap
{
    public static HashSet<char> Read(string path)
    {
        var b = File.ReadAllBytes(path);
        int U16(int o) => b[o] << 8 | b[o + 1];
        long U32(int o) => (long)b[o] << 24 | (long)b[o + 1] << 16 | (long)b[o + 2] << 8 | b[o + 3];
        short S16(int o) => (short)U16(o);

        var chars = new HashSet<char>();
        int cmap = -1;
        for (var i = 0; i < U16(4); i++)
        {
            var r = 12 + i * 16;
            if (Encoding.ASCII.GetString(b, r, 4) == "cmap") cmap = (int)U32(r + 8);
        }
        if (cmap < 0) return chars;

        int best = -1, bestScore = -1;
        for (var i = 0; i < U16(cmap + 2); i++)
        {
            var r = cmap + 4 + i * 8;
            int pid = U16(r), eid = U16(r + 2);
            var off = cmap + (int)U32(r + 4);
            var fmt = U16(off);
            var score = pid == 3 && eid == 10 && fmt == 12 ? 3 : pid == 3 && eid == 1 && fmt == 4 ? 2 : pid == 0 && (fmt == 4 || fmt == 12) ? 1 : 0;
            if (score > bestScore) { bestScore = score; best = off; }
        }
        if (best < 0) return chars;

        if (U16(best) == 4)
        {
            var segX2 = U16(best + 6);
            int ends = best + 14, starts = ends + segX2 + 2, deltas = starts + segX2, ranges = deltas + segX2;
            for (var s = 0; s < segX2 / 2; s++)
            {
                int end = U16(ends + s * 2), start = U16(starts + s * 2), ro = U16(ranges + s * 2);
                var delta = S16(deltas + s * 2);
                for (var c = start; c <= end && c != 0xFFFF; c++)
                {
                    int g = ro == 0 ? (c + delta) & 0xFFFF : U16(ranges + s * 2 + ro + (c - start) * 2);
                    if (ro != 0 && g != 0) g = (g + delta) & 0xFFFF;
                    if (g != 0) chars.Add((char)c);
                }
            }
        }
        else if (U16(best) == 12)
        {
            for (var i = 0; i < U32(best + 12); i++)
            {
                var r = best + 16 + i * 12;
                for (var c = U32(r); c <= U32(r + 4) && c <= 0xFFFF; c++) chars.Add((char)c);
            }
        }
        return chars;
    }
}
