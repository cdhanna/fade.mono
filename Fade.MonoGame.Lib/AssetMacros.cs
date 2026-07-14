using System.Collections.Generic;
using Fade.MonoGame.Content;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Pushes an asset file into the content build pipeline.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Use this inside a macro block (lines prefixed with <c>#</c>) to tell the content
    /// pipeline about an asset your game needs. The pipeline will process and pack it so
    /// it is available at runtime through commands like
    /// <see cref="LoadTexture">texture</see>, <see cref="LoadSpriteFont">font</see>, or
    /// <see cref="LoadSoundEffect">load sfx clip</see>.
    ///
    /// After pushing, you can rename the asset with
    /// <see cref="RenameCurrent">rename asset</see> if the original filename is unwieldy.
    /// The push/rename pair is the most common macro pattern for setting up content.
    /// </remarks>
    /// <example>
    /// Push a texture asset so it is available at runtime:
    /// <code>
    /// ` push an image into the content pipeline and give it a clean name
    /// # push asset "Assets/Images/ghost-sprite-v2.png"
    /// # rename asset "ghost"
    ///
    /// ` at runtime, load the pushed texture by its renamed name
    /// texture 1, "ghost"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   ` draw the sprite every frame so it stays on screen
    ///   sprite 1, 100, 100, 1
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Push a font asset for text rendering:
    /// <code>
    /// ` push a font into the content pipeline and give it a clean name
    /// # push asset "Assets/Fonts/MyFont.ttf"
    /// # rename asset "font"
    ///
    /// ` at runtime, load the pushed font and draw text every frame
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   ` text takes: textId, x, y, fontId, string
    ///   text 1, 550, 230, 1, "HELLO!"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="path">The file path of the asset to add to the content build.</param>
    /// <seealso cref="RenameCurrent">rename asset</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("push asset", FadeBasicCommandUsage.Macro)]
    public static void Push(string path)
    {
        ContentSystem.Push(path);
    }

    /// <summary>
    /// <para>Renames the most recently pushed asset in the content build pipeline.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.
    /// It operates on whatever <see cref="Push">push asset</see> last added.</para>
    /// </summary>
    /// <remarks>
    /// Call this right after <see cref="Push">push asset</see> when the original filename
    /// is too long, includes version numbers, or does not match the name you want to use in
    /// your runtime code. The new name becomes the content path you pass to loading
    /// commands like <see cref="LoadTexture">texture</see> or
    /// <see cref="LoadSoundEffect">load sfx clip</see>.
    /// </remarks>
    /// <example>
    /// Rename a pushed asset to a shorter, cleaner path:
    /// <code>
    /// ` push an audio file with a long filename and give it a short name
    /// # push asset "Assets/Audio/coin-pickup-2-293341.wav"
    /// # rename asset "coin"
    ///
    /// ` at runtime, load using the short name and play it once
    /// load sfx clip 1, "coin"
    /// play sfx 1
    ///
    /// ` load a font so we can show something while the sound plays
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   text 1, 550, 280, 1, "COIN SOUND PLAYED"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Rename multiple assets in sequence:
    /// <code>
    /// ` push and rename several assets, one rename per push
    /// # push asset "Assets/Images/ghost-sprite-final-v3.png"
    /// # rename asset "ghost"
    /// # push asset "Assets/Fonts/pixel-font-regular.ttf"
    /// # rename asset "font"
    ///
    /// ` at runtime, load them by their clean names and draw each frame
    /// texture 1, "ghost"
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   sprite 1, 100, 100, 1
    ///   text 1, 550, 240, 1, "READY"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="name">The new content name for the asset.</param>
    /// <seealso cref="Push">push asset</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    [FadeBasicCommand("rename asset", FadeBasicCommandUsage.Macro)]
    public static void RenameCurrent(string name)
    {
        ContentSystem.GetCurrent().name = name;
    }

    /// <summary>
    /// <para>Sets the texture compression format for a specific asset.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Use this inside a macro block to override the compression a single texture
    /// is built with. Compression trades disk + VRAM for image quality and encode
    /// time; <c>auto</c> picks <c>dxt1</c> for opaque images and <c>dxt5</c> for
    /// images with alpha, which is the right answer most of the time.
    ///
    /// The Playground's browser content builder honours this setting when it
    /// compiles uploaded PNG/JPG sources to XNB. The desktop content builder
    /// applies the equivalent <c>TextureProcessorOutputFormat</c> on the MGCB
    /// processor.
    ///
    /// Valid format strings (case-insensitive): <c>auto</c>, <c>none</c>,
    /// <c>color</c>, <c>dxt1</c>, <c>dxt3</c>, <c>dxt5</c>. <c>none</c> and
    /// <c>color</c> are synonyms — both leave the texture uncompressed
    /// (BGRA8888, 4 bytes per pixel).
    /// </remarks>
    /// <example>
    /// Compile a UI sprite uncompressed for crisp pixels:
    /// <code>
    /// ` push a texture and compile it uncompressed for crisp pixels
    /// # push asset "Assets/Images/ghost.png"
    /// # rename asset "ghost"
    /// # texture compression "ghost", "color"
    ///
    /// ` at runtime, load and draw the texture each frame
    /// texture 1, "ghost"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   sprite 1, 100, 100, 1
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Force DXT5 on a large textured background:
    /// <code>
    /// ` push a texture and force the DXT5 (alpha) compression format
    /// # push asset "Assets/Images/ghost.png"
    /// # rename asset "ghost"
    /// # texture compression "ghost", "dxt5"
    ///
    /// ` at runtime, load and draw the texture each frame
    /// texture 1, "ghost"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   sprite 1, 100, 100, 1
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="assetName">The asset name (the same string you pass to <c>texture</c> at runtime).</param>
    /// <param name="format">The compression format: <c>auto</c>, <c>none</c>/<c>color</c>, <c>dxt1</c>, <c>dxt3</c>, or <c>dxt5</c>.</param>
    /// <seealso cref="DefaultTextureCompression">default texture compression</seealso>
    /// <seealso cref="Push">push asset</seealso>
    [FadeBasicCommand("texture compression", FadeBasicCommandUsage.Macro)]
    public static void SetTextureCompression(string assetName, string format)
    {
        if (!TryParseCompression(format, out var c)) return;
        var fmtStr = ContentSystem.CompressionToString(c);
        var found = false;
        for (var i = 0; i < ContentSystem.entries.ptr; i++)
        {
            ref var e = ref ContentSystem.entries.buffer[i];
            // Match either the original push path or the renamed name —
            // users often rename right after pushing and this macro should
            // accept either handle.
            if (e.name == assetName || e.path == assetName)
            {
                if (e.parameters == null) e.parameters = new Dictionary<string, string>();
                e.parameters[ContentParameterKeys.Compression] = fmtStr;
                found = true;
            }
        }
        if (!found)
        {
            // Playground use case: user uploaded a PNG and references it
            // by name without calling `push asset` first. Auto-create an
            // entry so the compression setting reaches the playground's
            // compile pass via the plan. The desktop content builder
            // never sees this code path because desktop projects always
            // declare assets via `push asset` first.
            ref var e = ref ContentSystem.Push(assetName);
            if (e.parameters == null) e.parameters = new Dictionary<string, string>();
            e.parameters[ContentParameterKeys.Compression] = fmtStr;
        }
    }

    /// <summary>
    /// <para>Sets the default texture compression for every subsequent <c>push asset</c>.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Sets the compression baseline for the rest of the macro block. Individual
    /// textures can still opt out with <see cref="SetTextureCompression">texture compression</see>.
    /// The default if you never call this is <c>auto</c>.
    ///
    /// Valid format strings (case-insensitive): <c>auto</c>, <c>none</c>,
    /// <c>color</c>, <c>dxt1</c>, <c>dxt3</c>, <c>dxt5</c>.
    /// </remarks>
    /// <example>
    /// Compile every texture uncompressed (typical playground iteration):
    /// <code>
    /// ` every push after this line inherits the "color" (uncompressed) format
    /// # default texture compression "color"
    /// # push asset "Assets/Images/ghost.png"
    /// # rename asset "ghost"
    ///
    /// ` at runtime, load and draw the texture each frame
    /// texture 1, "ghost"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   sprite 1, 100, 100, 1
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="format">The compression format: <c>auto</c>, <c>none</c>/<c>color</c>, <c>dxt1</c>, <c>dxt3</c>, or <c>dxt5</c>.</param>
    /// <seealso cref="SetTextureCompression">texture compression</seealso>
    [FadeBasicCommand("default texture compression", FadeBasicCommandUsage.Macro)]
    public static void DefaultTextureCompression(string format)
    {
        if (!TryParseCompression(format, out var c)) return;
        ContentSystem.defaultTextureCompression = c;
    }

    // Lenient parser — anything we don't recognise leaves the setting at
    // its prior value. The Playground surfaces unknown formats as warnings
    // through its diagnostics pipeline; the desktop builder ignores them.
    private static bool TryParseCompression(string format, out TextureCompression value)
    {
        switch ((format ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "auto":     value = TextureCompression.Auto;     return true;
            case "none":     value = TextureCompression.None;     return true;
            case "color":    value = TextureCompression.Color;    return true;
            case "dxt1":     value = TextureCompression.Dxt1;     return true;
            case "dxt3":     value = TextureCompression.Dxt3;     return true;
            case "dxt5":     value = TextureCompression.Dxt5;     return true;
            case "alpha8":   value = TextureCompression.Alpha8;   return true;
            case "bgra4444": value = TextureCompression.Bgra4444; return true;
            default: value = TextureCompression.Auto; return false;
        }
    }

    /// <summary>
    /// <para>Sets the audio compression format for a specific sound asset.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Use this inside a macro block to override the compression a single
    /// sound is built with. <c>pcm</c> stores uncompressed 16-bit PCM
    /// samples (universal, instant decode, ~176 KB per second of stereo
    /// 44.1 kHz audio). <c>adpcm</c> reserves the MS-ADPCM slot — about
    /// 4:1 size at slightly reduced fidelity — for when the encoder
    /// ships; until then the playground transparently falls back to PCM.
    ///
    /// Source files can be any format the browser's Web Audio API
    /// decodes: WAV, MP3, OGG (Chrome/Firefox), FLAC, AAC/M4A. The
    /// encoder always emits a SoundEffect XNB with a PCM payload at the
    /// source's native sample rate.
    /// </remarks>
    /// <example>
    /// Compile a SFX with the default uncompressed PCM path:
    /// <code>
    /// ` push a sound and compile it as uncompressed 16-bit PCM
    /// # push asset "Assets/Audio/coin.wav"
    /// # rename asset "coin"
    /// # sound compression "coin", "pcm"
    ///
    /// ` at runtime, load and play the sound, then keep the frame drawing
    /// load sfx clip 1, "coin"
    /// play sfx 1
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   text 1, 550, 280, 1, "PCM SOUND PLAYED"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="assetName">The asset name (the same string you pass to <c>load sfx clip</c> at runtime).</param>
    /// <param name="format">The compression format: <c>auto</c>, <c>pcm</c>, or <c>adpcm</c>.</param>
    /// <seealso cref="DefaultSoundCompression">default sound compression</seealso>
    /// <seealso cref="Push">push asset</seealso>
    [FadeBasicCommand("sound compression", FadeBasicCommandUsage.Macro)]
    public static void SetSoundCompression(string assetName, string format)
    {
        if (!TryParseSoundCompression(format, out var c)) return;
        var fmtStr = ContentSystem.SoundCompressionToString(c);
        var found = false;
        for (var i = 0; i < ContentSystem.entries.ptr; i++)
        {
            ref var e = ref ContentSystem.entries.buffer[i];
            if (e.name == assetName || e.path == assetName)
            {
                if (e.parameters == null) e.parameters = new Dictionary<string, string>();
                e.parameters[ContentParameterKeys.SoundCompression] = fmtStr;
                found = true;
            }
        }
        if (!found)
        {
            // Same auto-create fallback as `texture compression` — the
            // playground doesn't require `push asset` for OPFS-uploaded
            // audio, so create a virtual entry so the plan carries the
            // setting through to the encoder.
            ref var e = ref ContentSystem.Push(assetName);
            if (e.parameters == null) e.parameters = new Dictionary<string, string>();
            e.parameters[ContentParameterKeys.SoundCompression] = fmtStr;
        }
    }

    /// <summary>
    /// <para>Sets the default audio compression for every subsequent <c>push asset</c>.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <example>
    /// Compile every sound as uncompressed PCM (the default if you
    /// don't call this at all):
    /// <code>
    /// ` every push after this line inherits the "pcm" (uncompressed) format
    /// # default sound compression "pcm"
    /// # push asset "Assets/Audio/coin.wav"
    /// # rename asset "coin"
    /// # push asset "Assets/Audio/boom.wav"
    /// # rename asset "explosion"
    ///
    /// ` at runtime, load both sounds and play one, then keep drawing
    /// load sfx clip 1, "coin"
    /// load sfx clip 2, "explosion"
    /// play sfx 1
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   text 1, 550, 280, 1, "SOUNDS LOADED"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="format">The compression format: <c>auto</c>, <c>pcm</c>, or <c>adpcm</c>.</param>
    /// <seealso cref="SetSoundCompression">sound compression</seealso>
    [FadeBasicCommand("default sound compression", FadeBasicCommandUsage.Macro)]
    public static void DefaultSoundCompression(string format)
    {
        if (!TryParseSoundCompression(format, out var c)) return;
        ContentSystem.defaultSoundCompression = c;
    }

    private static bool TryParseSoundCompression(string format, out SoundCompression value)
    {
        switch ((format ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "auto":  value = SoundCompression.Auto;  return true;
            case "pcm":   value = SoundCompression.Pcm;   return true;
            case "adpcm": value = SoundCompression.Adpcm; return true;
            default: value = SoundCompression.Auto; return false;
        }
    }

    /// <summary>
    /// <para>Sets the rasterization size (in pixels) for a TTF/OTF font asset.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Use this to pick the pixel size the playground rasterizes glyphs at
    /// when compiling a `.ttf`/`.otf` font into a SpriteFont. Larger sizes
    /// give crisper text at large display sizes but bigger atlas textures;
    /// smaller sizes save memory but look blurry when scaled up at runtime.
    ///
    /// Default if you never call this is 32 pixels. The runtime `scale text`
    /// command works on top of whatever size you choose here.
    /// </remarks>
    /// <example>
    /// Render a font at 48px for use as a UI title:
    /// <code>
    /// ` push a font and rasterize its glyphs at 48 pixels
    /// # push asset "Assets/Fonts/heading.ttf"
    /// # rename asset "font"
    /// # font size "font", 48
    ///
    /// ` at runtime, load the font and draw a title each frame
    /// font 1, "font"
    /// set sync rate 16
    /// do
    ///   set background color rgb(20, 20, 40)
    ///   text 1, 550, 240, 1, "TITLE"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="assetName">The asset name (the same string you pass to <c>font</c> at runtime).</param>
    /// <param name="sizePx">Render size in pixels. Typical values: 16, 24, 32, 48, 64.</param>
    [FadeBasicCommand("font size", FadeBasicCommandUsage.Macro)]
    public static void SetFontSize(string assetName, int sizePx)
    {
        if (sizePx < 4) sizePx = 4;
        var sizeStr = sizePx.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var found = false;
        for (var i = 0; i < ContentSystem.entries.ptr; i++)
        {
            ref var e = ref ContentSystem.entries.buffer[i];
            if (e.name == assetName || e.path == assetName)
            {
                if (e.parameters == null) e.parameters = new Dictionary<string, string>();
                e.parameters[ContentParameterKeys.FontSize] = sizeStr;
                found = true;
            }
        }
        if (!found)
        {
            // Auto-create entry — same pattern as `texture compression`
            // for the playground's no-push-asset workflow.
            ref var e = ref ContentSystem.Push(assetName);
            if (e.parameters == null) e.parameters = new Dictionary<string, string>();
            e.parameters[ContentParameterKeys.FontSize] = sizeStr;
        }
    }

    public static void Set()
    {
        // # push asset Fish/Audio/bubble-pop-2-293341.mp3
        // # rename asset Fish/Audio/bubble-pop-2.mp3
        // # set asset importer "Mp3Importer"
        // # set asset processor "SoundEffectProcessor"
        // # set asset param "Quality" "Best"
        
        
        
        // # rename asset Fish/Audio/bubble-pop-2-293341.mp3 Fish/Audio/bubble-pop-2.mp3 
        // # asset param Fish/Audio/bubble-pop-2-293341.mp3 
        
        // set importer 
        // set processor, 
        // set parameter
        // set output name
    }

}