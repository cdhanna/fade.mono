namespace Fade.MonoGame.Content;

/// <summary>
/// A release built with $(FadeMonoCompressContent) ships each XNB Brotli-compressed, as
/// Content/&lt;name&gt;.xnb.br instead of Content/&lt;name&gt;.xnb. The content CLI writes them
/// (XnbCompressor), and the game's FadeContentManager inflates them as it opens them, so MonoGame
/// reads the very bytes the pipeline made.
///
/// Not MonoGame's own XNB compression (ContentBuilderParams.CompressContent, LZ4): XNBs built
/// with that did not load (see the note in Fade.MonoGame.Content's Builder.cs). This wraps the
/// whole file instead, so the XNB inside is an ordinary uncompressed one.
/// </summary>
public static class CompressedContent
{
    public const string Suffix = ".xnb.br";
}
