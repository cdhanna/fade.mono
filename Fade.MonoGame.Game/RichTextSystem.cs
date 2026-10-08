using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using SpriteFont = Microsoft.Xna.Framework.Graphics.Fade.SpriteFont;

namespace Fade.MonoGame.Core;

/// <summary>
/// A picture that a font can draw in the middle of a line of text. A rich text sprite asks for one by
/// writing its alias, or its id, in braces.
/// </summary>
public struct FontIcon
{
    public int id;
    public string alias;
    public int textureId;
    public int frame;

    // Multipliers on the default fit. At (1, 1) the icon is as tall as the font's line, and as
    // wide as its frame's aspect ratio makes it.
    public Vector2 scale;

    // A nudge, as a ratio of the font's line height, so that it follows the text when the text is
    // resized.
    public Vector2 offset;
}

/// <summary>
/// The icons registered to one font. This is a class so that the copies of <see cref="RuntimeFont"/>
/// that get handed around all see the same icons.
/// </summary>
public class FontIconSet
{
    public List<FontIcon> icons = new List<FontIcon>();
    private Dictionary<int, int> _idToIndex = new Dictionary<int, int>();
    private Dictionary<string, int> _aliasToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Bumped on every change, so that cached layouts know to rebuild.
    public int version;
    public int highestIconId;

    public void GetIconIndex(int iconId, out int index, out FontIcon icon)
    {
        if (!_idToIndex.TryGetValue(iconId, out index))
        {
            highestIconId = iconId > highestIconId ? iconId : highestIconId;
            index = _idToIndex[iconId] = icons.Count;
            icon = new FontIcon
            {
                id = iconId,
                scale = Vector2.One,
            };
            icons.Add(icon);
            version++;
        }
        else
        {
            icon = icons[index];
        }
    }

    public void SetIcon(int index, FontIcon icon)
    {
        var oldAlias = icons[index].alias;
        if (!string.IsNullOrEmpty(oldAlias)
            && _aliasToIndex.TryGetValue(oldAlias, out var aliasIndex)
            && aliasIndex == index)
        {
            _aliasToIndex.Remove(oldAlias);
        }

        // The latest icon to claim an alias wins it.
        if (!string.IsNullOrEmpty(icon.alias))
        {
            _aliasToIndex[icon.alias] = index;
        }

        icons[index] = icon;
        version++;
    }

    /// <summary>
    /// Find an icon from the name written between braces. An alias is tried first, and then the name
    /// is tried as an icon id.
    /// </summary>
    public bool TryResolve(string name, out FontIcon icon)
    {
        if (_aliasToIndex.TryGetValue(name, out var index)
            || (int.TryParse(name, out var iconId) && _idToIndex.TryGetValue(iconId, out index)))
        {
            icon = icons[index];
            return true;
        }

        icon = default;
        return false;
    }
}

public struct RichTextRun
{
    public bool isIcon;

    // Where the top left of the run sits, relative to the top left of the whole text, before the
    // text's own scale is applied.
    public Vector2 offset;

    public string text;

    public int textureId;
    public int frame;
    public Vector2 iconSize;
}

/// <summary>
/// A string split into the pieces that get drawn: stretches of plain text, and icons. It is cached
/// on the text sprite, and only rebuilt when something it was built from changes.
/// </summary>
public class RichTextLayout
{
    public List<RichTextRun> runs = new List<RichTextRun>();
    public Vector2 size;

    // What the layout was built from.
    public string sourceText;
    public SpriteFont sourceFont;
    public FontIconSet sourceIcons;
    public int sourceIconVersion;
    public int sourceTextureVersion;
    public float sourceWrapWidth;
    public float sourceAlign;
}

/// <summary>
/// Lays out the text sprites that need more than one call to DrawString: the ones with icons in
/// them, and the ones that wrap onto several lines.
/// </summary>
public static class RichTextSystem
{
    // Icons are sized from their texture frames, so layouts need to rebuild when a frame table
    // changes. Texture changes are rare, so one counter for all of them is plenty.
    public static int textureVersion;

    /// <summary>
    /// Plain text on one line is drawn and measured by the font. Everything else goes through a
    /// layout.
    /// </summary>
    public static bool UsesLayout(ref TextSprite text)
    {
        return text.richText || text.wrapWidth > 0;
    }

    /// <summary>
    /// The size of a text sprite before its scale is applied. Use this instead of asking the font to
    /// measure the string, because the font does not know about icons or wrapping.
    /// </summary>
    public static Vector2 Measure(ref TextSprite text, ref RuntimeFont runtimeFont)
    {
        if (runtimeFont.font == null || string.IsNullOrEmpty(text.text)) return Vector2.Zero;
        if (!UsesLayout(ref text)) return runtimeFont.font.MeasureString(text.text);

        return GetLayout(ref text, ref runtimeFont).size;
    }

