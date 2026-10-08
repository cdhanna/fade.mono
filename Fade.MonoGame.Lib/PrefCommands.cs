using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Saves a whole number under an index, so that it is still there the next time the game runs.</para>
    /// <para>Read it back with <see cref="GetPref(int)">get pref</see>.</para>
    /// </summary>
    /// <remarks>
    /// Preferences are for the handful of settings a player expects a game to remember: how
    /// loud the sound is, whether it was fullscreen, the last level they picked. Each one is a
    /// whole number kept under an index that you choose.
    ///
    /// They are written to a file called <c>prefs.json</c> in the folder the game is run from,
    /// straight away, every time one changes. There is nothing to save at the end. Setting a
    /// preference to the value it already has does not write anything, so it is safe to call
    /// this every frame.
    ///
    /// Pick your indexes yourself, and keep them the same from one version of the game to the
    /// next, because the index is the only thing that says which setting a saved number is.
    /// Constants are a good way to give them names. Indexes run from <c>0</c> to <c>65535</c>.
    ///
    /// In the browser there is no file to write, so preferences last only until the page is closed.
    /// </remarks>
    /// <example>
    /// Remember a volume setting between runs:
    /// <code>
    /// #constant PREF_VOLUME 1
    ///
    /// ` 10 the first time the game is ever run, and whatever was saved after that
    /// volume = get pref(PREF_VOLUME, 10)
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// do
    ///   IF new upKey() THEN volume = volume + 1
    ///   IF new downKey() THEN volume = volume - 1
    ///
    ///   ` this only writes the file when the number has changed
    ///   set pref PREF_VOLUME, volume
    ///
    ///   set text 1, "volume: " + str$(volume)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Which preference to set, from <c>0</c> to <c>65535</c>.</param>
    /// <param name="value">The number to save.</param>
    /// <seealso cref="GetPref(int)">get pref</seealso>
    /// <seealso cref="PrefExists">pref exists</seealso>
    /// <seealso cref="ClearPrefs">clear prefs</seealso>
    [FadeBasicCommand("set pref")]
    public static void SetPref(int index, int value)
    {
        PrefsSystem.Set(index, value);
    }

    /// <summary>
    /// <para>Returns the number saved under an index by <see cref="SetPref">set pref</see>.</para>
    /// <para>Returns <c>0</c> if nothing has ever been saved there.</para>
    /// </summary>
    /// <remarks>
    /// The number may have been saved by an earlier run of the game. That is the point of a preference.
    ///
    /// When <c>0</c> is not a sensible starting value, give the one you want as a second
    /// number, and that is returned instead for a preference that has never been set. To
    /// find out whether one has been set at all, use <see cref="PrefExists">pref exists</see>.
    /// </remarks>
    /// <example>
    /// Count how many times the game has been run:
    /// <code>
    /// #constant PREF_RUNS 1
    ///
    /// runs = get pref(PREF_RUNS) + 1
    /// set pref PREF_RUNS, runs
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "run number " + str$(runs)
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Which preference to read.</param>
    /// <returns>The saved number, or <c>0</c> if there is not one.</returns>
    /// <seealso cref="SetPref">set pref</seealso>
    /// <seealso cref="PrefExists">pref exists</seealso>
    [FadeBasicCommand("get pref")]
    public static int GetPref(int index)
    {
        return PrefsSystem.Get(index, 0);
    }

    /// <summary>
    /// <para>Returns the number saved under an index by <see cref="SetPref">set pref</see>,
    /// or a number of your choosing if nothing has ever been saved there.</para>
    /// </summary>
    /// <remarks>
    /// This is the one to use for a setting whose starting value is not <c>0</c>, like a
    /// volume that starts at full. The default is only returned; it is not saved. Reading a
    /// preference never changes it.
    /// </remarks>
    /// <example>
    /// A volume that starts at 10 until the player changes it:
    /// <code>
    /// #constant PREF_VOLUME 1
    ///
    /// volume = get pref(PREF_VOLUME, 10)
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "volume: " + str$(volume)
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Which preference to read.</param>
    /// <param name="defaultValue">What to return if the preference has never been set.</param>
    /// <returns>The saved number, or <paramref name="defaultValue"/> if there is not one.</returns>
    /// <seealso cref="SetPref">set pref</seealso>
    /// <seealso cref="PrefExists">pref exists</seealso>
    [FadeBasicCommand("get pref")]
    public static int GetPref(int index, int defaultValue)
    {
        return PrefsSystem.Get(index, defaultValue);
    }

    /// <summary>
    /// <para>Checks whether a number has been saved under an index.</para>
    /// <para>Returns <c>1</c> if it has, and <c>0</c> if the index is free.</para>
    /// </summary>
    /// <remarks>
    /// A preference that was set to <c>0</c> exists. This is how to tell that apart from a
    /// preference that has never been set, which <see cref="GetPref(int)">get pref</see>
    /// also reports as <c>0</c>.
    /// </remarks>
    /// <example>
    /// Show a welcome the first time the game is ever run:
    /// <code>
    /// #constant PREF_SEEN_WELCOME 1
    ///
    /// font 1, "font"
    /// IF pref exists(PREF_SEEN_WELCOME)
    ///   text 1, 470, 200, 1, "welcome back"
    /// ELSE
    ///   text 1, 470, 200, 1, "hello, this is your first time here"
    ///   set pref PREF_SEEN_WELCOME, 1
    /// ENDIF
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Which preference to check.</param>
    /// <returns><c>1</c> if a number has been saved there, <c>0</c> if not.</returns>
    /// <seealso cref="SetPref">set pref</seealso>
    /// <seealso cref="GetFreePrefNextId">free pref id</seealso>
    [FadeBasicCommand("pref exists")]
    public static int PrefExists(int index)
    {
        return PrefsSystem.Exists(index) ? 1 : 0;
    }

    /// <summary>
    /// <para>Forgets every preference, and deletes the file they are saved in.</para>
    /// </summary>
    /// <remarks>
    /// After this, every preference reads as if it had never been set. The next
    /// <see cref="SetPref">set pref</see> starts a new file. This is what a "reset to
    /// defaults" button calls, and it is handy while you are testing what a brand new
    /// player sees.
    /// </remarks>
    /// <example>
    /// Wipe the saved settings when a key is pressed:
    /// <code>
    /// #constant PREF_VOLUME 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// do
    ///   IF new spaceKey() THEN clear prefs
    ///   set text 1, "volume: " + str$(get pref(PREF_VOLUME, 10))
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <seealso cref="SetPref">set pref</seealso>
    /// <seealso cref="PrefExists">pref exists</seealso>
    [FadeBasicCommand("clear prefs")]
    public static void ClearPrefs()
    {
        PrefsSystem.Clear();
    }

    /// <summary>
    /// <para>Returns the next preference index that is not taken, without taking it.</para>
    /// <para>That is one more than the highest index that has a number saved under it.</para>
    /// </summary>
    /// <remarks>
    /// Be careful with this for settings. Preferences are remembered between runs, so an
    /// index that was free last time is taken this time, and asking again gives a different
    /// answer. A setting that has to be read back next run needs an index you chose
    /// yourself and wrote into the program.
    ///
    /// This is for things that are added to over time, like a list of scores, where each
    /// new one just needs somewhere of its own to go.
    /// </remarks>
    /// <example>
    /// Show where the next new preference would go:
    /// <code>
    /// nextIndex = free pref id(nextIndex)
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "next free index: " + str$(nextIndex)
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Receives the next free preference index.</param>
    /// <returns>The next free preference index.</returns>
    /// <seealso cref="ReservePrefNextId">reserve pref id</seealso>
    /// <seealso cref="PrefExists">pref exists</seealso>
    [FadeBasicCommand("free pref id")]
    public static int GetFreePrefNextId(ref int index)
    {
        index = PrefsSystem.HighestTakenIndex() + 1;
        return index;
    }

    /// <summary>
    /// <para>Takes the next free preference index, by saving a <c>0</c> under it.</para>
    /// </summary>
    /// <remarks>
    /// The index is taken for good, not just for this run: it is saved like any other
    /// preference, and is still taken the next time the game starts. So every run that
    /// calls this takes a new index. See <see cref="GetFreePrefNextId">free pref id</see>
    /// for why that makes it the wrong tool for ordinary settings.
    /// </remarks>
    /// <example>
    /// Keep every score that has ever been made, each under its own index:
    /// <code>
    /// score = 1200
    ///
    /// slot = reserve pref id(slot)
    /// set pref slot, score
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "saved under index " + str$(slot)
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="index">Receives the preference index that was taken.</param>
    /// <returns>The preference index that was taken.</returns>
    /// <seealso cref="GetFreePrefNextId">free pref id</seealso>
    /// <seealso cref="SetPref">set pref</seealso>
    [FadeBasicCommand("reserve pref id")]
    public static int ReservePrefNextId(ref int index)
    {
        GetFreePrefNextId(ref index);
        PrefsSystem.Set(index, 0);
        return index;
    }
}
