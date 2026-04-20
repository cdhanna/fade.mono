using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

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
    /// ` load a font and display a greeting
    /// font 1, "Fonts/Arial"
    /// text 1, 100, 50, 1, "Hello World!"
    /// </code>
    /// </example>
    /// <example>
    /// Load multiple fonts for different UI elements:
    /// <code>
    /// ` load a heading font and a body font
    /// font 1, "Fonts/TitleFont"
    /// font 2, "Fonts/BodyFont"
    ///
    /// ` use the title font for the game name
    /// text 1, 200, 50, 1, "My Game"
    /// scale text 1, 2.0, 2.0
    ///
    /// ` use the body font for instructions
    /// text 2, 200, 120, 2, "Press space to start"
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
    /// ` check what texture ID would be assigned next
    /// nextId = free texture id(nextId)
    /// print nextId
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
    /// ` reserve a texture slot before setting up a render target
    /// texId = reserve texture id(texId)
    /// render target 1, 256, 256
    /// render target texture 1, texId
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
    /// ` load a player texture and create a sprite with it
    /// texture 1, "Images/Player"
    /// sprite 1, 100, 100, 1
    /// </code>
    /// </example>
    /// <example>
    /// Load a spritesheet texture and set up animation frames:
    /// <code>
    /// ` load a character spritesheet and split it into a 4x2 grid
    /// texture 1, "Images/CharacterSheet"
    /// set texture frame grid 1, 2, 4
    ///
    /// ` create a sprite and show frame 0
    /// sprite 1, 100, 100, 1
    /// set sprite frame 1, 0
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
    /// ` load a spritesheet and split it into frames
    /// texture 1, "Images/RunCycle"
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
    /// ` load a spritesheet and get the total frame count
    /// texture 1, "Images/Explosion"
    /// set texture frame grid 1, 4, 4
    /// totalFrames = texture frames(1)
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
    /// ` load a texture and size the sprite to match
    /// texture 1, "Images/Logo"
    /// sprite 1, 100, 100, 1
    /// w = texture width(1)
    /// h = texture height(1)
    /// size sprite 1, w, h
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
    /// ` load a texture and center the sprite vertically
    /// texture 1, "Images/Banner"
    /// sprite 1, 0, 0, 1
    /// h = texture height(1)
    /// screenH = screen height()
    /// yPos = (screenH - h) / 2
    /// position sprite 1, 0, yPos
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
        return tex.texture.Height;
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
    /// ` load a texture and scale the sprite proportionally
    /// texture 1, "Images/Portrait"
    /// sprite 1, 50, 50, 1
    ///
    /// ` set a target width and compute the matching height
    /// targetW = 200
    /// aspect = texture aspect(1)
    /// targetH = targetW * aspect
    /// size sprite 1, targetW, targetH
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
}
