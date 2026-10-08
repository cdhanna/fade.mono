using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    private static FontIconSet GetFontIcons(int fontId)
    {
        TextureSystem.GetSpriteFontIndex(fontId, out var fontIndex, out var runtimeFont);
        if (runtimeFont.icons == null)
        {
            runtimeFont.icons = new FontIconSet();
            TextureSystem.fonts[fontIndex] = runtimeFont;
        }

        return runtimeFont.icons;
    }

    /// <summary>
    /// <para>Peeks at the next available icon ID on a font without claiming it.</para>
    /// <para>Icon IDs belong to their font, so two fonts can both have an icon <c>1</c>.</para>
    /// </summary>
    /// <remarks>
    /// Same pattern as the other ID management commands, except that the font comes first, because
    /// every font numbers its own icons. The returned ID is not reserved. It becomes taken when you
    /// pass it to <see cref="SetFontIcon">font icon</see>.
    /// </remarks>
    /// <example>
    /// Register an icon without picking its ID by hand.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    /// free font icon id 1, ghostIcon
    /// font icon 1, ghostIcon, "ghost", 1
    ///
    /// text 1, 400, 200, 1, "watch out for the {ghost}"
    /// enable rich text 1
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="fontId">The font whose icons to look through.</param>
    /// <param name="iconId">Receives the next available icon ID for that font.</param>
    /// <returns>The next available icon ID for that font.</returns>
    /// <seealso cref="SetFontIcon">font icon</seealso>
    [FadeBasicCommand("free font icon id")]
    public static int GetFreeFontIconId(int fontId, ref int iconId)
    {
        iconId = GetFontIcons(fontId).highestIconId + 1;
        return iconId;
    }

    /// <summary>
    /// <para>Registers a picture on a font, so that rich text using the font can draw it in the middle of a line.</para>
    /// <para>If the icon ID already exists on the font, the icon is updated instead of creating a new one.</para>
    /// </summary>
    /// <remarks>
    /// An icon is a texture, or one frame of a texture, that a font knows by name. Any text sprite
    /// that uses the font and has had <see cref="EnableRichText">enable rich text</see> called on it
    /// draws the icon wherever its text has the alias in braces. The text <c>"press {z}"</c> draws
    /// the word, and then the icon whose alias is <c>z</c>. The alias is not case sensitive. An
    /// icon can also be written by its ID, as in <c>"press {1}"</c>, which is handy for an icon
    /// that has no sensible name.
    ///
    /// By default the icon is drawn as tall as a line of the font, and as wide as the shape of its
    /// frame makes it, so it grows and shrinks along with the text. Change that with
    /// <see cref="SetFontIconScale">set font icon scale</see>, and nudge it into place with
    /// <see cref="SetFontIconOffset">set font icon offset</see>. Icons keep their own colors
    /// instead of taking the color of the text, but they do fade with the text's alpha.
    ///
    /// Icons belong to the font, not to the text. Register them once after loading the font, and
    /// every rich text sprite that uses the font can draw them. To use the same pictures with a
    /// second font, register them on that font too.
    ///
    /// If a text names an icon that does not exist, the braces and the name are drawn as plain
    /// text, so a typo is easy to spot. To draw a brace on purpose in rich text, write two:
    /// <c>"{{"</c>.
    /// </remarks>
    /// <example>
    /// Draw a picture in the middle of a sentence.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    ///
    /// ` icon 1 of font 1 is the ghost texture, and text can ask for it as {ghost}
    /// font icon 1, 1, "ghost", 1
    ///
    /// text 1, 400, 200, 1, "watch out for the {ghost} over there"
    /// enable rich text 1
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Use frames of a sprite sheet as icons, and write them by ID.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    /// set texture frame grid 1, 1, 2
    ///
    /// font icon 1, 1, "left", 1, 0
    /// font icon 1, 2, "right", 1, 1
    ///
    /// text 1, 400, 200, 1, "{left} by name, {2} by id"
    /// enable rich text 1
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="fontId">The font to register the icon on.</param>
    /// <param name="iconId">The icon ID, which only has to be unique on this font. If it already exists, the icon is updated.</param>
    /// <param name="alias">The name that text uses, in braces, to draw the icon. Pass <c>""</c> for an icon that is only written by ID.</param>
    /// <param name="textureId">The texture to draw.</param>
    /// <param name="frame">Which frame of the texture to draw, if the texture has frames. Defaults to the first one.</param>
    /// <seealso cref="EnableRichText">enable rich text</seealso>
    /// <seealso cref="SetFontIconScale">set font icon scale</seealso>
    /// <seealso cref="SetFontIconOffset">set font icon offset</seealso>
    /// <seealso cref="GetFreeFontIconId">free font icon id</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    /// <seealso cref="SetTextureFramesByRowCol">set texture frame grid</seealso>
    [FadeBasicCommand("font icon")]
    public static void SetFontIcon(int fontId, int iconId, string alias, int textureId, int frame = 0)
    {
        var icons = GetFontIcons(fontId);
        icons.GetIconIndex(iconId, out var index, out var icon);
        icon.alias = alias;
        icon.textureId = textureId;
        icon.frame = frame;
        icons.SetIcon(index, icon);
    }

    /// <summary>
    /// <para>Changes how big a font's icon is drawn, as a multiple of its default size.</para>
    /// <para>At <c>1, 1</c> the icon is as tall as a line of the font.</para>
    /// </summary>
    /// <remarks>
    /// The scale multiplies the default fit, so it is not a size in pixels. That way the icon keeps
    /// its size relative to the letters when the text is resized with commands like
    /// <see cref="SizeSpriteTextAspectY">size text y</see>.
    ///
    /// A line of a font is taller than its capital letters, because it leaves room for accents and
    /// for the tails of letters like "g". If an icon looks too big next to the letters, something
    /// around <c>0.7</c> usually matches the capitals.
    ///
    /// An icon that is taller than the line does not push the lines apart. It hangs over the top
    /// and bottom of its line instead, so changing the scale never moves the text above or below.
    /// A wider icon does take up more room on its line.
    /// </remarks>
    /// <example>
    /// Shrink an icon so that it is about as tall as the capital letters.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    /// font icon 1, 1, "ghost", 1
    /// set font icon scale 1, 1, 0.7, 0.7
    ///
    /// text 1, 400, 200, 1, "a smaller {ghost}"
    /// enable rich text 1
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="fontId">The font the icon is registered on.</param>
    /// <param name="iconId">The icon ID to scale.</param>
    /// <param name="x">Width multiplier. <c>1.0</c> = the default width.</param>
    /// <param name="y">Height multiplier. <c>1.0</c> = as tall as a line of the font.</param>
    /// <seealso cref="SetFontIcon">font icon</seealso>
    /// <seealso cref="SetFontIconOffset">set font icon offset</seealso>
    [FadeBasicCommand("set font icon scale")]
    public static void SetFontIconScale(int fontId, int iconId, float x, float y)
    {
        var icons = GetFontIcons(fontId);
        icons.GetIconIndex(iconId, out var index, out var icon);
        icon.scale = new Vector2(x, y);
        icons.SetIcon(index, icon);
    }

    /// <summary>
    /// <para>Nudges where a font's icon is drawn, without moving the text around it.</para>
    /// <para>The offset is a fraction of the font's line height, so <c>0, 0.1</c> moves the icon down by a tenth of a line.</para>
    /// </summary>
    /// <remarks>
    /// Icons are centered on the font's capital letters by default. Pictures rarely have their
    /// visual middle exactly in the middle of the image, so this is the command for lining one up
    /// by eye. Positive <c>x</c> moves the icon right, and positive <c>y</c> moves it down.
    ///
    /// Like <see cref="SetFontIconScale">set font icon scale</see>, the offset is relative to the
    /// font and not a number of pixels, so the icon stays lined up when the text is resized.
    /// </remarks>
    /// <example>
    /// Drop an icon a little lower on its line.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    /// font icon 1, 1, "ghost", 1
    /// set font icon offset 1, 1, 0, 0.1
    ///
    /// text 1, 400, 200, 1, "a lower {ghost}"
    /// enable rich text 1
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="fontId">The font the icon is registered on.</param>
    /// <param name="iconId">The icon ID to move.</param>
    /// <param name="x">Horizontal nudge, as a fraction of the line height. Positive is right.</param>
    /// <param name="y">Vertical nudge, as a fraction of the line height. Positive is down.</param>
    /// <seealso cref="SetFontIcon">font icon</seealso>
    /// <seealso cref="SetFontIconScale">set font icon scale</seealso>
    [FadeBasicCommand("set font icon offset")]
    public static void SetFontIconOffset(int fontId, int iconId, float x, float y)
    {
        var icons = GetFontIcons(fontId);
        icons.GetIconIndex(iconId, out var index, out var icon);
        icon.offset = new Vector2(x, y);
        icons.SetIcon(index, icon);
    }

    /// <summary>
    /// <para>Turns on rich text for a text sprite, so that it draws its font's icons.</para>
    /// <para>Without this, braces in the text are drawn as ordinary characters.</para>
    /// </summary>
    /// <remarks>
    /// Rich text is something each text sprite opts into, so text that happens to contain a brace
    /// does not change when icons are added to its font. Once it is on, anything in the text
    /// written as <c>{name}</c> is replaced with the icon of that name from the sprite's font.
    /// Register icons with <see cref="SetFontIcon">font icon</see>.
    ///
    /// Everything else about the text sprite works the same way. The position, scale, rotation,
    /// and origin apply to the text and its icons as one block, and the commands that measure text,
    /// like <see cref="GetTextSizeX">get text size x</see> and
    /// <see cref="SizeSpriteTextAspectY">size text y</see>, count the icons. A drop shadow draws
    /// the shape of each icon in the shadow color.
    ///
    /// The setting belongs to the text sprite and survives changes to its string, so call it once
    /// and keep using <see cref="SetText">set text</see> as usual.
    /// </remarks>
    /// <example>
    /// The same string, with and without rich text.
    /// <code>
    /// font 1, "font"
    /// texture 1, "ghost"
    /// font icon 1, 1, "ghost", 1
    ///
    /// ` this one draws the braces
    /// text 1, 400, 200, 1, "plain {ghost}"
    ///
    /// ` and this one draws the picture
    /// text 2, 400, 260, 1, "rich {ghost}"
    /// enable rich text 2
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <seealso cref="DisableRichText">disable rich text</seealso>
    /// <seealso cref="SetFontIcon">font icon</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("enable rich text")]
    public static void EnableRichText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.richText = true;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Turns off rich text for a text sprite.</para>
    /// <para>The text goes back to drawing braces as ordinary characters.</para>
    /// </summary>
    /// <remarks>
    /// This is the counterpart to <see cref="EnableRichText">enable rich text</see>. The icons stay
    /// registered on the font, so turning rich text back on brings them straight back.
    /// </remarks>
    /// <param name="textId">The text sprite ID.</param>
    /// <seealso cref="EnableRichText">enable rich text</seealso>
    [FadeBasicCommand("disable rich text")]
    public static void DisableRichText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.richText = false;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Makes a text sprite wrap onto more lines when it gets wider than a number of pixels.</para>
    /// <para>Lines break between words. Pass <c>0</c> to go back to a single line.</para>
    /// </summary>
    /// <remarks>
    /// The width is measured in pixels at the size the text is drawn, so set the size of the text
    /// first and the wrap width second. Changing the scale later re-wraps the text to the same
    /// pixel width, which means the commands that size text from its measurements, like
    /// <see cref="SizeSpriteTextAspectX(int, float)">size text x</see> and
    /// <see cref="SizeSpriteTextAspectY">size text y</see>, are best called while the text is
    /// still a single line.
    ///
    /// A word that is wider than the wrap width gets a line of its own and sticks out, because
    /// words are never split. Icons in rich text wrap like words do. The spaces where a line
    /// breaks are dropped, so wrapped lines do not start or end with a gap.
    ///
    /// Once the text wraps, <see cref="GetTextSizeX">get text size x</see> is the width of the
    /// widest line, and <see cref="GetTextSizeY">get text size y</see> is the height of all the
    /// lines, which is how to size a panel around a block of text. The text's position and
    /// offset apply to the whole block. Use <see cref="SetTextAlign">set text align</see> to
    /// center the lines inside it.
    /// </remarks>
    /// <example>
    /// Wrap a sentence into a column, and draw a box behind it that fits.
    /// <code>
    /// font 1, "font"
    /// text 1, 500, 200, 1, "this sentence is far too long to fit on one line of a narrow column"
    /// set text offset 1, 0, 0
    /// scale text 1, 0.5, 0.5
    /// set text wrap 1, 220
    ///
    /// sprite 1, 500, 200, 0
    /// set sprite offset 1, 0, 0
    /// size sprite 1, get text size x(1), get text size y(1)
    /// color sprite 1, rgb(40, 40, 90)
    /// order sprite 1, 1
    /// order text 1, 2
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="width">The widest a line can be, in pixels. <c>0</c> turns wrapping off.</param>
    /// <seealso cref="SetTextAlign">set text align</seealso>
    /// <seealso cref="GetTextSizeX">get text size x</seealso>
    /// <seealso cref="GetTextSizeY">get text size y</seealso>
    /// <seealso cref="EnableRichText">enable rich text</seealso>
    [FadeBasicCommand("set text wrap")]
    public static void SetTextWrap(int textId, float width)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.wrapWidth = width;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Sets where the lines of a text sprite sit when they are not all the same width.</para>
    /// <para><c>0</c> lines them up on the left, <c>0.5</c> centers them, and <c>1</c> lines them up on the right.</para>
    /// </summary>
    /// <remarks>
    /// This only matters for text with more than one line, from
    /// <see cref="SetTextWrap">set text wrap</see> or from line breaks in the string. It moves the
    /// lines around inside the block of text. It does not move the block, which is still placed
    /// by the text's position and <see cref="SetSpriteTextOffset">set text offset</see>. To center
    /// a paragraph on a point, set the offset to <c>0.5</c> so that the block is centered on the
    /// point, and the align to <c>0.5</c> so that the lines are centered in the block.
    /// </remarks>
    /// <example>
    /// A centered paragraph.
    /// <code>
    /// font 1, "font"
    /// text 1, 640, 300, 1, "a short line and then a much longer line to follow it"
    /// scale text 1, 0.5, 0.5
    /// set text wrap 1, 260
    /// set text offset 1, 0.5, 0.5
    /// set text align 1, 0.5
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="align">Where the lines sit. <c>0</c> = left, <c>0.5</c> = centered, <c>1</c> = right.</param>
    /// <seealso cref="SetTextWrap">set text wrap</seealso>
    /// <seealso cref="SetSpriteTextOffset">set text offset</seealso>
    [FadeBasicCommand("set text align")]
    public static void SetTextAlign(int textId, float align)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.align = align;
        TextSystem.textSprites[index] = textSprite;
    }
}