    /// <summary>
    /// Measure a text sprite by its index, and keep the layout that was built to do it.
    /// </summary>
    public static Vector2 Measure(int textIndex, ref RuntimeFont runtimeFont)
    {
        return Measure(ref TextSystem.textSprites[textIndex], ref runtimeFont);
    }

    public static RichTextLayout GetLayout(ref TextSprite text, ref RuntimeFont runtimeFont)
    {
        var layout = text.layout;

        // Text that is not rich draws its braces, so it gets laid out as if the font had no icons.
        var icons = text.richText ? runtimeFont.icons : null;
        var iconVersion = icons?.version ?? 0;

        // The wrap width is given in pixels on screen, but the layout works in the font's own
        // units, from before the text's scale is applied.
        var wrapWidth = 0f;
        if (text.wrapWidth > 0)
        {
            var scale = Math.Abs(text.sprite.scale.X);
            wrapWidth = text.wrapWidth / (scale > .0001f ? scale : 1);
        }

        if (layout != null
            && ReferenceEquals(layout.sourceFont, runtimeFont.font)
            && ReferenceEquals(layout.sourceIcons, icons)
            && layout.sourceIconVersion == iconVersion
            && layout.sourceTextureVersion == textureVersion
            && layout.sourceWrapWidth == wrapWidth
            && layout.sourceAlign == text.align
            && string.Equals(layout.sourceText, text.text, StringComparison.Ordinal))
        {
            return layout;
        }

        layout ??= new RichTextLayout();
        layout.sourceText = text.text;
        layout.sourceFont = runtimeFont.font;
        layout.sourceIcons = icons;
        layout.sourceIconVersion = iconVersion;
        layout.sourceTextureVersion = textureVersion;
        layout.sourceWrapWidth = wrapWidth;
        layout.sourceAlign = text.align;

        _builder.Build(layout, text.text ?? string.Empty, runtimeFont.font, icons, wrapWidth, text.align);

        text.layout = layout;
        return layout;
    }

    private static readonly Builder _builder = new Builder();

    /// <summary>
    /// The state of one pass over a string. The game runs on one thread, so a single builder is
    /// reused for every layout.
    /// </summary>
    private class Builder
    {
        private struct Line
        {
            public int firstRun, endRun;
            public float width;
        }

        private RichTextLayout _layout;
        private SpriteFont _font;
        private float _wrapWidth;
        private float _lineHeight;

        private readonly List<Line> _lines = new List<Line>();
        private int _lineFirstRun;
        private bool _lineHasContent;
        private float _penX, _penY;

        // Text that will become one run. It is held back so that words and the spaces between
        // them are drawn with a single call, exactly as the font would space them.
        private readonly StringBuilder _pending = new StringBuilder();
        private float _pendingX;

        // The word being read, and the spaces in front of it. The spaces only get drawn if the
        // word fits on the line. If the word wraps, they are dropped, so that a wrapped line does
        // not start or end with a gap.
        private readonly StringBuilder _word = new StringBuilder();
        private int _spaces;

        private readonly StringBuilder _scratch = new StringBuilder();

        public void Build(RichTextLayout layout, string source, SpriteFont font, FontIconSet icons,
            float wrapWidth, float align)
        {
            _layout = layout;
            _font = font;
            _wrapWidth = wrapWidth;
            _lineHeight = font.LineSpacing;

            layout.runs.Clear();
            _lines.Clear();
            _pending.Clear();
            _word.Clear();
            _spaces = 0;
            _lineFirstRun = 0;
            _lineHasContent = false;
            _penX = _penY = _pendingX = 0;

            var iconCenter = GetIconCenter(font);

            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];

                if (c == '\r') continue;

                if (c == '\n')
                {
                    PlaceWord();
                    EndLine();
                    continue;
                }

                if (c == ' ')
                {
                    PlaceWord();
                    _spaces++;
                    continue;
                }

                if (c == '{' && icons != null)
                {
                    // Two in a row is how to write one that really is a brace.
                    if (i + 1 < source.Length && source[i + 1] == '{')
                    {
                        _word.Append('{');
                        i++;
                        continue;
                    }

                    var close = source.IndexOf('}', i + 1);
                    if (close > i + 1
                        && icons.TryResolve(source.Substring(i + 1, close - i - 1).Trim(), out var icon)
                        && TryGetIconSize(ref icon, _lineHeight, out var iconSize))
                    {
                        PlaceWord();
                        PlaceIcon(ref icon, iconSize, iconCenter);
                        i = close;
                        continue;
                    }

                    // Anything that is not an icon is left exactly as it was written, so that a
                    // typo shows up on screen instead of vanishing.
                }

                _word.Append(c);
            }

            PlaceWord();
            EndLine();

