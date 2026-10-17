using System;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;

/// <summary>Which `size text...` command last sized a text. See TextSprite.sizeMode.</summary>
public enum TextSizeMode : byte
{
    None,

    /// <summary>`size text`: to sizeX by sizeY, stretched.</summary>
    Stretch,

    /// <summary>`size text x`: sizeX wide, keeping the aspect.</summary>
    AspectX,

    /// <summary>`size text x` with bounds: sizeX wide, the scale kept from sizeMin to sizeMax.</summary>
    AspectXClamped,

    /// <summary>`size text y`: sizeY high, keeping the aspect.</summary>
    AspectY,

    /// <summary>`size text max`: as big as fits in sizeX by sizeY, keeping the aspect.</summary>
    Max,
}

/// <summary>
/// The sizing behind the `size text...` commands. A text remembers which one it was given, so the
/// localization system can size it again when its string changes: a size worked out for "Moves" is
/// wrong for "Jogadas".
/// </summary>
public static class TextSizing
{
    /// <summary>
    /// Sizes the text at an index by its remembered mode. Returns how big it ended up compared with
    /// the height it was asked for (1 when it got all of it), which is only below 1 for
    /// <see cref="TextSizeMode.Max"/> text that had to shrink to fit its width.
    /// </summary>
    public static float Apply(int index)
    {
        ref var text = ref TextSystem.textSprites[index];
        if (text.sizeMode == TextSizeMode.None) return 1;

        TextureSystem.GetSpriteFontIndex(text.sprite.imageId, out _, out var font);
        if (font.font == null) return 1;

        if (text.sizeMode == TextSizeMode.Max) return ApplyMax(ref text, ref font);

        var size = RichTextSystem.Measure(ref text, ref font);
        if (size.X <= 0 || size.Y <= 0) return 1; // nothing to size by yet

        switch (text.sizeMode)
        {
            case TextSizeMode.Stretch:
                text.sprite.scale = new Vector2(text.sizeX / size.X, text.sizeY / size.Y);
                break;
            case TextSizeMode.AspectX:
                text.sprite.scale = Vector2.One * (text.sizeX / size.X);
                break;
            case TextSizeMode.AspectXClamped:
                text.sprite.scale = Vector2.One * Math.Clamp(text.sizeX / size.X, text.sizeMin, text.sizeMax);
                break;
            case TextSizeMode.AspectY:
                text.sprite.scale = Vector2.One * (text.sizeY / size.Y);
                break;
        }
        return 1;
    }

    /// <summary>
    /// The biggest uniform scale at which the text is no more than sizeX wide and sizeY high. The
    /// height is the size that is wanted; the width only ever makes it smaller.
    ///
    /// Text that wraps lays out differently at every scale (the wrap width is in pixels on screen),
    /// so for that the scale is found by halving the range a few times. Text that does not wrap is
    /// the same shape at any scale, so it is one division.
    ///
    /// The scale is rounded down so that the text is a whole number of pixels high. The fonts are
    /// baked big and drawn small with point sampling, and at an in-between scale the rows of pixels
    /// in the letters come out uneven.
    /// </summary>
    private static float ApplyMax(ref TextSprite text, ref RuntimeFont font)
    {
        var maxX = text.sizeX;
        var maxY = text.sizeY;
        if (maxX <= 0 || maxY <= 0) return 1;

        float wanted;
        float scale;
        if (text.wrapWidth <= 0)
        {
            var size = RichTextSystem.Measure(ref text, ref font);
            if (size.X <= 0 || size.Y <= 0) return 1;
            wanted = maxY / size.Y;
            scale = Math.Min(wanted, maxX / size.X);
            scale = SnapToPixels(scale, size.Y);
        }
        else
        {
            // Wrapped: as big as one line of the font being maxY high, at most.
            wanted = maxY / Math.Max(1, font.font.LineSpacing);
            if (Fits(ref text, ref font, wanted, maxX, maxY)) scale = wanted;
            else
            {
                var lo = 0f;
                var hi = wanted;
                for (var i = 0; i < 12; i++)
                {
                    var mid = (lo + hi) / 2;
                    if (Fits(ref text, ref font, mid, maxX, maxY)) lo = mid;
                    else hi = mid;
                }
                scale = lo;
            }
            scale = SnapToPixels(scale, font.font.LineSpacing);
        }

        if (scale <= 0) return 1;
        text.sprite.scale = new Vector2(scale, scale);
        return wanted > 0 ? scale / wanted : 1;
    }

    // Whether wrapped text, laid out at this scale, fits in the box.
    private static bool Fits(ref TextSprite text, ref RuntimeFont font, float scale, float maxX, float maxY)
    {
        text.sprite.scale = new Vector2(scale, scale);
        var size = RichTextSystem.Measure(ref text, ref font);
        return size.X * scale <= maxX + .5f && size.Y * scale <= maxY + .5f;
    }

    // The largest scale no bigger than `scale` at which `height` font units are a whole number of pixels.
    private static float SnapToPixels(float scale, float height)
    {
        if (height <= 0) return scale;
        var pixels = MathF.Floor(height * scale + .001f);
        return pixels >= 1 ? pixels / height : scale;
    }
}
