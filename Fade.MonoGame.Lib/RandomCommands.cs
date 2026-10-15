using System;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>How many random streams there are. Their ids run from <c>0</c> to one less than this.</summary>
    public const int RandomStreamCount = 16;

    // One number of state for every stream. A stream that was never seeded starts somewhere
    // different every time the game runs, the way that rnd does.
    static readonly ulong[] RandomStreams = NewRandomStreams();

    static ulong[] NewRandomStreams()
    {
        var streams = new ulong[RandomStreamCount];
        for (var i = 0; i < streams.Length; i++) streams[i] = (ulong)Random.Shared.NextInt64();
        return streams;
    }

    // The mixing step of splitmix64. Numbers that are close together come out far apart.
    static ulong MixRandom(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    static uint NextRandom(int id)
    {
        RandomStreams[id] += 0x9E3779B97F4A7C15UL;
        return (uint)(MixRandom(RandomStreams[id]) >> 32);
    }

    static bool IsRandomStream(int id, string doing)
    {
        if (id >= 0 && id < RandomStreamCount) return true;
        Console.WriteLine($"[random] cannot {doing} random stream {id}. the streams are 0 to {RandomStreamCount - 1}.");
        return false;
    }

    /// <summary>
    /// <para>Starts a random stream over from a seed.</para>
    /// <para>The same seed always gives the same numbers, in the same order, on every machine.</para>
    /// </summary>
    /// <remarks>
    /// A random stream is a source of random numbers that is all your own. <c>rnd</c> hands out
    /// numbers from one shared source, so every bubble and every wobble of a sound takes a number
    /// from it, and how many of those there are depends on how long the player waited. Two games
    /// that start from the same place drift apart at once.
    ///
    /// A stream only moves on when you ask it for a number with
    /// <see cref="RandomStream">random stream</see>. Seed one when a game starts, draw only the
    /// rolls that matter to the rules from it, and leave everything else on <c>rnd</c>. Then
    /// two games with the same seed deal the same fish, cards, or dice, whatever is drawn on top.
    ///
    /// There are <c>16</c> streams, with ids from <c>0</c> to <c>15</c>, and they do not affect
    /// each other. Give each kind of roll its own stream, and one kind stays the same even when
    /// the player's choices change how many times another is rolled.
    ///
    /// Seeds that are close together, like <c>41</c> and <c>42</c>, give streams that have
    /// nothing to do with each other. A stream that is never seeded starts somewhere different
    /// every time the game runs.
    /// </remarks>
    /// <example>
    /// Deal the same five dice every time the game runs:
    /// <code>
    /// #constant STREAM_DICE 0
    /// seed random stream STREAM_DICE, 1234
    ///
    /// font 1, "font"
    /// FOR i = 1 TO 5
    ///   text i, 100, 100 + i * 24, 1, "die: " + str$(random stream(STREAM_DICE, 6) + 1)
    /// NEXT
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Which stream to seed, from <c>0</c> to <c>15</c>.</param>
    /// <param name="seed">Any whole number. The same seed gives the same numbers.</param>
    /// <seealso cref="RandomStream">random stream</seealso>
    /// <seealso cref="RandomStreamState">random stream state</seealso>
    [FadeBasicCommand("seed random stream")]
    public static void SeedRandomStream(int id, int seed)
    {
        if (!IsRandomStream(id, "seed")) return;

        // the id is mixed in, so that two streams with the same seed are still not the same
        RandomStreams[id] = MixRandom(((ulong)(uint)id << 32) | (uint)seed);
    }

    /// <summary>
    /// <para>Returns the next random whole number from a stream, from <c>0</c> up to one less than <c>max</c>.</para>
    /// <para>It works like <c>rnd</c>, but only this command moves the stream on.</para>
    /// </summary>
    /// <remarks>
    /// Use this for the rolls that decide what happens in the game, on a stream that was seeded
    /// with <see cref="SeedRandomStream">seed random stream</see>. Every call takes the next
    /// number of that stream, so the order of the calls matters: the same calls in the same
    /// order give the same numbers.
    ///
    /// Every number from <c>0</c> to <c>max - 1</c> is as likely as any other. A <c>max</c> of
    /// <c>1</c> or less always gives <c>0</c>, and still moves the stream on, so that a roll
    /// takes one number no matter what it is out of.
    /// </remarks>
    /// <example>
    /// Roll a six sided die from a seeded stream:
    /// <code>
    /// seed random stream 0, 99
    /// roll = random stream(0, 6) + 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "rolled: " + str$(roll)
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Which stream to draw from, from <c>0</c> to <c>15</c>.</param>
    /// <param name="max">How many different numbers can come up. The biggest is <c>max - 1</c>.</param>
    /// <returns>A whole number from <c>0</c> to <c>max - 1</c>.</returns>
    /// <seealso cref="SeedRandomStream">seed random stream</seealso>
    [FadeBasicCommand("random stream")]
    public static int RandomStream(int id, int max)
    {
        if (!IsRandomStream(id, "draw from")) return 0;

        var bound = max < 1 ? 1u : (uint)max;

        // Lemire's way of fitting a number into a range without favoring any of it. Once in a
        // great while a number has to be thrown away and the next one used.
        var product = (ulong)NextRandom(id) * bound;
        var low = (uint)product;
        if (low < bound)
        {
            var threshold = unchecked(0u - bound) % bound;
            while (low < threshold)
            {
                product = (ulong)NextRandom(id) * bound;
                low = (uint)product;
            }
        }
        return (int)(product >> 32);
    }

    /// <summary>
    /// <para>Returns one half of where a random stream has got to, so that it can be saved.</para>
    /// <para>Put both halves back with <see cref="SetRandomStreamState">set random stream state</see> and the stream carries on from the same place.</para>
    /// </summary>
    /// <remarks>
    /// A game that can be saved in the middle needs this. If only the seed were saved, a game
    /// that was loaded would start its stream over, and deal the fish of the first move again.
    ///
    /// Where a stream has got to is too big for one whole number, so it comes in two halves:
    /// ask for half <c>0</c> and half <c>1</c>, and save both.
    /// </remarks>
    /// <example>
    /// Save a stream into the preferences, and carry on from there the next time:
    /// <code>
    /// IF pref exists(10)
    ///   set random stream state 0, get pref(10), get pref(11)
    /// ELSE
    ///   seed random stream 0, 1234
    /// ENDIF
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "next: " + str$(random stream(0, 100))
    ///
    /// set pref 10, random stream state(0, 0)
    /// set pref 11, random stream state(0, 1)
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Which stream, from <c>0</c> to <c>15</c>.</param>
    /// <param name="half">Which half to return: <c>0</c> or <c>1</c>.</param>
    /// <returns>That half, as a whole number. It can be negative.</returns>
    /// <seealso cref="SetRandomStreamState">set random stream state</seealso>
    [FadeBasicCommand("random stream state")]
    public static int RandomStreamState(int id, int half)
    {
        if (!IsRandomStream(id, "read the state of")) return 0;
        return half == 0 ? (int)(uint)RandomStreams[id] : (int)(uint)(RandomStreams[id] >> 32);
    }

    /// <summary>
    /// <para>Puts a random stream back to where it had got to.</para>
    /// <para>The two halves are the ones that <see cref="RandomStreamState">random stream state</see> returned.</para>
    /// </summary>
    /// <remarks>
    /// Call this when a saved game is loaded, with the two halves that were saved. The stream
    /// then gives the same numbers that it would have given if the game had never been closed.
    /// </remarks>
    /// <example>
    /// Roll the same number twice by putting the stream back:
    /// <code>
    /// seed random stream 0, 7
    /// half0 = random stream state(0, 0)
    /// half1 = random stream state(0, 1)
    ///
    /// first = random stream(0, 100)
    /// set random stream state 0, half0, half1
    /// again = random stream(0, 100)
    ///
    /// ` both numbers are the same
    /// font 1, "font"
    /// text 1, 470, 200, 1, str$(first) + " and " + str$(again)
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Which stream, from <c>0</c> to <c>15</c>.</param>
    /// <param name="half0">Half <c>0</c> of the saved state.</param>
    /// <param name="half1">Half <c>1</c> of the saved state.</param>
    /// <seealso cref="RandomStreamState">random stream state</seealso>
    /// <seealso cref="SeedRandomStream">seed random stream</seealso>
    [FadeBasicCommand("set random stream state")]
    public static void SetRandomStreamState(int id, int half0, int half1)
    {
        if (!IsRandomStream(id, "set the state of")) return;
        RandomStreams[id] = ((ulong)(uint)half1 << 32) | (uint)half0;
    }
}