            var width = 0f;
            for (var i = 0; i < _lines.Count; i++)
            {
                width = Math.Max(width, _lines[i].width);
            }

            // Slide each line over inside the block. 0 keeps them on the left, 1 puts them on the
            // right, and .5 centers them.
            if (align != 0)
            {
                for (var i = 0; i < _lines.Count; i++)
                {
                    var shift = (width - _lines[i].width) * align;
                    for (var r = _lines[i].firstRun; r < _lines[i].endRun; r++)
                    {
                        var run = layout.runs[r];
                        run.offset.X += shift;
                        layout.runs[r] = run;
                    }
                }
            }

            layout.size = new Vector2(width, _lines.Count * _lineHeight);
            _layout = null;
            _font = null;
        }

        private float MeasureWidth(StringBuilder text)
        {
            return text.Length == 0 ? 0 : _font.MeasureString(text).X;
        }

        /// <summary>
        /// How wide the pending text would be with the waiting spaces, and optionally the waiting
        /// word, added to the end of it.
        /// </summary>
        private float MeasurePendingWith(bool includeWord)
        {
            _scratch.Clear();
            _scratch.Append(_pending);
            _scratch.Append(' ', _spaces);
            if (includeWord) _scratch.Append(_word);
            return MeasureWidth(_scratch);
        }

        private bool ShouldWrap(float right)
        {
            // A line always takes at least one thing, even if that thing is too wide for it.
            // Otherwise a long word would wrap forever.
            return _wrapWidth > 0 && _lineHasContent && right > _wrapWidth;
        }

        private void PlaceWord()
        {
            if (_word.Length == 0) return;

            if (ShouldWrap(_pendingX + MeasurePendingWith(includeWord: true)))
            {
                _spaces = 0;
                EndLine();
            }

            _pending.Append(' ', _spaces);
            _pending.Append(_word);
            _penX = _pendingX + MeasureWidth(_pending);

            _word.Clear();
            _spaces = 0;
            _lineHasContent = true;
        }

        private void PlaceIcon(ref FontIcon icon, Vector2 iconSize, float iconCenter)
        {
            if (ShouldWrap(_pendingX + MeasurePendingWith(includeWord: false) + iconSize.X))
            {
                _spaces = 0;
                EndLine();
            }

            _pending.Append(' ', _spaces);
            _spaces = 0;
            FlushText();

            _layout.runs.Add(new RichTextRun
            {
                isIcon = true,
                textureId = icon.textureId,
                frame = icon.frame,
                iconSize = iconSize,

                // An icon bigger than the line hangs out of it on both sides instead of pushing
                // the lines apart, so that adding an icon never moves the text.
                offset = new Vector2(
                    _penX + icon.offset.X * _lineHeight,
                    _penY + iconCenter - iconSize.Y * .5f + icon.offset.Y * _lineHeight),
            });

            _penX += iconSize.X;
            _pendingX = _penX;
            _lineHasContent = true;
        }

        private void FlushText()
        {
            if (_pending.Length == 0) return;

            _layout.runs.Add(new RichTextRun
            {
                text = _pending.ToString(),
                offset = new Vector2(_pendingX, _penY),
            });
            _penX = _pendingX + MeasureWidth(_pending);
            _pendingX = _penX;
            _pending.Clear();
        }

        private void EndLine()
        {
            FlushText();
            _lines.Add(new Line
            {
                firstRun = _lineFirstRun,
                endRun = _layout.runs.Count,
                width = _penX,
            });

            _lineFirstRun = _layout.runs.Count;
            _lineHasContent = false;
            _spaces = 0;
            _penX = _pendingX = 0;
            _penY += _lineHeight;
        }
    }

    /// <summary>
    /// Icons are centered on the capital letters, not on the line. A font's line usually has more
    /// room below the letters than above, so the middle of the line looks too low.
    /// </summary>
    private static float GetIconCenter(SpriteFont font)
    {
        if (font.TryGetGlyphIndex('H', out var glyphIndex))
        {
            var glyph = font.Glyphs[glyphIndex];
            if (glyph.BoundsInTexture.Height > 0)
            {
                return glyph.Cropping.Y + glyph.BoundsInTexture.Height * .5f;
            }
        }

        return font.LineSpacing * .5f;
    }

    private static bool TryGetIconSize(ref FontIcon icon, float lineHeight, out Vector2 size)
    {
        size = default;

        TextureSystem.GetTextureIndex(icon.textureId, out _, out var runtimeTex);
        if (runtimeTex.texture == null) return false;

        var src = TextureSystem.GetSourceRect(ref runtimeTex, icon.frame);
        if (src.Width <= 0 || src.Height <= 0) return false;

        size = new Vector2(
            lineHeight * ((float)src.Width / src.Height) * icon.scale.X,
            lineHeight * icon.scale.Y);
        return true;
    }
}
