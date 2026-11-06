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

    // The font's id for the icon, so that a text can scale one of its icons when it is drawn. see
    // TextSystem.iconScales.
    public int iconId;
    public int textureId;
    public int frame;
    public Vector2 iconSize;

    // What the tags of the text did to this run. see Builder.TryStyle.
    public bool hasColor;
    public Color color;
    public bool bold;
    public bool bounce;
    public int bounceIndex;
}

/// <summary>
/// A string split into the pieces that get drawn: stretches of plain text, and icons. It is cached
/// on the text sprite, and only rebuilt when something it was built from changes.
///
/// Rich text can also style its words with tags in braces: {bold}, {bounce}, and the colors {gold},
/// {red}, {teal} and {blue}. They stay on until {end}, which ends all of them. A tag takes up no room,
/// and it does not count as a character for the reveal.
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

        _builder.Build(layout, text.text ?? string.Empty, runtimeFont.font, text.richText, icons, wrapWidth, text.align);

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

        // The tags that are on right now. see TryStyle.
        private bool _hasColor, _bold, _bounce;
        private Color _color;

        // A word that comes straight after a tag ended, with no space, is the same word, like the !
        // of {gold}school{end}!, so it does not wrap away from it.
        private bool _gluedToTag;

        public void Build(RichTextLayout layout, string source, SpriteFont font, bool rich, FontIconSet icons,
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
            _hasColor = _bold = _bounce = _gluedToTag = false;

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

                if (c == '{' && rich)
                {
                    // Two in a row is how to write one that really is a brace.
                    if (i + 1 < source.Length && source[i + 1] == '{')
                    {
                        _word.Append('{');
                        i++;
                        continue;
                    }

                    var close = source.IndexOf('}', i + 1);
                    if (close > i + 1 && TryStyle(source.Substring(i + 1, close - i - 1).Trim()))
                    {
                        i = close;
                        continue;
                    }

                    if (close > i + 1
                        && icons != null
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

                // Chinese and Japanese put no spaces between words, so every character of theirs is
                // a word of its own as far as wrapping goes.
                if (_word.Length > 0 && CanBreakBetween(_word[_word.Length - 1], c))
                    PlaceWord();

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

        /// <summary>
        /// A tag turns a style on, or {end} turns them all off. The text before it is finished first,
        /// because a run has one style.
        /// </summary>
        private bool TryStyle(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "bold": PlaceWord(); FlushText(); _bold = true; return true;
                case "bounce": PlaceWord(); FlushText(); _bounce = true; return true;
                case "gold": PlaceWord(); FlushText(); SetColor(new Color(255, 222, 96)); return true;
                case "red": PlaceWord(); FlushText(); SetColor(new Color(255, 110, 90)); return true;
                case "teal": PlaceWord(); FlushText(); SetColor(new Color(110, 235, 200)); return true;
                case "blue": PlaceWord(); FlushText(); SetColor(new Color(150, 190, 255)); return true;
                case "end":
                    PlaceWord();
                    FlushText();
                    _hasColor = _bold = _bounce = false;
                    _gluedToTag = _spaces == 0;
                    return true;
            }

            return false;
        }

        private void SetColor(Color color)
        {
            _hasColor = true;
            _color = color;
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

        // Kana, the CJK ideographs and their punctuation. Not Hangul: Korean puts spaces between words.
        private static bool IsCjk(char c) =>
            (c >= '　' && c <= 'ヿ') || (c >= '㐀' && c <= '䶿') || (c >= '一' && c <= '鿿')
            || (c >= '豈' && c <= '﫿') || (c >= '＀' && c <= '￯');

        // What may not start a line (closing marks, small kana, the long vowel), and what may not end one
        // (opening marks): the usual line breaking rules of Japanese and Chinese.
        private const string NoLineStart = "、。，．・：；？！ー」』）】〕〉》｝〗〙〟’”ゝゞヽヾぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ…‥!?,.:;)]}%";
        private const string NoLineEnd = "「『（【〔〈《｛〖〘〝‘“([{";

        // A number stays with the counter after it (3手, 5步).
        private static bool CanBreakBetween(char before, char after) =>
            (IsCjk(before) || (IsCjk(after) && !char.IsLetterOrDigit(before)))
            && NoLineStart.IndexOf(after) < 0 && NoLineEnd.IndexOf(before) < 0;

        private bool ShouldWrap(float right)
        {
            // A line always takes at least one thing, even if that thing is too wide for it.
            // Otherwise a long word would wrap forever.
            return _wrapWidth > 0 && _lineHasContent && right > _wrapWidth;
        }

        private void PlaceWord()
        {
            if (_word.Length == 0) return;

            if (!_gluedToTag && ShouldWrap(_pendingX + MeasurePendingWith(includeWord: true)))
            {
                _spaces = 0;
                EndLine();
            }

            _gluedToTag = false;
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
                iconId = icon.id,
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

            if (_bounce)
            {
                // Every letter bounces on its own, so every letter is a run.
                var letters = _pending.ToString();
                for (var i = 0; i < letters.Length; i++)
                {
                    var before = i == 0 ? 0 : _font.MeasureString(letters.Substring(0, i)).X;
                    _layout.runs.Add(StyledRun(letters[i].ToString(), new Vector2(_pendingX + before, _penY), i));
                }
            }
            else
            {
                _layout.runs.Add(StyledRun(_pending.ToString(), new Vector2(_pendingX, _penY), 0));
            }

            _penX = _pendingX + MeasureWidth(_pending);
            _pendingX = _penX;
            _pending.Clear();
        }

        private RichTextRun StyledRun(string runText, Vector2 offset, int bounceIndex)
        {
            return new RichTextRun
            {
                text = runText,
                offset = offset,
                hasColor = _hasColor,
                color = _color,
                bold = _bold,
                bounce = _bounce,
                bounceIndex = bounceIndex,
            };
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
