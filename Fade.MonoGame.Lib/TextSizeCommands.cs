using System;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    // Every `size text...` command remembers itself on the text, so that a loc text is sized again when
    // its string changes. See TextSizing in Fade.MonoGame.Game.
    private static void RememberSize(ref TextSprite text, TextSizeMode mode, float x, float y, float min = 0, float max = 0)
    {
        text.sizeMode = mode;
        text.sizeX = x;
        text.sizeY = y;
        text.sizeMin = min;
        text.sizeMax = max;
    }

    /// <summary>
    /// <para>Sizes a text to be as big as it can be inside a box, without stretching it: never taller than <paramref name="yPixels"/>, and never wider than <paramref name="xPixels"/>.</para>
    /// <para>The height is the size you want; the width only ever makes it smaller. A short string comes out exactly as <c>size text y</c> would make it, and a long one shrinks until it fits.</para>
    /// </summary>
    /// <remarks>
    /// This is the one to use where space is tight, and above all for text that is translated. The
    /// same label can be twice as long in another language: "Rounds Left" is "Rodadas Restantes" in
    /// Portuguese. With <c>size text y</c> the long one runs into whatever is next to it; with
    /// <c>size text x</c> a short one grows to fill the width. <c>size text max</c> keeps both at the
    /// height you picked, and only shrinks the one that would not fit.
    ///
    /// The size is worked out again whenever the text of a loc text changes, because the language
    /// changed or one of its args did. That is true of every <c>size text</c> command: each one is
    /// remembered and done again. A plain <c>set text</c> keeps the size it had, as it always did, so
    /// call <c>size text max</c> again after one if the new string may be longer.
    ///
    /// The text is rounded down to a whole number of pixels high, so that letters drawn small from a
    /// big font stay sharp.
    ///
    /// Text that wraps (see <c>set text wrap</c>) wraps at its own width already, so for that the
    /// box's height is what matters: the text shrinks until all of its lines fit in it.
    ///
    /// While you work on the game, a loc text that has to shrink to less than four fifths of its
    /// height says so in the log, with its key and language, so that the strings that are too long
    /// for their spot can be found by playing in each language.
    ///
    /// <c>scale text</c> sets the scale by hand and forgets any <c>size text</c> command.
    /// </remarks>
    /// <example>
    /// A label that is 20 pixels high in any language, unless it would be wider than 150:
    /// <code>
    /// load strings "Fish/Lang"
    /// font 1, "Fish/Fonts/ZeroCool"
    ///
    /// text loc 1, 538, 16, 1, "hud.roundsleft"
    /// set text offset 1, 1, 0
    /// size text max 1, 150, 20
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="textId">The text's ID.</param>
    /// <param name="xPixels">The most it can be wide, in pixels.</param>
    /// <param name="yPixels">How high it should be, and the most it can be, in pixels.</param>
    /// <seealso cref="SizeSpriteTextAspectY">size text y</seealso>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="TextLoc">text loc</seealso>
    [FadeBasicCommand("size text max")]
    public static void SizeTextMax(int textId, float xPixels, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        RememberSize(ref textSprite, TextSizeMode.Max, xPixels, yPixels);
        TextSystem.textSprites[index] = textSprite;

        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text max {textId}, {xPixels}, {yPixels}` has no effect yet, because the text has no font");
            return;
        }
        LocalizationSystem.ReportFit(index, TextSizing.Apply(index));
    }
}
