using FadeBasic.SourceGenerators;
using Steamworks;
using Steamworks.Data;

namespace Fade.MonoGame.Steam;

public partial class FadeSteamCommands
{
    /// <summary>
    /// <para>Unlocks an achievement for the player, and tells Steam straight away.</para>
    /// <para>The name is the achievement's API name from Steamworks, not the title that players see.</para>
    /// </summary>
    /// <remarks>
    /// Achievements are set up in Steamworks, where each one gets an API name like
    /// <c>FIRST_WIN</c>. That is the name to use here.
    ///
    /// Unlocking one that is already unlocked does nothing, so there is no need to check
    /// first, and it is safe to call this every time the thing happens. Steam shows its
    /// own popup the first time.
    ///
    /// An achievement that has not been published in Steamworks, or a name that is spelled
    /// wrong, is ignored without an error.
    /// </remarks>
    /// <example>
    /// Unlock an achievement the first time a key is pressed:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN unlock steam achievement "FIRST_POP"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <seealso cref="IsSteamAchievementUnlocked">steam achievement unlocked</seealso>
    /// <seealso cref="ClearSteamAchievement">clear steam achievement</seealso>
    [FadeBasicCommand("unlock steam achievement")]
    public static void UnlockSteamAchievement(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return;
        new Achievement(name).Trigger();
    }

    /// <summary>
    /// <para>Checks whether the player has unlocked an achievement.</para>
    /// <para>Returns <c>1</c> if they have, and <c>0</c> if not, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// Use this to draw the game's own list of achievements, or to skip something that
    /// only needs to happen until an achievement has been earned. You do not need it
    /// before <see cref="UnlockSteamAchievement">unlock steam achievement</see>, which is
    /// safe to call twice.
    /// </remarks>
    /// <example>
    /// Show whether an achievement has been earned:
    /// <code>
    /// font 1, "font"
    /// IF steam achievement unlocked("FIRST_POP")
    ///   text 1, 470, 200, 1, "first pop: earned"
    /// ELSE
    ///   text 1, 470, 200, 1, "first pop: not yet"
    /// ENDIF
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <returns><c>1</c> if it is unlocked, <c>0</c> if not.</returns>
    /// <seealso cref="UnlockSteamAchievement">unlock steam achievement</seealso>
    /// <seealso cref="ClearSteamAchievement">clear steam achievement</seealso>
    [FadeBasicCommand("steam achievement unlocked")]
    public static int IsSteamAchievementUnlocked(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return 0;
        return new Achievement(name).State ? 1 : 0;
    }

    /// <summary>
    /// <para>Locks an achievement again, as if the player had never earned it.</para>
    /// <para>This is a tool for testing. A finished game should never take an achievement away.</para>
    /// </summary>
    /// <remarks>
    /// While you are building the game you will unlock every achievement on your own
    /// account long before the game is done, and then you cannot see the popup again.
    /// This puts one back so that you can.
    /// </remarks>
    /// <example>
    /// Earn an achievement with one key, and take it back with another:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN unlock steam achievement "FIRST_POP"
    ///   IF new returnKey() THEN clear steam achievement "FIRST_POP"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <seealso cref="UnlockSteamAchievement">unlock steam achievement</seealso>
    /// <seealso cref="IsSteamAchievementUnlocked">steam achievement unlocked</seealso>
    [FadeBasicCommand("clear steam achievement")]
    public static void ClearSteamAchievement(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return;
        new Achievement(name).Clear();
        SteamUserStats.StoreStats();
    }

