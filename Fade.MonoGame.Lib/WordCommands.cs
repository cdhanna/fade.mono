using System;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    // what counts as a word: anything between spaces. a mark on its own, which French puts after a
    // space, stays with the word before it, so that a line never starts with one.
    static string[] SplitWords(string words)
    {
        var parts = (words ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kept = new System.Collections.Generic.List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (kept.Count > 0 && part.Length == 1 && "?!:;".IndexOf(part[0]) >= 0)
                kept[kept.Count - 1] += "\u00a0" + part;
            else
                kept.Add(part);
        }
        return kept.ToArray();
    }

    /// <summary>
    /// <para>Counts the words of a string, the way that <see cref="WordOf">word of$</see> numbers them.</para>
    /// </summary>
    /// <param name="words">The string.</param>
    /// <returns>How many words there are.</returns>
    [FadeBasicCommand("word count")]
    public static int WordCount(string words)
    {
        return SplitWords(words).Length;
    }

    /// <summary>
    /// <para>Gets one word of a string.</para>
    /// </summary>
    /// <param name="words">The string.</param>
    /// <param name="index">Which word, from 0.</param>
    /// <returns>The word, or " " past the end.</returns>
    [FadeBasicCommand("word of$")]
    public static string WordOf(string words, int index)
    {
        var parts = SplitWords(words);
        if (index < 0 || index >= parts.Length) return " ";
        return parts[index];
    }

    /// <summary>
    /// <para>Finds how many letters come before a word of a string. A picture in curly brackets, like {info}, is one letter, which is how a reveal counts it.</para>
    /// </summary>
    /// <param name="words">The string.</param>
    /// <param name="index">Which word, from 0.</param>
    /// <returns>How many letters come before it.</returns>
    [FadeBasicCommand("word start")]
    public static int WordStart(string words, int index)
    {
        var parts = SplitWords(words);
        var letters = 0;
        for (var i = 0; i < index && i < parts.Length; i++)
        {
            var part = parts[i];
            letters += part.StartsWith("{") ? 2 : part.Length + 1;
        }
        return letters;
    }
}
