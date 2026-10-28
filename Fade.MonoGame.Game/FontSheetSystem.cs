using System;
using System.Collections.Generic;
using System.IO;
using Fade.MonoGame.Core.Debug;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteFont = Microsoft.Xna.Framework.Graphics.Fade.SpriteFont;

namespace Fade.MonoGame.Core;

/// <summary>
/// Fonts that are pictures: a grid of cells, one character to a cell, drawn by hand or exported
/// from a baked font and then touched up. The characters run across each row from the top left,
/// in character-code order.
///
/// A pixel is INK when its alpha is at least <see cref="InkAlpha"/>, and anything fainter is
/// treated as empty. That leaves room for guides in the picture: the exporter tints every other
/// cell faintly so the grid can be seen in a paint program, and the loader never draws the tint.
/// </summary>
public static class FontSheetSystem
{
    public const int InkAlpha = 128;

    private const int FirstExported = 32;  // space
    private const int LastExported = 126;  // ~

    /// <summary>
    /// Build a font from the cells of a texture. Each glyph is as wide as its ink, so the letters
    /// do not have to be lined up inside their cells, and a line is as tall as a cell. A cell
    /// with no ink is a character the font does not have, except for the space.
    /// </summary>
    public static bool LoadFontFromSheet(int fontId, int textureId, int columns, int rows,
        int letterSpacing, int spaceWidth, int firstCharacter)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var runtimeTex);
        var source = runtimeTex.texture;
        if (source == null)
        {
            Console.Error.WriteLine($"[fade] font sheet {fontId}: texture {textureId} is not loaded");
            return false;
        }

        if (columns <= 0 || rows <= 0 || source.Width < columns || source.Height < rows)
        {
            Console.Error.WriteLine($"[fade] font sheet {fontId}: a {source.Width} x {source.Height} texture cannot be cut into {columns} columns and {rows} rows");
            return false;
        }

        var width = source.Width;
        var height = source.Height;
        var pixels = new Color[width * height];
        try
        {
            source.GetData(pixels);
        }
        catch (Exception ex)
        {
            // A compressed texture cannot be read back as plain colors.
            Console.Error.WriteLine($"[fade] font sheet {fontId}: could not read the pixels of texture {textureId} ({ex.Message}). The texture must not be compressed.");
            return false;
        }

        // Guides, and anything else too faint to be ink, are not part of the font.
        for (var i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].A < InkAlpha) pixels[i] = default;
        }

        var cellWidth = width / columns;
        var cellHeight = height / rows;
        if (spaceWidth <= 0) spaceWidth = Math.Max(1, cellWidth / 3);

        var bounds = new List<Rectangle>();
        var cropping = new List<Rectangle>();
        var characters = new List<char>();
        var kernings = new List<Vector3>();
        var hasQuestionMark = false;

        for (var cell = 0; cell < columns * rows; cell++)
        {
            var character = (char)(firstCharacter + cell);
            var cellX = (cell % columns) * cellWidth;
            var cellY = (cell / columns) * cellHeight;

            int minX = cellWidth, maxX = -1, minY = cellHeight, maxY = -1;
            for (var y = 0; y < cellHeight; y++)
            {
                var row = (cellY + y) * width + cellX;
                for (var x = 0; x < cellWidth; x++)
                {
                    if (pixels[row + x].A == 0) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < 0)
            {
                if (character != ' ') continue;

                // The space has no ink, only a width.
                bounds.Add(new Rectangle(cellX, cellY, 0, 0));
                cropping.Add(new Rectangle(0, 0, 0, 0));
                kernings.Add(new Vector3(0, spaceWidth, 0));
            }
            else
            {
                var glyphWidth = maxX - minX + 1;
                var glyphHeight = maxY - minY + 1;
                bounds.Add(new Rectangle(cellX + minX, cellY + minY, glyphWidth, glyphHeight));

                // Sideways the ink starts at the pen, wherever it was in the cell. Up and down it
                // stays where it was drawn, because that is what puts the letters on one baseline.
                cropping.Add(new Rectangle(0, minY, glyphWidth, glyphHeight));
                kernings.Add(new Vector3(0, glyphWidth, 0));
            }

            characters.Add(character);
            if (character == '?') hasQuestionMark = true;
        }

        if (characters.Count == 0)
        {
            Console.Error.WriteLine($"[fade] font sheet {fontId}: texture {textureId} has no ink in any cell");
            return false;
        }

        // The font keeps pixels of its own, so that it does not care what happens to the texture
        // afterwards. Run the command again to pick up a change to the picture.
        var sheet = TextureSystem.Own(new Texture2D(GameSystem.game.GraphicsDevice, width, height, false, SurfaceFormat.Color));
        sheet.SetData(pixels);

        var font = new SpriteFont(sheet, bounds, cropping, characters, cellHeight, letterSpacing, kernings,
            hasQuestionMark ? '?' : (char?)null);

        TextureSystem.GetSpriteFontIndex(fontId, out var index, out var runtimeFont);
        runtimeFont.font = font;
        TextureSystem.fonts[index] = runtimeFont;
        return true;
    }

    /// <summary>
    /// Write a loaded font out as a sheet that <see cref="LoadFontFromSheet"/> can read back: the
    /// characters from space to ~, each in a cell as tall as a line of the font. A pixel becomes
    /// solid white ink when the glyph covers at least <paramref name="cover"/> of it, and is left
    /// empty otherwise, so the smooth edges of a baked font come out hard.
    /// </summary>
    public static bool SaveFontSheet(int fontId, string filePath, int columns, float cover)
    {
        TextureSystem.GetSpriteFontIndex(fontId, out _, out var runtimeFont);
        var font = runtimeFont.font;
        if (font == null)
        {
            Console.Error.WriteLine($"[fade] save font sheet: font {fontId} is not loaded");
            return false;
        }

        if (columns <= 0) columns = 16;

        var source = font.Texture;
        byte[] sourceAlpha;
        try
        {
            sourceAlpha = ReadAlpha(source);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[fade] save font sheet: could not read the pixels of font {fontId} ({ex.Message})");
            return false;
        }

        var glyphs = font.GetGlyphs();
        var widest = 1;
        foreach (var pair in glyphs)
        {
            if (pair.Key < FirstExported || pair.Key > LastExported) continue;
            if (pair.Value.BoundsInTexture.Width > widest) widest = pair.Value.BoundsInTexture.Width;
        }

        // A pixel of room on each side, so that neighbours never touch across a cell edge.
        var cellWidth = widest + 2;
        var cellHeight = Math.Max(1, font.LineSpacing);
        var count = LastExported - FirstExported + 1;
        var rows = (count + columns - 1) / columns;
        var width = cellWidth * columns;
        var height = cellHeight * rows;
        var rgba = new byte[width * height * 4];
        var minAlpha = (int)MathF.Round(Math.Clamp(cover, 0.01f, 1f) * 255f);

        for (var cell = 0; cell < columns * rows; cell++)
        {
            var cellX = (cell % columns) * cellWidth;
            var cellY = (cell / columns) * cellHeight;

            // The guide: every other cell is tinted, too faintly to count as ink.
            if (((cell % columns) + (cell / columns)) % 2 == 0)
            {
                for (var y = 0; y < cellHeight; y++)
                {
                    for (var x = 0; x < cellWidth; x++)
                    {
                        var o = ((cellY + y) * width + cellX + x) * 4;
                        rgba[o] = 255;
                        rgba[o + 1] = 0;
                        rgba[o + 2] = 255;
                        rgba[o + 3] = 40;
                    }
                }
            }

            if (!glyphs.TryGetValue((char)(FirstExported + cell), out var glyph)) continue;

            var from = glyph.BoundsInTexture;
            for (var y = 0; y < from.Height; y++)
            {
                var toY = glyph.Cropping.Y + y;
                if (toY < 0 || toY >= cellHeight) continue;

                for (var x = 0; x < from.Width; x++)
                {
                    if (sourceAlpha[(from.Y + y) * source.Width + from.X + x] < minAlpha) continue;

                    var o = ((cellY + toY) * width + cellX + 1 + x) * 4;
                    rgba[o] = 255;
                    rgba[o + 1] = 255;
                    rgba[o + 2] = 255;
                    rgba[o + 3] = 255;
                }
            }
        }

        if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) filePath += ".png";
        File.WriteAllBytes(filePath, PngEncoder.Encode(width, height, rgba));
        Console.WriteLine($"[fade] font {fontId} saved as a sheet: {filePath}, {columns} columns, {rows} rows, cells {cellWidth} x {cellHeight}");
        return true;
    }

    /// <summary>
    /// The alpha of every pixel of a texture, a row at a time from the top. A baked font keeps its
    /// glyphs in whatever format the content pipeline chose, which is often a compressed one.
    /// </summary>
    private static byte[] ReadAlpha(Texture2D texture)
    {
        var width = texture.Width;
        var height = texture.Height;
        var alpha = new byte[width * height];

        switch (texture.Format)
        {
            case SurfaceFormat.Color:
            {
                var colors = new Color[width * height];
                texture.GetData(colors);
                for (var i = 0; i < colors.Length; i++) alpha[i] = colors[i].A;
                return alpha;
            }

            case SurfaceFormat.Alpha8:
                texture.GetData(alpha);
                return alpha;

            case SurfaceFormat.Dxt3:
            {
                // Blocks of 4 by 4 pixels, 16 bytes each. The first 8 bytes are the alpha, 4 bits
                // for a pixel, and the rest is the color, which a font has no use for.
                var blocksWide = (width + 3) / 4;
                var blocksHigh = (height + 3) / 4;
                var blocks = new byte[blocksWide * blocksHigh * 16];
                texture.GetData(blocks);
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var block = ((y / 4) * blocksWide + x / 4) * 16;
                        var pixel = (y % 4) * 4 + x % 4;
                        var nibble = (blocks[block + pixel / 2] >> ((pixel % 2) * 4)) & 0xF;
                        alpha[y * width + x] = (byte)(nibble * 17);
                    }
                }
                return alpha;
            }

            default:
                throw new NotSupportedException($"the font's texture is in the {texture.Format} format");
        }
    }
}