    /// <summary>
    /// <para>Shows Steam's popup that says how far along the player is towards an achievement, like "450 of 1000".</para>
    /// <para>It only shows the popup. It does not save the progress, and it does not unlock anything.</para>
    /// </summary>
    /// <remarks>
    /// An achievement that is earned bit by bit, like popping a thousand fish, is driven
    /// by a stat. In Steamworks, the achievement is given a stat and the number that the
    /// stat has to reach. The game keeps the stat up to date with
    /// <see cref="AddSteamStat">add steam stat</see> and
    /// <see cref="StoreSteamStats">store steam stats</see>, and Steam unlocks the
    /// achievement by itself when the stat gets there. The progress bar on the player's
    /// achievements page comes from the stat too.
    ///
    /// This command is the extra nudge: a popup in the corner of the screen, at a moment
    /// of your choosing. Show it at milestones, like every hundred, and not every time
    /// the number goes up, or the popups never stop.
    ///
    /// Nothing happens when the achievement is already unlocked, or when the progress is
    /// not less than the goal. To finish an achievement, use
    /// <see cref="UnlockSteamAchievement">unlock steam achievement</see>, or let the stat
    /// do it.
    /// </remarks>
    /// <example>
    /// Count pops in a stat, and show the progress at every hundred:
    /// <code>
    /// DO
    ///   IF new spaceKey()
    ///     add steam stat "FISH_POPPED", 1
    ///     popped = steam stat("FISH_POPPED")
    ///     IF popped mod 100 = 0
    ///       ` send the stat, and let the player see how they are doing
    ///       store steam stats
    ///       show steam achievement progress "POP_1000", popped, 1000
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <param name="progress">How far the player has got.</param>
    /// <param name="goal">How far they have to get.</param>
    /// <seealso cref="UnlockSteamAchievement">unlock steam achievement</seealso>
    /// <seealso cref="AddSteamStat">add steam stat</seealso>
    /// <seealso cref="StoreSteamStats">store steam stats</seealso>
    [FadeBasicCommand("show steam achievement progress")]
    public static void ShowSteamAchievementProgress(string name, int progress, int goal)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return;
        if (progress < 0 || progress >= goal) return;
        SteamUserStats.IndicateAchievementProgress(name, progress, goal);
    }

    /// <summary>
    /// <para>Returns how many achievements the game has.</para>
    /// <para>Returns <c>0</c> when Steam is not up, or when the game has none.</para>
    /// </summary>
    /// <remarks>
    /// The achievements are the ones that are set up and published in Steamworks. This is
    /// the start of drawing the game's own page of them: go from <c>0</c> to one less than
    /// the count, and ask for each one's name with
    /// <see cref="GetSteamAchievementName">steam achievement name$</see>.
    /// </remarks>
    /// <example>
    /// Say how many achievements the player has earned:
    /// <code>
    /// earned = 0
    /// FOR i = 0 TO steam achievement count() - 1
    ///   IF steam achievement unlocked(steam achievement name$(i)) THEN earned = earned + 1
    /// NEXT
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, str$(earned) + " of " + str$(steam achievement count()) + " achievements"
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>How many achievements there are.</returns>
    /// <seealso cref="GetSteamAchievementName">steam achievement name$</seealso>
    /// <seealso cref="IsSteamAchievementUnlocked">steam achievement unlocked</seealso>
    [FadeBasicCommand("steam achievement count")]
    public static int GetSteamAchievementCount()
    {
        if (!SteamSystem.Available) return 0;
        return SteamUserStats.Achievements.Count();
    }

    /// <summary>
    /// <para>Returns the API name of one of the game's achievements, by its place in the list.</para>
    /// <para>Returns an empty string when there is no achievement at that place.</para>
    /// </summary>
    /// <remarks>
    /// The API name is the one that every other achievement command takes, like
    /// <c>FIRST_POP</c>. It is not what players see. For that, pass it on to
    /// <see cref="GetSteamAchievementTitle">steam achievement title$</see>.
    ///
    /// The list is in the order that the achievements have in Steamworks, from <c>0</c>
    /// to one less than <see cref="GetSteamAchievementCount">steam achievement count</see>.
    /// </remarks>
    /// <example>
    /// Print the name and the title of every achievement:
    /// <code>
    /// FOR i = 0 TO steam achievement count() - 1
    ///   name$ = steam achievement name$(i)
    ///   print name$ + ": " + steam achievement title$(name$)
    /// NEXT
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="index">Which achievement, from <c>0</c>.</param>
    /// <returns>The API name of that achievement.</returns>
    /// <seealso cref="GetSteamAchievementCount">steam achievement count</seealso>
    /// <seealso cref="GetSteamAchievementTitle">steam achievement title$</seealso>
    /// <seealso cref="GetSteamAchievementDescription">steam achievement description$</seealso>
    [FadeBasicCommand("steam achievement name$")]
    public static string GetSteamAchievementName(int index)
    {
        return SteamAchievements.NameAt(index);
    }

    /// <summary>
    /// <para>Returns the title of an achievement, as players see it.</para>
    /// <para>Returns an empty string for an achievement that does not exist, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// The title is the one that was written in Steamworks, in the language that the
    /// player has picked, if the achievement has been translated into it.
    ///
    /// A title can have characters in it that a game's font does not have, the same as
    /// a player's name can.
    /// </remarks>
    /// <example>
    /// Show the title of the first achievement:
    /// <code>
    /// font 1, "font"
    /// text 1, 470, 200, 1, steam achievement title$(steam achievement name$(0))
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <returns>The title of the achievement.</returns>
    /// <seealso cref="GetSteamAchievementDescription">steam achievement description$</seealso>
    /// <seealso cref="GetSteamAchievementName">steam achievement name$</seealso>
    [FadeBasicCommand("steam achievement title$")]
    public static string GetSteamAchievementTitle(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return "";
        return new Achievement(name).Name ?? "";
    }

    /// <summary>
    /// <para>Returns the description of an achievement, which says how it is earned.</para>
    /// <para>Returns an empty string for an achievement that does not exist, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// The description is the one that was written in Steamworks, in the player's
    /// language if there is a translation.
    /// </remarks>
    /// <example>
    /// Show the title and the description of the first achievement:
    /// <code>
    /// name$ = steam achievement name$(0)
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, steam achievement title$(name$)
    /// text 2, 470, 230, 1, steam achievement description$(name$)
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <returns>The description of the achievement.</returns>
    /// <seealso cref="GetSteamAchievementTitle">steam achievement title$</seealso>
    /// <seealso cref="GetSteamAchievementName">steam achievement name$</seealso>
    [FadeBasicCommand("steam achievement description$")]
    public static string GetSteamAchievementDescription(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return "";
        return new Achievement(name).Description ?? "";
    }

    /// <summary>
    /// <para>Puts the icon of an achievement into a texture, so that a sprite can show it.</para>
    /// <para>Returns <c>1</c> once the texture holds the icon, and <c>0</c> while Steam is still fetching it.</para>
    /// </summary>
    /// <remarks>
    /// Steam does not have an icon on hand until it has downloaded it, and asking is
    /// what starts the download. So the first call usually returns <c>0</c>. Call this
    /// every frame until it returns <c>1</c>, and do not use the texture before then.
    /// Once it has returned <c>1</c>, calling it again costs nothing, so it is fine to
    /// leave it in the loop.
    ///
    /// An achievement has two icons in Steamworks: one for when it is locked, and one
    /// for when it has been earned. The texture gets the one for how the achievement is
    /// now. Leaving the call in the loop means that the texture changes to the earned
    /// icon by itself when the player unlocks the achievement.
    ///
    /// The icons are 64 pixels across, or whatever size was uploaded to Steamworks. Use
    /// <c>size sprite</c> to draw one at another size. The texture id is one that you
    /// choose, the same as for a texture that is loaded from a file.
    ///
    /// It stays <c>0</c> for an achievement that does not exist, and when Steam is not up.
    /// </remarks>
    /// <example>
    /// Show the icon of the first achievement, once it has arrived:
    /// <code>
    /// name$ = steam achievement name$(0)
    /// shown = 0
    ///
    /// DO
    ///   ` 0 until steam has fetched the icon, and 1 from then on
    ///   IF steam achievement icon(50, name$)
    ///     IF shown = 0
    ///       sprite 1, 100, 100, 50
    ///       shown = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="textureId">The texture to put the icon into. Any texture id that you choose.</param>
    /// <param name="name">The API name of the achievement, as it is in Steamworks.</param>
    /// <returns><c>1</c> if the texture holds the icon, <c>0</c> if not yet.</returns>
    /// <seealso cref="GetSteamAchievementName">steam achievement name$</seealso>
    /// <seealso cref="GetSteamAchievementTitle">steam achievement title$</seealso>
    /// <seealso cref="IsSteamAchievementUnlocked">steam achievement unlocked</seealso>
    [FadeBasicCommand("steam achievement icon")]
    public static int LoadSteamAchievementIcon(int textureId, string name)
    {
        return SteamAchievements.LoadIcon(textureId, name) ? 1 : 0;
    }

    /// <summary>
    /// <para>Sets one of the player's Steam stats to a whole number.</para>
    /// <para>The change stays on this machine until <see cref="StoreSteamStats">store steam stats</see> sends it.</para>
    /// </summary>
    /// <remarks>
    /// A stat is a number that Steam keeps for each player: games played, fish popped,
    /// the best multiplier. Stats are set up in Steamworks, where each one gets an API
    /// name, and that is the name to use here. Steam can unlock an achievement by itself
    /// when a stat reaches a number, and it can show the stats of every player added up.
    ///
    /// Setting a stat is cheap and only changes Steam's copy on this machine. Call
    /// <see cref="StoreSteamStats">store steam stats</see> at a quiet moment to send the
    /// changes, like the end of a round. A stat that is not set up in Steamworks, or
    /// that is not a whole number there, is ignored.
    /// </remarks>
    /// <example>
    /// Keep the best score in a stat:
    /// <code>
    /// score = 1200
    /// IF score &gt; steam stat("BEST_SCORE")
    ///   set steam stat "BEST_SCORE", score
    ///   store steam stats
    /// ENDIF
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the stat, as it is in Steamworks.</param>
    /// <param name="value">The number to set it to.</param>
    /// <seealso cref="AddSteamStat">add steam stat</seealso>
    /// <seealso cref="GetSteamStat">steam stat</seealso>
    /// <seealso cref="StoreSteamStats">store steam stats</seealso>
    [FadeBasicCommand("set steam stat")]
    public static void SetSteamStat(string name, int value)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return;
        SteamUserStats.SetStat(name, value);
    }

    /// <summary>
    /// <para>Adds a whole number to one of the player's Steam stats.</para>
    /// <para>The change stays on this machine until <see cref="StoreSteamStats">store steam stats</see> sends it.</para>
    /// </summary>
    /// <remarks>
    /// This is the one to use for a stat that counts something. It is the same as reading
    /// the stat with <see cref="GetSteamStat">steam stat</see>, adding to it, and setting
    /// it with <see cref="SetSteamStat">set steam stat</see>. Add a negative number to
    /// take away.
    /// </remarks>
    /// <example>
    /// Count the games that the player has started:
    /// <code>
    /// add steam stat "GAMES_STARTED", 1
    /// store steam stats
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "games started: " + str$(steam stat("GAMES_STARTED"))
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the stat, as it is in Steamworks.</param>
    /// <param name="amount">How much to add.</param>
    /// <seealso cref="SetSteamStat">set steam stat</seealso>
    /// <seealso cref="GetSteamStat">steam stat</seealso>
    /// <seealso cref="StoreSteamStats">store steam stats</seealso>
    [FadeBasicCommand("add steam stat")]
    public static void AddSteamStat(string name, int amount)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return;
        SteamUserStats.AddStat(name, amount);
    }

    /// <summary>
    /// <para>Returns one of the player's Steam stats.</para>
    /// <para>Returns <c>0</c> for a stat that does not exist, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// This reads Steam's copy on this machine, so it already includes anything that
    /// <see cref="SetSteamStat">set steam stat</see> or
    /// <see cref="AddSteamStat">add steam stat</see> changed, whether or not it has been
    /// sent yet.
    /// </remarks>
    /// <example>
    /// Show a stat:
    /// <code>
    /// font 1, "font"
    /// text 1, 470, 200, 1, "fish popped: " + str$(steam stat("FISH_POPPED"))
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="name">The API name of the stat, as it is in Steamworks.</param>
    /// <returns>The stat, or <c>0</c> if there is not one.</returns>
    /// <seealso cref="SetSteamStat">set steam stat</seealso>
    /// <seealso cref="AddSteamStat">add steam stat</seealso>
    [FadeBasicCommand("steam stat")]
    public static int GetSteamStat(string name)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(name)) return 0;
        return SteamUserStats.GetStatInt(name);
    }

    /// <summary>
    /// <para>Sends every changed stat to Steam.</para>
    /// </summary>
    /// <remarks>
    /// <see cref="SetSteamStat">set steam stat</see> and
    /// <see cref="AddSteamStat">add steam stat</see> only change the copy on this machine.
    /// This is what makes them count. Steam also sends them when the game closes, but a
    /// game that crashes loses whatever was not sent.
    ///
    /// Do not call it every frame. Steam limits how often a game may send, and sending
    /// is slow next to setting. The end of a round or of a game is the place for it.
    /// </remarks>
    /// <example>
    /// Count pops as they happen, and send the count now and then:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN add steam stat "FISH_POPPED", 1
    ///   IF new returnKey() THEN store steam stats
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetSteamStat">set steam stat</seealso>
    /// <seealso cref="AddSteamStat">add steam stat</seealso>
    [FadeBasicCommand("store steam stats")]
    public static void StoreSteamStats()
    {
        if (!SteamSystem.Available) return;
        SteamUserStats.StoreStats();
    }
}
