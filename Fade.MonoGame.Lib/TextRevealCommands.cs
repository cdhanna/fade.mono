using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Draws only the first few characters of a text, like a typewriter.</para>
    /// </summary>
    /// <remarks>
    /// The whole text is still laid out, so a word that is half shown already sits on the line
    /// that it ends up on, and nothing moves as more of it shows. An icon, like <c>{ghost}</c>,
    /// counts as one character. Only rich text, or text that wraps, can be revealed this way.
    /// A count below 0 shows all of it again.
    /// </remarks>
    /// <example>
    /// <code>
    /// text 1, 40, 40, 1, "hello there"
    /// enable rich text 1
    /// for n = 0 to text reveal length(1)
    ///     set text reveal 1, n
    ///     sync
    /// next
    /// </code>
    /// </example>
    /// <param name="textId">The text sprite.</param>
    /// <param name="count">How many characters to draw, or -1 for all of them.</param>
    /// <seealso cref="GetTextRevealLength">text reveal length</seealso>
    /// <seealso cref="GetTextRevealChar">text reveal char</seealso>
    [FadeBasicCommand("set text reveal")]
    public static void SetTextReveal(int textId, int count)
    {
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        textSprite.revealOn = count >= 0;
        textSprite.reveal = count < 0 ? 0 : count;
        TextSystem.textSprites[index] = textSprite;
    }

    /// <summary>
    /// <para>How many characters <see cref="SetTextReveal">set text reveal</see> counts in a text.</para>
    /// </summary>
    /// <remarks>
    /// This is the count of the text as it is laid out: an icon is one character, and the spaces
    /// that a line was broken at are not there.
    /// </remarks>
    /// <param name="textId">The text sprite.</param>
    /// <returns>The number of characters, or 0 when the text has no font yet.</returns>
    [FadeBasicCommand("text reveal length")]
    public static int GetTextRevealLength(int textId)
    {
        if (!TryGetRevealLayout(textId, out var layout)) return 0;
        var total = 0;
        foreach (var run in layout.runs)
        {
            total += run.isIcon ? 1 : (run.text?.Length ?? 0);
        }
        return total;
    }

    /// <summary>
    /// <para>The character code of one character of a text, counted the way that
    /// <see cref="SetTextReveal">set text reveal</see> counts them.</para>
    /// </summary>
    /// <remarks>
    /// Use it to react to what has just been revealed, like a sound for every vowel. An icon is 0.
    /// </remarks>
    /// <param name="textId">The text sprite.</param>
    /// <param name="position">Which character, counting from 0.</param>
    /// <returns>The character code, 0 for an icon, or -1 past the end.</returns>
    [FadeBasicCommand("text reveal char")]
    public static int GetTextRevealChar(int textId, int position)
    {
        if (position < 0) return -1;
        if (!TryGetRevealLayout(textId, out var layout)) return -1;
        var at = position;
        foreach (var run in layout.runs)
        {
            if (run.isIcon)
            {
                if (at == 0) return 0;
                at -= 1;
                continue;
            }
            var length = run.text?.Length ?? 0;
            if (at < length) return run.text[at];
            at -= length;
        }
        return -1;
    }

    static bool TryGetRevealLayout(int textId, out RichTextLayout layout)
    {
        layout = null;
        TextSystem.GetTextSpriteIndex(textId, out var index, out var textSprite);
        TextureSystem.GetSpriteFontIndex(textSprite.sprite.imageId, out _, out var runtimeFont);
        if (runtimeFont.font == null) return false;
        if (!RichTextSystem.UsesLayout(ref TextSystem.textSprites[index])) return false;
        layout = RichTextSystem.GetLayout(ref TextSystem.textSprites[index], ref runtimeFont);
        return layout != null;
    }
}
