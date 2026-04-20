using System.Diagnostics;
using Fade.MonoGame.Core;
using FadeBasic.Lib.Standard.Util;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Peeks at the next available text sprite ID without claiming it.</para>
    /// <para>The returned ID is not reserved, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Same pattern as the sprite ID management commands. Call this when you need to know what ID
    /// will be assigned next but aren't ready to create the text sprite yet. If you actually want
    /// to lock in the ID, use <see cref="ReserveTextNextId">reserve text id</see> instead.
    /// </remarks>
    /// <example>
    /// Check what the next text ID will be before creating it.
    /// <code>
    /// ` peek at the next available text ID
    /// nextId = free text id()
    /// print "Next text ID will be: " + str(nextId)
    /// </code>
    /// </example>
    /// <param name="textId">Receives the next available text ID.</param>
    /// <returns>The next available text ID.</returns>
    /// <seealso cref="ReserveTextNextId">reserve text id</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("free text id")]
    public static int GetFreeTextNextId(ref int textId)
    {
        textId = TextSystem.highestTextId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textId;
    }

    /// <summary>
    /// <para>Claims the next available text sprite ID and initializes its slot.</para>
    /// <para>Unlike <see cref="GetFreeTextNextId">free text id</see>, this actually reserves the ID so nothing else can take it.</para>
    /// </summary>
    /// <remarks>
    /// Same pattern as the sprite ID reservation. Use this when you want to set up an ID ahead of time
    /// before calling <see cref="Text">text</see> to fill in the details. Handy if you need to wire up
    /// references between text sprites before they're fully configured.
    /// </remarks>
    /// <example>
    /// Reserve a text ID ahead of time, then create the text later.
    /// <code>
    /// ` reserve the ID so nothing else grabs it
    /// myTextId = reserve text id()
    ///
    /// ` later, use the reserved ID to create the text
    /// font 1, "Fonts/Arial"
    /// text myTextId, 100, 50, 1, "Hello!"
    /// </code>
    /// </example>
    /// <param name="textId">Receives the reserved text ID.</param>
    /// <returns>The reserved text ID.</returns>
    /// <seealso cref="GetFreeTextNextId">free text id</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    [FadeBasicCommand("reserve text id")]
    public static int ReserveTextNextId(ref int textId)
    {
        GetFreeTextNextId(ref textId);
        TextSystem.GetTextSpriteIndex(textId, out _, out _);
        return textId;
    }


    /// <summary>
    /// <para>Creates a text sprite with a position, font, and string content.</para>
    /// <para>If the ID already exists, it updates the existing text sprite instead of creating a new one.</para>
    /// </summary>
    /// <remarks>
    /// This is the main entry point for getting text on screen. You need a font loaded via
    /// <see cref="LoadSpriteFont">font</see> first, or you'll get nothing. The text sprite won't
    /// actually appear until the next <see cref="Sync">sync</see>. Text sprites work almost
    /// identically to regular sprites. They share the same rendering pipeline for z-ordering,
    /// render targets, transforms, etc.
    /// </remarks>
    /// <example>
    /// Create a simple text sprite and display it.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Hello World!"
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Update an existing text sprite by reusing the same ID.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "First message"
    /// sync
    /// wait 1000
    /// ` reusing ID 1 updates the text in place
    /// text 1, 100, 50, 1, "Updated message"
    /// sync
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID. If it already exists, the sprite is updated.</param>
    /// <param name="x">X position in pixels.</param>
    /// <param name="y">Y position in pixels.</param>
    /// <param name="spriteFontId">The sprite font ID returned by <see cref="LoadSpriteFont">font</see>.</param>
    /// <param name="text">The string to display.</param>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    /// <seealso cref="SetText">set text</seealso>
    /// <seealso cref="SetTextPosition">set text position</seealso>
    /// <seealso cref="SetTextColor">color text</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    /// <seealso cref="SetTextOrder">order text</seealso>
    [FadeBasicCommand("text")]
    public static void Text(int textId, int x, int y, int spriteFontId, string text)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.position = new Vector2(x, y);
        textSprite.sprite.imageId = spriteFontId;
        textSprite.text = text;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Updates the displayed string of an existing text sprite.</para>
    /// <para>This changes only the text content. Position, color, scale, and everything else stay the same.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to change what a text sprite says without tearing it down and recreating it.
    /// For example, updating a score counter or a status label every frame. If you haven't created the
    /// text sprite yet, call <see cref="Text">text</see> first. If you also need to resize the sprite
    /// to fit the new string, follow up with <see cref="SizeText">size text</see> or
    /// <see cref="SizeSpriteTextAspectX(int, float)">size text x</see> since the scale won't
    /// automatically adjust to the new content.
    /// </remarks>
    /// <example>
    /// Update a score display every frame.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 10, 10, 1, "Score: 0"
    /// score = 0
    /// DO
    ///   score = score + 1
    ///   set text 1, "Score: " + str(score)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to update.</param>
    /// <param name="text">The new string to display.</param>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    [FadeBasicCommand("set text")]
    public static void SetText(int textId, string text)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.text = text;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Moves a text sprite to a new screen position.</para>
    /// <para>This is the text equivalent of <see cref="PositionSprite">position sprite</see>.</para>
    /// </summary>
    /// <remarks>
    /// Call this whenever you need to reposition a text sprite. Use it every frame for animation, or once
    /// for static placement. The position is in screen pixels and represents the top-left corner by
    /// default, but that changes if you've set a custom origin with
    /// <see cref="SetSpriteTextOffset">set text offset</see>. If the text sprite is attached to a
    /// transform via <see cref="SetSpriteTextRelativeToAnother">attach text to transform</see>,
    /// this position becomes relative to that transform.
    /// </remarks>
    /// <example>
    /// Animate a text sprite moving across the screen.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 0, 100, 1, "Moving text!"
    /// xPos = 0
    /// DO
    ///   xPos = xPos + 2
    ///   set text position 1, xPos, 100
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to move.</param>
    /// <param name="x">New X position in pixels.</param>
    /// <param name="y">New Y position in pixels.</param>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SetSpriteTextOffset">set text offset</seealso>
    /// <seealso cref="SetSpriteTextRelativeToAnother">attach text to transform</seealso>
    /// <seealso cref="TextX">text x</seealso>
    /// <seealso cref="TextY">text y</seealso>
    [FadeBasicCommand("set text position")]
    public static void SetTextPosition(int textId, int x, int y)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.position = new Vector2(x, y);
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Sets the color of a text sprite using a packed RGBA color value.</para>
    /// <para>This replaces the current color entirely, alpha included. Use
    /// <see cref="SetTextDiffuse">set text alpha</see> if you only want to change transparency.</para>
    /// </summary>
    /// <remarks>
    /// The color value is a packed integer in RGBA format. This works just like
    /// <see cref="ColorSprite">color sprite</see> but for text. The color tints the rendered
    /// glyphs, so white (<c>0xFFFFFFFF</c>) shows the font's original appearance. If the text
    /// sprite has a drop shadow enabled, use <see cref="SetTextDropShadowColor">color text drop shadow</see>
    /// to color the shadow independently.
    /// </remarks>
    /// <example>
    /// Color text red and display it.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Warning!"
    /// ` red with full opacity
    /// color text 1, 0xFF0000FF
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to color.</param>
    /// <param name="colorCode">Packed RGBA color value.</param>
    /// <seealso cref="SetTextDiffuse">set text alpha</seealso>
    /// <seealso cref="SetTextDropShadowColor">color text drop shadow</seealso>
    /// <seealso cref="EnableTextDropShadow">enable text drop shadow</seealso>
    [FadeBasicCommand("color text")]
    public static void SetTextColor(int textId, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.sprite.color = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Sets the color of a text sprite's drop shadow independently from the main text color.</para>
    /// <para>The drop shadow must already be enabled via <see cref="EnableTextDropShadow">enable text drop shadow</see>
    /// for this to have any visible effect.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you want to change just the shadow color without touching the offset or toggling
    /// the shadow on/off. A common pattern is a dark, semi-transparent shadow. Pack your RGBA with
    /// a low alpha for a subtle effect. The shadow is drawn as a second copy of the text at the offset
    /// you specified when enabling it, so this color applies to that entire second copy.
    /// </remarks>
    /// <example>
    /// Change a drop shadow to a subtle blue after enabling it.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Shadow text"
    /// enable text drop shadow 1, 2, 2, 0x000000FF
    /// ` change the shadow color to dark blue with half opacity
    /// color text drop shadow 1, 0x000088AA
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID whose shadow color to change.</param>
    /// <param name="colorCode">Packed RGBA color value for the shadow.</param>
    /// <seealso cref="EnableTextDropShadow">enable text drop shadow</seealso>
    /// <seealso cref="DisableTextDropShadow">disable text drop shadow</seealso>
    /// <seealso cref="SetTextColor">color text</seealso>
    [FadeBasicCommand("color text drop shadow")]
    public static void SetTextDropShadowColor(int textId, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.dropShadowColor = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Enables a drop shadow on a text sprite and configures its offset and color in one call.</para>
    /// <para>The shadow is drawn as a second copy of the text rendered behind the original at the given pixel offset.</para>
    /// </summary>
    /// <remarks>
    /// Drop shadows make text more readable over busy backgrounds. The shadow is literally the same
    /// string drawn again at <c>(x, y)</c> pixels from the original position, using the color you
    /// provide here. Common values are small offsets like <c>(2, 2)</c> with a dark or black color.
    /// Once enabled, you can tweak just the color later with
    /// <see cref="SetTextDropShadowColor">color text drop shadow</see>, or turn it off entirely
    /// with <see cref="DisableTextDropShadow">disable text drop shadow</see>. The shadow respects
    /// the text sprite's scale, rotation, and render target assignment.
    /// </remarks>
    /// <example>
    /// Add a black drop shadow offset by 2 pixels in each direction.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Readable text"
    /// ` black shadow, 2 pixels down and right
    /// enable text drop shadow 1, 2, 2, 0x000000FF
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Use a soft, semi-transparent shadow for a subtler effect.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 200, 100, 1, "Soft shadow"
    /// ` dark gray shadow with half opacity, offset 1 pixel
    /// enable text drop shadow 1, 1, 1, 0x33333388
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="x">Shadow X offset in pixels from the text position.</param>
    /// <param name="y">Shadow Y offset in pixels from the text position.</param>
    /// <param name="colorCode">Packed RGBA color value for the shadow.</param>
    /// <seealso cref="SetTextDropShadowColor">color text drop shadow</seealso>
    /// <seealso cref="DisableTextDropShadow">disable text drop shadow</seealso>
    /// <seealso cref="SetTextColor">color text</seealso>
    [FadeBasicCommand("enable text drop shadow")]
    public static void EnableTextDropShadow(int textId, int x, int y, int colorCode)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        ColorUtil.UnpackColor(colorCode, out var r, out var g, out var b, out var a);
        textSprite.dropShadowEnabled = true;
        textSprite.dropShadowOffset = new Vector2(x, y);
        textSprite.dropShadowColor = new Color(r, g, b, a);
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Disables the drop shadow on a text sprite.</para>
    /// <para>The shadow settings (offset, color) are preserved, so re-enabling later restores the previous look.</para>
    /// </summary>
    /// <remarks>
    /// Use this to turn off a shadow you previously enabled with
    /// <see cref="EnableTextDropShadow">enable text drop shadow</see>. This is a visibility toggle
    /// only. It doesn't clear the offset or color, so calling
    /// <see cref="EnableTextDropShadow">enable text drop shadow</see> again will bring back the
    /// same shadow without needing to reconfigure it.
    /// </remarks>
    /// <example>
    /// Toggle a drop shadow on and off.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Toggle shadow"
    /// enable text drop shadow 1, 2, 2, 0x000000FF
    /// sync
    /// wait 2000
    /// ` turn off the shadow; settings are preserved
    /// disable text drop shadow 1
    /// sync
    /// wait 2000
    /// ` re-enable with the same offset and color
    /// enable text drop shadow 1, 2, 2, 0x000000FF
    /// sync
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID whose shadow to disable.</param>
    /// <seealso cref="EnableTextDropShadow">enable text drop shadow</seealso>
    /// <seealso cref="SetTextDropShadowColor">color text drop shadow</seealso>
    [FadeBasicCommand("disable text drop shadow")]
    public static void DisableTextDropShadow(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.dropShadowEnabled = false;
        TextSystem.textSprites[index] = textSprite;
    }


    /// <summary>
    /// <para>Sets the transparency of a text sprite.</para>
    /// <para><c>0</c> is fully transparent (invisible) and <c>255</c> is fully opaque.</para>
    /// </summary>
    /// <remarks>
    /// This modifies only the alpha channel, leaving the RGB color untouched. If you need to
    /// change both color and alpha at once, use <see cref="SetTextColor">color text</see> instead
    /// since that takes a packed RGBA value. Useful for fade-in/fade-out effects; just tween the
    /// alpha value each frame. The drop shadow (if enabled) is not affected by this; it uses
    /// the alpha from its own color set via <see cref="SetTextDropShadowColor">color text drop shadow</see>
    /// or <see cref="EnableTextDropShadow">enable text drop shadow</see>.
    /// </remarks>
    /// <example>
    /// Fade text in from transparent to fully opaque.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Fading in..."
    /// a = 0
    /// DO
    ///   set text alpha 1, a
    ///   IF a &lt; 255 THEN a = a + 5 ENDIF
    ///   IF a &gt; 255 THEN a = 255 ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="alpha">Alpha value from <c>0</c> (transparent) to <c>255</c> (opaque).</param>
    /// <seealso cref="SetTextColor">color text</seealso>
    /// <seealso cref="SetTextDropShadowColor">color text drop shadow</seealso>
    /// <seealso cref="EnableTextDropShadow">enable text drop shadow</seealso>
    [FadeBasicCommand("set text alpha")]
    public static void SetTextDiffuse(int textId, byte alpha)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.color.A = alpha;
        TextSystem.textSprites[index] = textSprite;
    }


    /// <summary>
    /// <para>Sets the X and Y scale factors of a text sprite directly.</para>
    /// <para>A scale of <c>1.0</c> is the font's native size; values below shrink, above enlarge.</para>
    /// </summary>
    /// <remarks>
    /// This gives you direct control over the scale, unlike <see cref="SizeText">size text</see>
    /// which calculates the scale from a target pixel size. You can set different X and Y values
    /// to stretch the text non-uniformly, but that usually looks bad for readable text. If you
    /// want uniform scaling to a target pixel width or height, use
    /// <see cref="SizeSpriteTextAspectX(int, float)">size text x</see> or
    /// <see cref="SizeSpriteTextAspectY">size text y</see> instead.
    /// </remarks>
    /// <example>
    /// Double the size of a text sprite.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Big text"
    /// ` scale to twice the native font size
    /// scale text 1, 2.0, 2.0
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="x">Scale factor on the X axis. <c>1.0</c> = native size.</param>
    /// <param name="y">Scale factor on the Y axis. <c>1.0</c> = native size.</param>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    /// <seealso cref="SizeSpriteTextAspectY">size text y</seealso>
    [FadeBasicCommand("scale text")]
    public static void SetTextScale(int textId, float x, float y)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.scale = new Vector2(x, y);
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Sets the draw order (z-order) for a text sprite.</para>
    /// <para>Higher values draw on top of lower values, just like regular sprites.</para>
    /// </summary>
    /// <remarks>
    /// Text sprites and regular sprites share the same z-order space within a render target,
    /// so you can interleave them. For example, a text sprite with order <c>10</c> draws on top of
    /// a regular <see cref="Sprite">sprite</see> with order <c>5</c>. Setting the order marks
    /// the render target's sprite list as dirty, so it will be re-sorted before the next draw.
    /// </remarks>
    /// <example>
    /// Layer text on top of a sprite using z-order.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// ` create a sprite and a text label
    /// sprite 1, 100, 100, loadImage("background.png")
    /// order sprite 1, 5
    /// text 1, 110, 110, 1, "On top!"
    /// order text 1, 10
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="order">The z-order value. Higher = drawn on top.</param>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="HideSpriteText">hide text</seealso>
    /// <seealso cref="ShowpriteText">show text</seealso>
    /// <seealso cref="SetSpriteTextRenderTarget">set text render target</seealso>
    [FadeBasicCommand("order text")]
    public static void SetTextOrder(int textId, int order)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.zOrder = order;
        TextSystem.textSprites[index] = textSprite;
        RenderSystem.GetOutputIndex(textSprite.sprite.outputIdFlags, out _, out var output);
        output.spritesOrderDirty = true;
    }


    /// <summary>
    /// <para>Hides a text sprite so it is not drawn.</para>
    /// <para>The text sprite still exists and keeps all its properties. It just becomes invisible.</para>
    /// </summary>
    /// <remarks>
    /// Use this instead of destroying and recreating text sprites when you need to toggle visibility.
    /// The sprite stays in memory with its position, color, scale, and everything else intact.
    /// Call <see cref="ShowpriteText">show text</see> to make it visible again. This is the text
    /// equivalent of hiding a regular <see cref="Sprite">sprite</see>.
    /// </remarks>
    /// <example>
    /// Hide a text sprite and show it again after a delay.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Now you see me"
    /// sync
    /// wait 2000
    /// hide text 1
    /// sync
    /// wait 2000
    /// show text 1
    /// sync
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to hide.</param>
    /// <seealso cref="ShowpriteText">show text</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SetTextOrder">order text</seealso>
    [FadeBasicCommand("hide text")]
    public static void HideSpriteText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.hidden = true;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Makes a previously hidden text sprite visible again.</para>
    /// <para>Has no effect if the text sprite is already visible.</para>
    /// </summary>
    /// <remarks>
    /// This is the counterpart to <see cref="HideSpriteText">hide text</see>. The text sprite
    /// reappears exactly as it was before hiding, with the same position, color, scale, render target,
    /// and everything else. You don't need to reconfigure anything after showing it.
    /// </remarks>
    /// <example>
    /// Show a hidden text sprite.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Hidden at first"
    /// hide text 1
    /// sync
    /// wait 1000
    /// ` make it visible again
    /// show text 1
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to show.</param>
    /// <seealso cref="HideSpriteText">hide text</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SetTextOrder">order text</seealso>
    [FadeBasicCommand("show text")]
    public static void ShowpriteText(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.hidden = false;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Assigns a text sprite to draw on a specific render target.</para>
    /// <para>This replaces any previous render target assignment. The text sprite will only draw to the new target.</para>
    /// </summary>
    /// <remarks>
    /// By default, text sprites draw to the main screen (render target <c>1</c>). Use this to
    /// redirect a text sprite to a different render target created with
    /// <see cref="SetRenderTargetTexture">render target</see>. This works the same way as
    /// render target assignment for regular sprites. If you want the text sprite to appear on
    /// multiple render targets simultaneously, use
    /// <see cref="AddSpriteTextRenderTarget">add text render target</see> instead. To go back
    /// to the default, call <see cref="ResetSpriteTextRenderTarget">reset text render target</see>.
    /// </remarks>
    /// <example>
    /// Draw text onto a custom render target.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// ` create a 256x256 render target
    /// rtId = render target(256, 256)
    /// text 1, 10, 10, 1, "On render target"
    /// ` redirect text to the custom target
    /// set text render target 1, rtId
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="outputId">The render target ID to draw to.</param>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    /// <seealso cref="ResetSpriteTextRenderTarget">reset text render target</seealso>
    /// <seealso cref="AddSpriteTextRenderTarget">add text render target</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("set text render target")]
    public static void SetSpriteTextRenderTarget(int textId, int outputId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        RenderSystem.SetSpriteTextToOutput(textId, outputId, textSprite.sprite.outputIdFlags);
        textSprite.sprite.outputIdFlags = outputId;
        TextSystem.textSprites[index] = textSprite;
    }
    /// <summary>
    /// <para>Resets a text sprite to draw on the default render target (the main screen).</para>
    /// <para>This removes any custom render target assignment.</para>
    /// </summary>
    /// <remarks>
    /// Equivalent to calling <see cref="SetSpriteTextRenderTarget">set text render target</see>
    /// with output ID <c>1</c>. Use this when you're done drawing a text sprite to an off-screen
    /// render target and want it back on the main screen.
    /// </remarks>
    /// <example>
    /// Move text back to the main screen after drawing to a custom render target.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// rtId = render target(256, 256)
    /// text 1, 10, 10, 1, "Temporary"
    /// set text render target 1, rtId
    /// sync
    /// ` move it back to the main screen
    /// reset text render target 1
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to reset.</param>
    /// <seealso cref="SetSpriteTextRenderTarget">set text render target</seealso>
    /// <seealso cref="AddSpriteTextRenderTarget">add text render target</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("reset text render target")]
    public static void ResetSpriteTextRenderTarget(int textId)
    {
        SetSpriteTextRenderTarget(textId, 1);
    }
    /// <summary>
    /// <para>Adds an additional render target for a text sprite without removing existing ones.</para>
    /// <para>The text sprite will draw to all assigned render targets each frame.</para>
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="SetSpriteTextRenderTarget">set text render target</see> which replaces
    /// the assignment, this stacks on top of whatever targets the text sprite already draws to.
    /// Useful when you want the same text to appear on the main screen and also on an off-screen
    /// render target (e.g., a minimap or a UI overlay). Works the same way as adding render
    /// targets to regular sprites.
    /// </remarks>
    /// <example>
    /// Draw the same text on both the main screen and a custom render target.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// rtId = render target(256, 256)
    /// text 1, 10, 10, 1, "Everywhere!"
    /// ` text already draws to the main screen by default;
    /// ` add it to the custom target as well
    /// add text render target 1, rtId
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="outputId">The additional render target ID to add.</param>
    /// <seealso cref="SetSpriteTextRenderTarget">set text render target</seealso>
    /// <seealso cref="ResetSpriteTextRenderTarget">reset text render target</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target</seealso>
    [FadeBasicCommand("add text render target")]
    public static void AddSpriteTextRenderTarget(int textId, int outputId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);

        RenderSystem.AddSpriteTextToOutput(index, outputId, textSprite.sprite.outputIdFlags);
        textSprite.sprite.outputIdFlags = SpriteSystem.AddIdToFlags(outputId, textSprite.sprite.outputIdFlags);
        TextSystem.textSprites[index] = textSprite;

    }


    /// <summary>
    /// <para>Scales a text sprite to fit exact pixel dimensions for both width and height.</para>
    /// <para>This calculates independent X and Y scale factors, so the text may stretch non-uniformly.</para>
    /// </summary>
    /// <remarks>
    /// The command measures the text string using the assigned font and then computes the scale
    /// needed to fill the target rectangle. Because X and Y are calculated independently, the
    /// text will distort if the aspect ratio doesn't match. If you want to scale uniformly
    /// (preserving the font's aspect ratio), use
    /// <see cref="SizeSpriteTextAspectX(int, float)">size text x</see> or
    /// <see cref="SizeSpriteTextAspectY">size text y</see> instead. If you change the text
    /// content with <see cref="SetText">set text</see>, you'll need to call this again since
    /// the measured size will be different.
    /// </remarks>
    /// <example>
    /// Scale text to fill a 200x50 pixel box.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 50, 50, 1, "Stretched to fit"
    /// ` scale to exactly 200 wide by 50 tall (may stretch)
    /// size text 1, 200, 50
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="xPixels">Target width in pixels.</param>
    /// <param name="yPixels">Target height in pixels.</param>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    /// <seealso cref="SizeSpriteTextAspectY">size text y</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    /// <seealso cref="SetText">set text</seealso>
    [FadeBasicCommand("size text")]
    public static void SizeText(int textId, float xPixels, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);

        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);

        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        var yRatio = yPixels / size.Y;

        textSprite.sprite.scale = new Vector2(xRatio, yRatio);
        TextSystem.textSprites[index] = textSprite;
    }


    /// <summary>
    /// <para>Scales a text sprite to a target width in pixels, scaling uniformly to maintain aspect ratio.</para>
    /// <para>Both X and Y scale are set to the same value, so the text won't stretch or squish.</para>
    /// </summary>
    /// <remarks>
    /// This measures the text string's natural width and calculates a uniform scale factor so
    /// the rendered width matches <paramref name="xPixels"/>. The height scales proportionally.
    /// If the font hasn't been assigned yet, this logs a warning and does nothing. For the
    /// height-based equivalent, see <see cref="SizeSpriteTextAspectY">size text y</see>. If
    /// you need to clamp the resulting scale to a range (e.g., to prevent text from getting
    /// absurdly large or tiny), use the overload
    /// <see cref="SizeSpriteTextAspectX(int, float, float, float)">size text x</see> that
    /// takes min and max parameters.
    /// </remarks>
    /// <example>
    /// Scale text uniformly to fit a 300-pixel width.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 50, 50, 1, "Uniform scale"
    /// ` scale so the width is exactly 300 pixels; height adjusts proportionally
    /// size text x 1, 300
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="xPixels">Target width in pixels.</param>
    /// <seealso cref="SizeSpriteTextAspectX(int, float, float, float)">size text x</seealso>
    /// <seealso cref="SizeSpriteTextAspectY">size text y</seealso>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    [FadeBasicCommand("size text x")]
    public static void SizeSpriteTextAspectX(int textId, float xPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text x {textId}, {xPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        textSprite.sprite.scale = new Vector2(xRatio, xRatio);
        TextSystem.textSprites[index] = textSprite;

    }

    /// <summary>
    /// <para>Scales a text sprite to a target width in pixels with clamped scale bounds, maintaining aspect ratio.</para>
    /// <para>The computed scale is clamped between <paramref name="min"/> and <paramref name="max"/>,
    /// preventing the text from becoming too small or too large.</para>
    /// </summary>
    /// <remarks>
    /// Works like the unclamped <see cref="SizeSpriteTextAspectX(int, float)">size text x</see>,
    /// but after computing the scale factor it clamps the result to the
    /// <c>[min, max]</c> range. This is useful when you have dynamic text (like player names or
    /// scores) that varies wildly in length. You can target a fixed width but guarantee the
    /// text never scales below a readable minimum or above a maximum that breaks your layout.
    /// If the font hasn't been assigned yet, this logs a warning and does nothing.
    /// </remarks>
    /// <example>
    /// Size text to 200 pixels wide, but clamp the scale between 0.5 and 2.0.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 50, 50, 1, "Clamped scale"
    /// ` target 200px wide, but never shrink below 0.5 or grow above 2.0
    /// size text x 1, 200, 0.5, 2.0
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="xPixels">Target width in pixels.</param>
    /// <param name="min">Minimum allowed scale factor.</param>
    /// <param name="max">Maximum allowed scale factor.</param>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    /// <seealso cref="SizeSpriteTextAspectY">size text y</seealso>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    [FadeBasicCommand("size text x")]
    public static void SizeSpriteTextAspectX(int textId, float xPixels, float min, float max)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text x {textId}, {xPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var xRatio = xPixels / size.X;
        xRatio = Math.Clamp(xRatio, min, max);

        textSprite.sprite.scale = new Vector2(xRatio, xRatio);
        TextSystem.textSprites[index] = textSprite;

    }


    /// <summary>
    /// <para>Scales a text sprite to a target height in pixels, scaling uniformly to maintain aspect ratio.</para>
    /// <para>Both X and Y scale are set to the same value, so the text won't stretch or squish.</para>
    /// </summary>
    /// <remarks>
    /// This is the height-based counterpart to
    /// <see cref="SizeSpriteTextAspectX(int, float)">size text x</see>. It measures the text
    /// string's natural height and calculates a uniform scale factor so the rendered height
    /// matches <paramref name="yPixels"/>. The width scales proportionally. If the font
    /// hasn't been assigned yet, this logs a warning and does nothing. Handy when you want
    /// text to fit a fixed vertical space (like a UI row) regardless of the string length.
    /// </remarks>
    /// <example>
    /// Scale text to fit a 40-pixel tall row.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 50, 50, 1, "Fit the row"
    /// ` scale so the height is exactly 40 pixels; width adjusts proportionally
    /// size text y 1, 40
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="yPixels">Target height in pixels.</param>
    /// <seealso cref="SizeSpriteTextAspectX(int, float)">size text x</seealso>
    /// <seealso cref="SizeText">size text</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    [FadeBasicCommand("size text y")]
    public static void SizeSpriteTextAspectY(int textId, float yPixels)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null)
        {
            Console.Error.WriteLine($"`size text y {textId}, {yPixels}` has no effect, because text has no font yet");
            return;
        }
        var size = runtimeFont.font.MeasureString(textSprite.text);
        var yRatio = yPixels / size.Y;
        textSprite.sprite.scale = new Vector2(yRatio, yRatio);
        TextSystem.textSprites[index] = textSprite;
    }


    /// <summary>
    /// <para>Attaches a text sprite to a transform for hierarchical positioning.</para>
    /// <para>The text sprite's position, rotation, and scale become relative to the transform.</para>
    /// </summary>
    /// <remarks>
    /// Once attached, the text sprite follows the transform as it moves, rotates, and scales.
    /// This is how you make text follow a game object. Create a transform with
    /// <see cref="CreateTransform">transform</see>, attach it to your entity, then attach the
    /// text sprite to that same transform. The text sprite's own position (set via
    /// <see cref="SetTextPosition">set text position</see>) becomes an offset relative to the
    /// transform rather than an absolute screen position. Works identically to how regular
    /// sprites attach to transforms.
    /// </remarks>
    /// <example>
    /// Make a health label follow a character transform.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// ` create a transform for the character
    /// tId = transform()
    /// position transform tId, 200, 150
    ///
    /// ` create the label and attach it to the transform
    /// text 1, 0, -20, 1, "100 HP"
    /// attach text to transform 1, tId
    ///
    /// ` now moving the transform moves the text too
    /// DO
    ///   position transform tId, 200 + rnd(4), 150
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID to attach.</param>
    /// <param name="transformId">The transform ID to attach to, created via <see cref="CreateTransform">transform</see>.</param>
    /// <seealso cref="CreateTransform">transform</seealso>
    /// <seealso cref="SetTextPosition">set text position</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="RotateSpriteText">rotate text</seealso>
    [FadeBasicCommand("attach text to transform")]
    public static void SetSpriteTextRelativeToAnother(int textId, int transformId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.sprite.anchorTransformId = transformId;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>Sets the rotation of a text sprite to a specific angle in radians.</para>
    /// <para>The text rotates around its origin point, which defaults to the top-left corner.</para>
    /// </summary>
    /// <remarks>
    /// The angle is in radians, not degrees. Use <see cref="Rad">rad</see> to convert from
    /// degrees if that's easier to think about. The rotation pivot is the text sprite's origin,
    /// which you can change with <see cref="SetSpriteTextOffset">set text offset</see>. For
    /// rotation around the center of the text, set the offset to <c>(0.5, 0.5)</c> first.
    /// This sets an absolute angle. It doesn't accumulate, so calling it with the same value
    /// twice has no additional effect.
    /// </remarks>
    /// <example>
    /// Spin text around its center.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 200, 150, 1, "Spinning!"
    /// ` set the origin to center so it rotates in place
    /// set text offset 1, 0.5, 0.5
    /// angle# = 0.0
    /// DO
    ///   angle# = angle# + 0.02
    ///   rotate text 1, angle#
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="angle">Rotation angle in radians. <c>0</c> = no rotation.</param>
    /// <seealso cref="Rad">rad</seealso>
    /// <seealso cref="SetSpriteTextOffset">set text offset</seealso>
    /// <seealso cref="SetSpriteTextRelativeToAnother">attach text to transform</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("rotate text")]
    public static void RotateSpriteText(int textId, float angle)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var text);
        text.sprite.rotation = angle;
        TextSystem.textSprites[index] = text;
    }

    /// <summary>
    /// <para>Sets the origin (pivot point) of a text sprite as a ratio of its measured size.</para>
    /// <para><c>(0, 0)</c> is the top-left corner, <c>(0.5, 0.5)</c> is the center, and <c>(1, 1)</c> is the bottom-right.</para>
    /// </summary>
    /// <remarks>
    /// The origin affects where the text sprite "anchors" to its position. By default it's
    /// <c>(0, 0)</c> (top-left), which means the position you set with
    /// <see cref="SetTextPosition">set text position</see> corresponds to the top-left corner
    /// of the text. Setting it to <c>(0.5, 0.5)</c> centers the text on that position, which
    /// is usually what you want for rotation (via <see cref="RotateSpriteText">rotate text</see>)
    /// or for centering text in a UI element. The origin also serves as the pivot for scaling.
    /// </remarks>
    /// <example>
    /// Center the text origin so it draws centered on its position.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 400, 300, 1, "Centered!"
    /// ` set origin to the center of the text
    /// set text offset 1, 0.5, 0.5
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <param name="xRatio">Horizontal origin as a ratio. <c>0</c> = left edge, <c>0.5</c> = center, <c>1</c> = right edge.</param>
    /// <param name="yRatio">Vertical origin as a ratio. <c>0</c> = top edge, <c>0.5</c> = center, <c>1</c> = bottom edge.</param>
    /// <seealso cref="SetTextPosition">set text position</seealso>
    /// <seealso cref="RotateSpriteText">rotate text</seealso>
    /// <seealso cref="SetTextScale">scale text</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("set text offset")]
    public static void SetSpriteTextOffset(int textId, float xRatio, float yRatio)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var text);
        text.sprite.origin = new Vector2(xRatio, yRatio);
        TextSystem.textSprites[index] = text;
    }

    /// <summary>
    /// <para>Returns the current X position of a text sprite.</para>
    /// <para>This is the raw position value, not accounting for transform attachment or origin offset.</para>
    /// </summary>
    /// <remarks>
    /// Returns the X component of the position last set by <see cref="Text">text</see> or
    /// <see cref="SetTextPosition">set text position</see>. If the text sprite is attached to
    /// a transform, this still returns the local position, not the final on-screen position.
    /// Use this together with <see cref="TextY">text y</see> to read back both coordinates.
    /// </remarks>
    /// <example>
    /// Read back the X position of a text sprite.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 150, 80, 1, "Hello"
    /// xPos = text x(1)
    /// print "Text X is: " + str(xPos)
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <returns>The X position in pixels.</returns>
    /// <seealso cref="TextY">text y</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SetTextPosition">set text position</seealso>
    [FadeBasicCommand("text x")]
    public static float TextX(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var sprite);
        return sprite.sprite.position.X;
    }

    /// <summary>
    /// <para>Returns the current Y position of a text sprite.</para>
    /// <para>This is the raw position value, not accounting for transform attachment or origin offset.</para>
    /// </summary>
    /// <remarks>
    /// Returns the Y component of the position last set by <see cref="Text">text</see> or
    /// <see cref="SetTextPosition">set text position</see>. If the text sprite is attached to
    /// a transform, this still returns the local position, not the final on-screen position.
    /// Use this together with <see cref="TextX">text x</see> to read back both coordinates.
    /// </remarks>
    /// <example>
    /// Read back the Y position of a text sprite.
    /// <code>
    /// font 1, "Fonts/Arial"
    /// text 1, 150, 80, 1, "Hello"
    /// yPos = text y(1)
    /// print "Text Y is: " + str(yPos)
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite ID.</param>
    /// <returns>The Y position in pixels.</returns>
    /// <seealso cref="TextX">text x</seealso>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="SetTextPosition">set text position</seealso>
    [FadeBasicCommand("text y")]
    public static float TextY(int textId)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var sprite);
        return sprite.sprite.position.Y;
    }
}
