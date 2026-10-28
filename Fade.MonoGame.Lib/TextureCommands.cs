using System;
using System.Collections.Generic;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Loads a font from the content pipeline and assigns it to the given ID.</para>
    /// <para>Call this during setup before you try to render any text. You cannot create
    /// a <see cref="Text">text</see> sprite without a loaded font.</para>
    /// </summary>
    /// <remarks>
    /// Fonts are the first thing you need if you want to draw any text on screen. Load
    /// one here, then pass its ID to <see cref="Text">text</see> when you create a text
    /// sprite. You only need to load a font once; after that, any number of text sprites
    /// can share the same font ID.
    ///
    /// The content path is relative to the Content directory and doesn't need a file
    /// extension. So if your font lives at <c>Content/Fonts/Arial</c>, just pass
    /// <c>"Fonts/Arial"</c>.
    /// </remarks>
    /// <example>
    /// Load a font and create a text sprite with it:
    /// <code>
    /// ` load the font before drawing any text
    /// font 1, "font"
    ///
    /// ` create a text sprite that uses the loaded font
    /// text 1, 550, 230, 1, "Hello World!"
    ///
    /// ` present the text every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Load multiple fonts for different UI elements:
    /// <code>
    /// ` load the same font into two ids for different ui elements
    /// font 1, "font"
    /// font 2, "font"
    ///
    /// ` use font id 1 for the game name and scale it up
    /// text 1, 650, 230, 1, "My Game"
    /// scale text 1, 2.0, 2.0
    ///
    /// ` use font id 2 for the instructions
    /// text 2, 650, 300, 2, "Press space to start"
    ///
    /// ` present both text sprites every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="fontId">The ID to assign to this font.</param>
    /// <param name="filePath">Content path to the font asset, relative to the Content directory (no extension needed).</param>
    /// <seealso cref="Text">text</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    [FadeBasicCommand("font")]
    public static void LoadSpriteFont(int fontId, string filePath)
    {
        TextureSystem.LoadSpriteFontFromContent(fontId, filePath);
    }

    /// <summary>
    /// <para>Makes a font out of a texture that holds a grid of hand-drawn characters.</para>
    /// <para>Each cell of the grid is one character, and a line of the font is as tall as a cell.</para>
    /// </summary>
    /// <remarks>
    /// This is for pixel fonts: letters drawn by hand in a paint program, or exported from a baked
    /// font with <see cref="SaveFontSheet">save font sheet</see> and then touched up. Draw text in
    /// the font at a scale of 1, or a whole multiple, and every pixel lands where it was drawn.
    ///
    /// The texture is cut into <paramref name="columns"/> by <paramref name="rows"/> equal cells.
    /// The characters run across each row from the top left, in character-code order, starting at
    /// <paramref name="firstCharacter"/> (the space, by default). So a sheet of 16 columns and 6
    /// rows holds the space through to <c>~</c>.
    ///
    /// A glyph is as wide as its ink, so letters do not need to be lined up sideways inside their
    /// cells, and narrow letters take less room than wide ones. Up and down they are drawn exactly
    /// where they sit in the cell, so draw them all on one baseline. A cell with no ink is a
    /// character the font does not have. Text that uses one draws <c>?</c> in its place.
    ///
    /// A pixel counts as ink when its alpha is at least half. Anything fainter is ignored and is
    /// never drawn, so a sheet can carry faint guides for whoever edits it. The ink keeps its
    /// color, and text color multiplies it, so white ink takes the color of the text.
    ///
    /// The font takes a copy of the pixels. To see a change to the picture, load the texture and
    /// run this command again. The texture must not be compressed.
    ///
    /// Everything that works on a font works on this one: <see cref="Text">text</see>,
    /// <see cref="SetFontIcon">font icon</see>, <see cref="SetFontEffect">set font effect</see>.
    /// </remarks>
    /// <example>
    /// Load a hand-drawn font and write with it.
    /// <code>
    /// ` "letters" is a picture of 16 columns and 6 rows of characters
    /// texture 1, "letters"
    /// font sheet 1, 1, 16, 6
    ///
    /// text 1, 100, 100, 1, "HELLO"
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="fontId">The ID to assign to this font.</param>
    /// <param name="textureId">A loaded texture that holds the grid of characters.</param>
    /// <param name="columns">How many cells there are across the texture.</param>
    /// <param name="rows">How many cells there are down the texture.</param>
    /// <param name="letterSpacing">The gap between one letter and the next, in pixels.</param>
    /// <param name="spaceWidth">The width of the space, in pixels. <c>0</c> picks a third of a cell.</param>
    /// <param name="firstCharacter">The character code of the top left cell.</param>
    /// <seealso cref="SaveFontSheet">save font sheet</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("font sheet")]
    public static void LoadFontSheet(int fontId, int textureId, int columns, int rows, int letterSpacing = 1,
        int spaceWidth = 0, int firstCharacter = 32)
    {
        FontSheetSystem.LoadFontFromSheet(fontId, textureId, columns, rows, letterSpacing, spaceWidth, firstCharacter);
    }

    /// <summary>
    /// <para>Saves a loaded font as a picture: a grid of its characters, with hard pixel edges.</para>
    /// <para>The picture is a starting point for a hand-drawn font, to be loaded back with <c>font sheet</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is a tool for making a game, not something a game does while it is played. Bake a font
    /// at the size it will be drawn at, save it as a sheet, fix the letters that came out badly in
    /// a paint program, and load the result with <see cref="LoadFontSheet">font sheet</see>.
    ///
    /// The sheet holds the characters from the space to <c>~</c>, 95 of them, so with 16 columns
    /// it has 6 rows. Each cell is as tall as a line of the font, and wide enough for the widest
    /// letter. The size of the cells is printed to the log.
    ///
    /// The smooth edges of the baked font are snapped: a pixel becomes solid white when the letter
    /// covers at least <paramref name="cover"/> of it, and stays empty otherwise. A lower cover
    /// gives bolder letters, and a higher one thinner letters.
    ///
    /// Every other cell is tinted faintly, so that the grid can be seen while editing. The tint
    /// is too faint to count as ink, and <c>font sheet</c> never draws it.
    /// </remarks>
    /// <example>
    /// Save a baked font as a sheet to edit.
    /// <code>
    /// font 1, "font"
    /// save font sheet 1, "font_sheet.png"
    /// end
    /// </code>
    /// </example>
    /// <param name="fontId">A loaded font.</param>
    /// <param name="filePath">The path to save the picture to. The <c>.png</c> extension is added if missing.</param>
    /// <param name="columns">How many cells to put in a row.</param>
    /// <param name="cover">How much of a pixel a letter has to cover for it to become ink, from <c>0</c> to <c>1</c>.</param>
    /// <seealso cref="LoadFontSheet">font sheet</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    [FadeBasicCommand("save font sheet")]
    public static void SaveFontSheet(int fontId, string filePath, int columns = 16, float cover = 0.5f)
    {
        FontSheetSystem.SaveFontSheet(fontId, filePath, columns, cover);
    }

    /// <summary>
    /// <para>Gets the next available texture ID without reserving it.</para>
    /// <para>The returned ID is not claimed, so another call could grab it before you
    /// use it. If you need a guaranteed slot, use
    /// <see cref="ReserveTextureNextId">reserve texture id</see> instead.</para>
    /// </summary>
    /// <remarks>
    /// This is handy when you want to peek at what ID is available next without actually
    /// committing to it. A common use is to check the next ID for bookkeeping or logging
    /// before deciding whether to load a texture.
    ///
    /// If you plan to actually load something into that slot, prefer
    /// <see cref="ReserveTextureNextId">reserve texture id</see>. It calls this
    /// internally and then initializes the slot so nothing else can steal the ID out
    /// from under you.
    /// </remarks>
    /// <example>
    /// Peek at the next available texture ID:
    /// <code>
    /// ` peek at the next free texture id without reserving it
    /// nextId = free texture id(nextId)
    ///
    /// ` load the ghost into that id and show it on screen
    /// texture nextId, "ghost"
    /// sprite 1, 320, 240, nextId
    ///
    /// ` keep drawing the sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">Receives the next free texture ID.</param>
    /// <returns>The next available texture ID. Not yet reserved, just a peek at what is next.</returns>
    /// <seealso cref="ReserveTextureNextId">reserve texture id</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    [FadeBasicCommand("free texture id")]
    public static int GetFreeTextureNextId(ref int textureId)
    {
        textureId = TextureSystem.highestTextureId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textureId;
    }

    /// <summary>
    /// <para>Reserves the next available texture ID and initializes its slot.</para>
    /// <para>Unlike <see cref="GetFreeTextureNextId">free texture id</see>, this
    /// actually claims the ID so it will not be handed out again.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need a texture slot ready before you fill it. For example,
    /// when you are about to set up a <see cref="SetRenderTargetTexture">render target texture</see>
    /// that writes into a texture, or any other workflow where you need the ID allocated
    /// ahead of time.
    ///
    /// Under the hood, this calls <see cref="GetFreeTextureNextId">free texture id</see>
    /// to find the next open slot and then immediately initializes it. After this call,
    /// the ID is yours and will not be reused by other texture commands.
    /// </remarks>
    /// <example>
    /// Reserve a texture ID for later use with a render target:
    /// <code>
    /// ` reserve a texture slot so nothing else can claim the id
    /// texId = reserve texture id(texId)
    ///
    /// ` create a render target and point it at the reserved texture
    /// rtId = reserve render target id(rtId)
    /// render target rtId, texId
    ///
    /// ` load the ghost so we have something visible to draw
    /// texture 2, "ghost"
    /// sprite 1, 320, 240, 2
    ///
    /// ` keep drawing the sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">Receives the reserved texture ID.</param>
    /// <returns>The newly reserved texture ID, ready to be used.</returns>
    /// <seealso cref="GetFreeTextureNextId">free texture id</seealso>
    /// <seealso cref="SetRenderTargetTexture">render target texture</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    [FadeBasicCommand("reserve texture id")]
    public static int ReserveTextureNextId(ref int textureId)
    {
        GetFreeTextureNextId(ref textureId);
        TextureSystem.GetTextureIndex(textureId, out _, out _);
        return textureId;
    }


    /// <summary>
    /// <para>Loads a texture from the content pipeline and assigns it to the given ID.</para>
    /// <para>This is the main way to get images into Fade. Once loaded, you can assign
    /// the texture to a <see cref="Sprite">sprite</see>, split it into frames, or query
    /// its dimensions.</para>
    /// </summary>
    /// <remarks>
    /// Textures are the raw image data that sprites display. You load one here, then
    /// reference it by ID when creating a <see cref="Sprite">sprite</see>. Multiple
    /// sprites can share the same texture, which is great for things like particle effects
    /// or tiled backgrounds.
    ///
    /// The content path is relative to the Content directory and doesn't need a file
    /// extension. If you want to use the texture as a spritesheet, load it first and then
    /// call <see cref="SetTextureFramesByRowCol">set texture frame grid</see> to carve
    /// it into frames.
    ///
    /// You can also query the loaded texture's size with
    /// <see cref="GetTextureWidth">texture width</see> and
    /// <see cref="GetTextureHeight">texture height</see>, which is useful for things
    /// like scaling sprites with <see cref="SizeSprite">size sprite</see>.
    /// </remarks>
    /// <example>
    /// Load a texture and display it as a sprite:
    /// <code>
    /// ` load the ghost image and create a sprite with it
    /// texture 1, "ghost"
    /// sprite 1, 100, 100, 1
    ///
    /// ` keep drawing the sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Load a spritesheet texture and set up animation frames:
    /// <code>
    /// ` load the ghost image and treat it as a 2x4 spritesheet
    /// texture 1, "ghost"
    /// set texture frame grid 1, 2, 4
    ///
    /// ` create a sprite and show frame 0
    /// sprite 1, 100, 100, 1
    /// set sprite frame 1, 0
    ///
    /// ` keep the frame on screen every tick
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID to assign to this texture. Must be unique; loading over an existing ID replaces it.</param>
    /// <param name="filePath">Content path to the texture asset, relative to the Content directory (no extension needed).</param>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="SetTextureFramesByRowCol">set texture frame grid</seealso>
    /// <seealso cref="SetSpriteFrame">set sprite frame</seealso>
    /// <seealso cref="GetTextureWidth">texture width</seealso>
    /// <seealso cref="GetTextureHeight">texture height</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    [FadeBasicCommand("texture")]
    public static void LoadTexture(int textureId, string filePath)
    {
        TextureSystem.LoadTextureFromContent(textureId, filePath);
    }

    /// <summary>
    /// <para>Loads a texture from an image file on disk while the game runs, instead of from the
    /// content pipeline.</para>
    /// <para>Returns <c>1</c> when the file was loaded, and <c>0</c> when there is no such file or
    /// it could not be read. Then the texture ID is left exactly as it was.</para>
    /// </summary>
    /// <remarks>
    /// This is for art that is not built into the game: pictures a player or an artist drops into
    /// a folder next to the game, to be seen without rebuilding it. The file can be a <c>.png</c>,
    /// <c>.jpg</c> or <c>.bmp</c>. A PNG keeps its transparency.
    ///
    /// A relative path is relative to the folder the executable is in, not to the working
    /// directory, so a folder shipped next to the game is found however it is started. Include
    /// the extension.
    ///
    /// Because a missing file changes nothing, the usual pattern is to load the built-in texture
    /// with <see cref="LoadTexture">texture</see> first, and then call this with the same ID to
    /// let a file replace it when there is one. The texture can be any size: sprites that are
    /// given a size with <see cref="SizeSprite">size sprite</see> are drawn at that size anyway.
    ///
    /// A file loaded this way is not watched for changes. Run the command again to pick up an
    /// edit. In the browser there are no files, and this always returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Let a file next to the game replace the built-in ghost:
    /// <code>
    /// texture 1, "ghost"
    /// replaced = texture file(1, "CustomArt/ghost.png")
    /// sprite 1, 100, 100, 1
    ///
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID to load the texture into. An existing texture there is replaced only if the file loads.</param>
    /// <param name="filePath">Path to the image file, with its extension. Relative paths start at the executable's folder.</param>
    /// <returns><c>1</c> if the file was loaded into the ID, <c>0</c> if not.</returns>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    [FadeBasicCommand("texture file")]
    public static int LoadTextureFile(int textureId, string filePath)
    {
        return TextureSystem.LoadTextureFromFile(textureId, filePath) ? 1 : 0;
    }

    /// <summary>
    /// <para>Splits a texture into a grid of frames for spritesheet animation.</para>
    /// <para>Each cell in the grid becomes a separate frame you can select with
    /// <see cref="SetSpriteFrame">set sprite frame</see>. Frames are numbered left-to-right,
    /// top-to-bottom, starting at <c>0</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is how you turn a single spritesheet image into an animation-ready texture.
    /// Say you have a character sheet that is 4 columns wide and 2 rows tall. Call this
    /// with rows <c>2</c> and columns <c>4</c>, and you will get 8 frames numbered <c>0</c>
    /// through <c>7</c>.
    ///
    /// The texture must already be loaded with <see cref="LoadTexture">texture</see> before
    /// you call this. The command divides the texture evenly, so make sure your spritesheet
    /// has uniform cell sizes. If the texture dimensions do not divide evenly by the row
    /// and column count, you will get frames that clip into neighboring cells.
    ///
    /// After setting up frames, use <see cref="SetSpriteFrame">set sprite frame</see> on
    /// any sprite using this texture to pick which frame to display. You can check how many
    /// frames a texture has with <see cref="GetTextureFrameCount">texture frames</see>.
    /// </remarks>
    /// <example>
    /// Set up a 4x2 spritesheet and animate it in a loop:
    /// <code>
    /// ` load the ghost image and split it into frames
    /// texture 1, "ghost"
    /// set texture frame grid 1, 2, 4
    ///
    /// ` create the sprite
    /// sprite 1, 100, 100, 1
    ///
    /// ` animate through frames in the game loop
    /// frame = 0
    /// totalFrames = texture frames(1)
    /// set sync rate 16
    /// DO
    ///   set sprite frame 1, frame
    ///   frame = frame + 1
    ///   IF frame &gt;= totalFrames THEN frame = 0
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to split. Must already be loaded with <see cref="LoadTexture">texture</see>.</param>
    /// <param name="rows">Number of rows in the grid. Must be at least <c>1</c>.</param>
    /// <param name="columns">Number of columns in the grid. Must be at least <c>1</c>.</param>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SetSpriteFrame">set sprite frame</seealso>
    /// <seealso cref="GetTextureFrameCount">texture frames</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("set texture frame grid")]
    public static void SetTextureFramesByRowCol(int textureId, int rows, int columns)
    {
        TextureSystem.GetTextureIndex(textureId, out var index, out var tex);
        // tex.descriptor.cols = columns;
        // tex.descriptor.rows = rows;
        var total = rows * columns;
        var width = tex.texture.Width;
        var height = tex.texture.Height;

        var cellWidth = width / columns;
        var cellHeight = height / rows;

        var frames = tex.descriptor.frames = new List<TextureFrame>(total);
        for (var y = 0; y < rows; y++)
        {
            var yOffset = y * cellHeight;
            for (var x = 0; x < columns; x++)
            {
                var xOffset = x * cellWidth;
                frames.Add(new TextureFrame
                {
                    xOffset = xOffset,
                    yOffset = yOffset,
                    xSize = cellWidth,
                    ySize = cellHeight
                });
            }
        }

        TextureSystem.textures[index] = tex;
        RichTextSystem.textureVersion++; // icons are sized from texture frames
    }

    /// <summary>
    /// <para>Reserve a texture's frame list so individual frames can be set with
    /// <see cref="SetTextureFrameRect">set texture frame rect</see>.</para>
    /// </summary>
    /// <remarks>
    /// <para>Pair with <c>set texture frame rect</c> to describe an atlas whose frames are NOT a
    /// uniform grid. That matters for a tightly packed sheet: trimming each frame to its own
    /// bounding box and packing the results is dramatically smaller than a grid, because a grid
    /// cell has to be big enough for the UNION of every frame's position, not the largest single
    /// frame. On a 160-frame character sheet that difference was 5.9 Mpx against 0.9 Mpx.</para>
    /// <para>Every frame starts as the whole texture; set them all before drawing.</para>
    /// </remarks>
    /// <param name="textureId">The texture to configure.</param>
    /// <param name="count">How many frames the sheet holds.</param>
    [FadeBasicCommand("set texture frame count")]
    public static void SetTextureFrameCount(int textureId, int count)
    {
        TextureSystem.GetTextureIndex(textureId, out var index, out var tex);
        if (count < 0) count = 0;
        var frames = tex.descriptor.frames = new List<TextureFrame>(count);
        for (var i = 0; i < count; i++)
        {
            frames.Add(new TextureFrame
            {
                index = i,
                xOffset = 0, yOffset = 0,
                xSize = tex.texture.Width, ySize = tex.texture.Height,
            });
        }
        TextureSystem.textures[index] = tex;
        RichTextSystem.textureVersion++; // icons are sized from texture frames
    }

    /// <summary>
    /// <para>Set one frame's source rectangle in pixels, for a non-uniform atlas.</para>
    /// </summary>
    /// <remarks>
    /// <para>Call <see cref="SetTextureFrameCount">set texture frame count</see> first. Frames not
    /// set keep the whole texture as their rect, which draws visibly wrong rather than silently,
    /// so a missing frame is easy to spot.</para>
    /// <para>Note that <c>size sprite</c> derives its scale from the CURRENT frame's rect, so on a
    /// sheet with varying frame sizes it goes stale the moment the frame changes. Use
    /// <c>scale sprite</c> instead, which sets the ratio directly and stays correct.</para>
    /// <para>The sprite's own offset is a RATIO of the frame, so a tightly packed sheet needs a
    /// per-frame offset too: trimming moves each frame's content relative to its box, and pinning
    /// every frame at one ratio makes the art jitter as it animates.</para>
    /// </remarks>
    /// <param name="textureId">The texture to configure.</param>
    /// <param name="frameIndex">Which frame, from 0.</param>
    /// <param name="x">Left edge in texture pixels.</param>
    /// <param name="y">Top edge in texture pixels.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    [FadeBasicCommand("set texture frame rect")]
    public static void SetTextureFrameRect(int textureId, int frameIndex,
                                           int x, int y, int width, int height)
    {
        TextureSystem.GetTextureIndex(textureId, out var index, out var tex);
        var frames = tex.descriptor.frames;
        if (frames == null || frameIndex < 0 || frameIndex >= frames.Count) return;
        frames[frameIndex] = new TextureFrame
        {
            index = frameIndex,
            xOffset = x, yOffset = y, xSize = width, ySize = height,
        };
        TextureSystem.textures[index] = tex;
        RichTextSystem.textureVersion++; // icons are sized from texture frames
    }

    /// <summary>
    /// <para>Returns the total number of frames in a texture's frame grid.</para>
    /// <para>Only meaningful after you have called
    /// <see cref="SetTextureFramesByRowCol">set texture frame grid</see> on the texture.</para>
    /// </summary>
    /// <remarks>
    /// This tells you how many frames are available for animation on a given texture.
    /// It is useful when you are cycling through frames and need to know when to wrap
    /// back to <c>0</c>. For example, you might set the sprite frame to
    /// <c>currentFrame mod textureFrames</c> each tick.
    ///
    /// If you have not called <see cref="SetTextureFramesByRowCol">set texture frame grid</see>
    /// on this texture yet, the frame count will not reflect a grid layout.
    /// </remarks>
    /// <example>
    /// Use the frame count to loop an animation:
    /// <code>
    /// ` load the ghost image and get the total frame count
    /// texture 1, "ghost"
    /// set texture frame grid 1, 4, 4
    /// totalFrames = texture frames(1)
    ///
    /// ` create the sprite that will play the animation
    /// sprite 1, 100, 100, 1
    ///
    /// ` cycle through all frames
    /// frame = 0
    /// set sync rate 16
    /// DO
    ///   set sprite frame 1, frame
    ///   frame = frame + 1
    ///   IF frame &gt;= totalFrames THEN frame = 0
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to check. Must already be loaded with <see cref="LoadTexture">texture</see>.</param>
    /// <returns>The number of frames in the texture's frame grid.</returns>
    /// <seealso cref="SetTextureFramesByRowCol">set texture frame grid</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SetSpriteFrame">set sprite frame</seealso>
    [FadeBasicCommand("texture frames")]
    public static int GetTextureFrameCount(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.descriptor.frames.Count;
    }

    /// <summary>
    /// <para>Returns the width of a texture in pixels.</para>
    /// </summary>
    /// <remarks>
    /// Handy when you need to know a texture's dimensions for layout or scaling. For
    /// example, you might use this alongside <see cref="GetTextureHeight">texture height</see>
    /// to size a <see cref="Sprite">sprite</see> to match its texture exactly, or to
    /// calculate a custom aspect ratio.
    ///
    /// You can also grab the pre-calculated ratio directly with
    /// <see cref="GetTextureAspect">texture aspect</see> if that is all you need.
    /// </remarks>
    /// <example>
    /// Size a sprite to match its texture dimensions:
    /// <code>
    /// ` load the ghost image and size the sprite to match
    /// texture 1, "ghost"
    /// sprite 1, 100, 100, 1
    /// w = texture width(1)
    /// h = texture height(1)
    /// size sprite 1, w, h
    ///
    /// ` keep drawing the sized sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to measure. Must already be loaded with <see cref="LoadTexture">texture</see>.</param>
    /// <returns>The width of the texture in pixels.</returns>
    /// <seealso cref="GetTextureHeight">texture height</seealso>
    /// <seealso cref="GetTextureAspect">texture aspect</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("texture width")]
    public static int GetTextureWidth(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        // a picture in an atlas is as big as its region, not as the whole page
        if (tex.atlasPageId != 0) return TextureSystem.GetSourceRect(ref tex, 0).Width;
        return tex.texture.Width;
    }

    /// <summary>
    /// <para>Returns the height of a texture in pixels.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to know a texture's vertical size for layout or scaling.
    /// Pair it with <see cref="GetTextureWidth">texture width</see> to get the full
    /// dimensions, or use <see cref="GetTextureAspect">texture aspect</see> if you
    /// just need the ratio.
    ///
    /// This is particularly useful when you want to scale a sprite proportionally.
    /// For instance, use <see cref="SizeSpriteAspectX">size sprite x</see> to set
    /// the width and let it calculate the height from the aspect ratio.
    /// </remarks>
    /// <example>
    /// Use texture height to center a sprite vertically on screen:
    /// <code>
    /// ` load the ghost image and center the sprite vertically
    /// texture 1, "ghost"
    /// sprite 1, 0, 0, 1
    /// h = texture height(1)
    /// screenH = screen height()
    /// yPos = (screenH - h) / 2
    /// position sprite 1, 0, yPos
    ///
    /// ` keep drawing the centered sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to measure. Must already be loaded with <see cref="LoadTexture">texture</see>.</param>
    /// <returns>The height of the texture in pixels.</returns>
    /// <seealso cref="GetTextureWidth">texture width</seealso>
    /// <seealso cref="GetTextureAspect">texture aspect</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("texture height")]
    public static int GetTextureHeight(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        if (tex.atlasPageId != 0) return TextureSystem.GetSourceRect(ref tex, 0).Height;
        return tex.texture.Height;
    }

    /// <summary>
    /// <para>Says that the picture the game loads from <paramref name="path"/> is a rectangle of an
    /// atlas page, and not a file of its own.</para>
    /// </summary>
    /// <remarks>
    /// Load the page with <see cref="LoadTexture">texture</see> first, then register every picture in
    /// it. From then on, loading one of those paths with <c>texture</c> gives a texture that draws only
    /// its rectangle of the page, at the rectangle's size, so the code that loads it by its old path
    /// does not change. <see cref="GetTextureWidth">texture width</see> and
    /// <see cref="GetTextureHeight">texture height</see> give the rectangle's size.
    ///
    /// The path is matched without regard to case or to which way the slashes go. Register before the
    /// pictures are loaded: a texture that was loaded already keeps its own file.
    ///
    /// An effect that works out its own texture coordinates, rather than using the ones the sprite
    /// gives it, sees the whole page. Keep pictures drawn with such an effect out of the atlas.
    /// </remarks>
    /// <example>
    /// <code>
    /// texture 50, "Atlas"
    /// atlas region 50, "Cards/bomb", 8, 30, 48, 48
    /// texture 1, "Cards/bomb"   ` this is the 48 x 48 rectangle of the atlas
    /// sprite 1, 100, 100, 1
    /// </code>
    /// </example>
    /// <param name="pageTextureId">The texture of the atlas page. It must be loaded already.</param>
    /// <param name="path">The path that the picture would be loaded from on its own.</param>
    /// <param name="x">The left of its rectangle on the page, in pixels.</param>
    /// <param name="y">The top of its rectangle on the page, in pixels.</param>
    /// <param name="width">The width of its rectangle, in pixels.</param>
    /// <param name="height">The height of its rectangle, in pixels.</param>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SetTextureFrameRect">set texture frame rect</seealso>
    [FadeBasicCommand("atlas region")]
    public static void AtlasRegion(int pageTextureId, string path, int x, int y, int width, int height)
    {
        TextureSystem.atlasRegions[TextureSystem.NormalizeAtlasPath(path)] = new TextureSystem.AtlasRegion
        {
            pageTextureId = pageTextureId,
            rect = new Microsoft.Xna.Framework.Rectangle(x, y, width, height),
        };
    }

    /// <summary>
    /// <para>Returns the aspect ratio of a texture, calculated as height divided by width.</para>
    /// <para>A value greater than <c>1.0</c> means the texture is taller than it is wide.
    /// Less than <c>1.0</c> means it is wider than it is tall.</para>
    /// </summary>
    /// <remarks>
    /// This saves you from doing the division yourself when you need to scale things
    /// proportionally. A common pattern is to set a sprite's width to some target size
    /// and then multiply by the aspect ratio to get the matching height, keeping the
    /// image from looking stretched.
    ///
    /// If you need the raw pixel dimensions instead, use
    /// <see cref="GetTextureWidth">texture width</see> and
    /// <see cref="GetTextureHeight">texture height</see>.
    /// </remarks>
    /// <example>
    /// Scale a sprite to a target width while preserving proportions:
    /// <code>
    /// ` load the ghost image and scale the sprite proportionally
    /// texture 1, "ghost"
    /// sprite 1, 50, 50, 1
    ///
    /// ` set a target width and compute the matching height
    /// targetW = 200
    /// aspect = texture aspect(1)
    /// targetH = targetW * aspect
    /// size sprite 1, targetW, targetH
    ///
    /// ` keep drawing the scaled sprite every frame
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to measure. Must already be loaded with <see cref="LoadTexture">texture</see>.</param>
    /// <returns>The height-to-width ratio as a decimal. For example, a 200x100 texture returns <c>2.0</c> and a 100x200 texture returns <c>0.5</c>.</returns>
    /// <seealso cref="GetTextureWidth">texture width</seealso>
    /// <seealso cref="GetTextureHeight">texture height</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="SizeSprite">size sprite</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    [FadeBasicCommand("texture aspect")]
    public static float GetTextureAspect(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        return tex.texture.Height / (float)tex.texture.Width;
    }

    public static bool TryGetSurfaceFormat(int index, out SurfaceFormat format)
    {
        format = SurfaceFormat.Color;
        switch (index)
        {
            case 0:
                return true;
            
            case 1:
                format = SurfaceFormat.HalfSingle;
                return true;
            
            case 2:
                format = SurfaceFormat.Single;
                return true;
            
            case 3:
                format = SurfaceFormat.Rg32;
                return true;
                        
            case 4:
                format = SurfaceFormat.HdrBlendable;
                return true;
            
            case 5:
                format = SurfaceFormat.HalfVector2;
                return true;
            
            case 6:
                format = SurfaceFormat.Vector2;
                return true;
            
            case 7:
                format = SurfaceFormat.HalfVector4;
                return true;
            
            case 8:
                format = SurfaceFormat.Vector4;
                return true;
            default:
                return false;
        }
    }

    [FadeBasicCommand("get surface format count")]
    public static int GetSurfaceFormatCount()
    {
        return 9;
    }
    
    [FadeBasicCommand("get surface format name$")]
    public static string GetSurfaceFormatName([FromVm]VirtualMachine _, int surfaceFormatIndex)
    {
        if (TryGetSurfaceFormat(surfaceFormatIndex, out var format))
        {
            return format.ToString();
        }

        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }
    
    
    public static bool TryGetDepthFormat(int index, out DepthFormat format)
    {
        format = DepthFormat.None;
        switch (index)
        {
            case 0:
                return true;
            
            case 1:
                format = DepthFormat.Depth16;
                return true;
            
            case 2:
                format = DepthFormat.Depth24;
                return true;
            
            case 3:
                format = DepthFormat.Depth24Stencil8;
                return true;
                    
            default:
                return false;
        }
    }

    [FadeBasicCommand("get depth format count")]
    public static int GetDepthFormatCount()
    {
        return 4;
    }
    
    [FadeBasicCommand("get depth format name$")]
    public static string GetDepthFormatName([FromVm]VirtualMachine _, int depthFormatIndex)
    {
        if (TryGetDepthFormat(depthFormatIndex, out var format))
        {
            return format.ToString();
        }

        throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
    }

    
    /// <summary>
    /// <para>Returns the surface format a texture ACTUALLY has, as a string.</para>
    /// </summary>
    /// <remarks>
    /// Worth checking rather than assuming, because the format asked for in
    /// <see cref="CreateRenderTarget">create texture target</see> is only a PREFERENCE. When a
    /// device cannot use it as a render target, the driver silently substitutes one it can --
    /// no error, no warning. A float target quietly downgraded to Color still works, it just
    /// bands, and hunting that from the symptom costs a lot more than printing this once at
    /// startup.
    ///
    /// The single- and two-channel float formats are the likeliest to be substituted.
    /// </remarks>
    /// <example>
    /// <code>
    /// create texture target 100, 1280, 720, 7  ` HalfVector4
    /// print "world target is " + texture format$(100)
    /// </code>
    /// </example>
    /// <param name="textureId">The ID of the texture to inspect.</param>
    /// <returns>The name of the texture's surface format, or an empty string if it has no texture yet.</returns>
    /// <seealso cref="GetTextureDepthFormat">texture depth format$</seealso>
    /// <seealso cref="CreateRenderTarget">create texture target</seealso>
    /// <seealso cref="GetSurfaceFormatName">get surface format name$</seealso>
    [FadeBasicCommand("texture format$")]
    public static string GetTextureFormat(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        if (tex.texture == null)
        {
            return "";
        }

        return tex.texture.Format.ToString();
    }

    /// <summary>
    /// <para>Returns the depth format a render target texture ACTUALLY has, as a string.</para>
    /// </summary>
    /// <remarks>
    /// Same caveat as <see cref="GetTextureFormat">texture format$</see>: the depth format is a
    /// preference too. Returns an empty string for a texture that is not a render target, which
    /// is also how to tell the two apart.
    ///
    /// Note that when several textures are bound to one output, only the FIRST one's depth
    /// buffer is used -- so a depth format on any of the others is inert.
    /// </remarks>
    /// <param name="textureId">The ID of the texture to inspect.</param>
    /// <returns>The name of the depth format, or an empty string if the texture is not a render target.</returns>
    /// <seealso cref="GetTextureFormat">texture format$</seealso>
    /// <seealso cref="CreateRenderTarget">create texture target</seealso>
    [FadeBasicCommand("texture depth format$")]
    public static string GetTextureDepthFormat(int textureId)
    {
        TextureSystem.GetTextureIndex(textureId, out _, out var tex);
        if (tex.texture is RenderTarget2D target)
        {
            return target.DepthStencilFormat.ToString();
        }

        return "";
    }

    [FadeBasicCommand("create texture target")]
    public static void CreateRenderTarget([FromVm]VirtualMachine vm, int textureId, int width, int height, int surfaceFormatIndex=0, int depthFormatIndex=0)
    {
        if (!TryGetSurfaceFormat(surfaceFormatIndex, out var surfaceFormat))
        {
            throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
        }
        if (!TryGetDepthFormat(depthFormatIndex, out var depthFormat))
        {
            throw new Exception("TODO: Change this to a VM exception, using the VirtualMachine arg");
        }
        
        TextureSystem.GetTextureIndex(textureId, out var index, out var tex);
        var target = TextureSystem.Own(new RenderTarget2D(GameSystem.graphicsDeviceManager.GraphicsDevice,
            width: width,
            height: height,
            mipMap: false,
            preferredFormat: surfaceFormat,
            preferredDepthFormat: depthFormat));

        tex.SetComputedTexture(target);

        TextureSystem.textures[index] = tex;

        // If this id is already bound to an output, that output is still holding the previous
        // target. Point it at the new one, or it would keep drawing into the old buffer while
        // every sampler read the new blank one -- silently, with nothing to blame.
        //
        // This makes it safe to call in either order relative to `render target`.
        RenderSystem.RebindOutputsToTarget(textureId, target);
    }
}
