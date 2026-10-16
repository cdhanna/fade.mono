namespace Fade.MonoGame.Content;

/// <summary>
/// The Vorbis comments that a release build writes into each sound it ships as .ogg instead of
/// .xnb (see $(FadeMonoEncodeAudioToOgg)). The content CLI writes them, and the game's
/// FadeContentManager reads them back to rebuild the exact SoundEffect XNB the pipeline made.
///
/// They exist because Vorbis does not reliably round-trip a length: NVorbis 0.10.4 decodes some
/// files a few hundred samples long at the end, and a looping sound then clicks at the seam. The
/// loader cuts (or pads) to <see cref="Samples"/>, so a sound is exactly as long as it was.
/// </summary>
public static class OggAudioTags
{
    /// <summary>Sample frames (per channel) in the original PCM.</summary>
    public const string Samples = "FADE_SAMPLES";

    /// <summary>The SoundEffect's loop start, in sample frames, as the XNB had it.</summary>
    public const string LoopStart = "FADE_LOOP_START";

    /// <summary>The SoundEffect's loop length, in sample frames, as the XNB had it.</summary>
    public const string LoopLength = "FADE_LOOP_LENGTH";

    /// <summary>The SoundEffect's duration in milliseconds, as the XNB had it.</summary>
    public const string DurationMs = "FADE_DURATION_MS";

    /// <summary>The XNB's target platform byte ('d' for DesktopGL), as a character.</summary>
    public const string XnbPlatform = "FADE_XNB_PLATFORM";

    /// <summary>
    /// Shipped at Content/sounds.manifest next to the .ogg files: one line per sound, its asset
    /// name and a hash of its .ogg, separated by a tab. The hash is what the decoded XNB is cached
    /// under, so an update that changes one sound misses the cache for that sound only.
    /// </summary>
    public const string ManifestFileName = "sounds.manifest";
}
