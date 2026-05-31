namespace Fade.MonoGame.Content;


public struct ContentEntry
{
    public string path;
    public ContentProcessorType processr;
    public ContentImporterType importer;
    public Dictionary<string, string> parameters;
    public string name;
}

// Well-known keys the playground + desktop content builder agree on for
// parameters[]. Kept as constants (not strongly typed) so the dictionary
// stays a generic key/value bag — additional Fade-specific knobs can be
// added without changing the ContentEntry struct shape.
public static class ContentParameterKeys
{
    /// <summary>
    /// Texture compression: `auto`, `none`/`color`, `dxt1`, `dxt3`, `dxt5`,
    /// `alpha8`, `bgra4444`. Read by both the desktop MGCB builder (mapped
    /// to TextureProcessorOutputFormat) and the playground's PNG → XNB
    /// encoder.
    /// </summary>
    public const string Compression = "Compression";

    /// <summary>
    /// Sound compression for SoundEffect assets: `auto`, `pcm`, `adpcm`.
    /// Read by the playground's WAV/MP3/OGG → SoundEffect-XNB encoder.
    /// The desktop builder doesn't honour this yet — MGCB's
    /// SoundEffectProcessor.Quality is what controls codec there.
    /// </summary>
    public const string SoundCompression = "SoundCompression";

    /// <summary>
    /// Render size in pixels for a TTF/OTF font source. Read by the
    /// playground's font rasterizer to decide how many pixels tall to
    /// rasterize glyphs at. Larger values give crisper text at large
    /// display sizes but bigger atlases; smaller values save memory but
    /// look blurry when scaled up.
    /// </summary>
    public const string FontSize = "FontSize";
}

public enum ContentProcessorType
{
    Auto,
    Texture,
    Effect,
    SoundEffect,
    SpriteFont,
}

public enum ContentImporterType
{
    Auto,
    Texture,
    Effect,
    Mp3,
    SpriteFont
}

// Texture compression options recognised by the macro surface and the
// Playground's PNG → XNB encoder. Auto resolves to a per-image decision
// at compile time (opaque → Dxt1, alpha → Dxt5). None / Color keep the
// source RGBA in the XNB uncompressed (4 bytes/pixel) — a safe default
// for sprites where the editor wants instant feedback. Alpha8 stores a
// single alpha channel (1 byte/pixel) for fonts, masks, SDFs. Bgra4444
// stores 12-bit colour + 4-bit alpha (2 bytes/pixel) — useful for retro
// looks and small UI atlases where Color is overkill but DXT artifacts
// are undesirable on sharp edges.
public enum TextureCompression
{
    Auto,
    None,
    Color,
    Dxt1,
    Dxt3,
    Dxt5,
    Alpha8,
    Bgra4444,
}

// Sound compression options recognised by the macro surface and the
// Playground's WAV/MP3/OGG → SoundEffect-XNB encoder. Auto resolves to
// Pcm today (uncompressed PCM 16-bit); Adpcm is reserved for the
// future MS-ADPCM (wFormatTag=2) encoder.
public enum SoundCompression
{
    Auto,
    Pcm,
    Adpcm,
}
