using FadeBasic.SourceGenerators;
using Steamworks;
using Steamworks.Data;

namespace Fade.MonoGame.Steam;

public partial class FadeSteamCommands
{
    // The timeline came with a newer Steam than the rest of what is used here. A player whose
    // Steam does not have it gets one line in the log, and the commands do nothing after that.
    static bool _timelineMissing;

    static void Timeline(Action work)
    {
        if (!SteamSystem.Available || _timelineMissing) return;
        try
        {
            work();
        }
        catch (Exception ex)
        {
            _timelineMissing = true;
            Console.WriteLine("[steam] the timeline cannot be used: " + ex.Message);
        }
    }

    /// <summary>
    /// <para>Puts a mark on the player's Steam timeline, at this moment of the game.</para>
    /// </summary>
    /// <remarks>
    /// The timeline is the bar under a recording that Steam makes of the game. A mark on it
    /// is a small icon with a title, and it is how a player finds a moment again when they
    /// go to save a clip. A game that marks nothing leaves them an empty bar to scrub along.
    ///
    /// The icon is the name of one of Steam's own, like <c>steam_star</c>, <c>steam_flag</c>,
    /// <c>steam_crown</c>, <c>steam_checkmark</c>, <c>steam_heart</c>, <c>steam_death</c> or
    /// <c>steam_marker</c>, or the name of one that was uploaded for the game in Steamworks.
    ///
    /// The priority is from <c>0</c> to <c>1000</c>. When two marks are too close together
    /// to both be drawn, the one with the higher priority is the one that shows.
    ///
    /// The last value says how good a clip the moment would make: <c>0</c> for a moment
    /// that is not worth a clip, <c>1</c> for one that is, and <c>2</c> for the best ones,
    /// which Steam offers to the player first.
    ///
    /// Nothing happens for a player who has recording turned off, or whose Steam is too
    /// old to have a timeline.
    /// </remarks>
    /// <example>
    /// Mark the moment that a key is pressed:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN add steam timeline event "Jump", "A very good jump", "steam_star", 100, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="title">The words that show with the mark.</param>
    /// <param name="description">More about it, shown when the player points at the mark.</param>
    /// <param name="icon">The name of the icon, like <c>steam_star</c>.</param>
    /// <param name="priority">From <c>0</c> to <c>1000</c>. The higher of two marks that overlap is shown.</param>
    /// <param name="clip"><c>0</c> for no clip, <c>1</c> for a moment worth a clip, <c>2</c> for one of the best.</param>
    /// <seealso cref="SetSteamTimelineTooltip">set steam timeline tooltip</seealso>
    /// <seealso cref="SetSteamTimelineMode">set steam timeline mode</seealso>
    [FadeBasicCommand("add steam timeline event")]
    public static void AddSteamTimelineEvent(string title, string description, string icon, int priority, int clip)
    {
        if (string.IsNullOrEmpty(title)) return;
        if (string.IsNullOrEmpty(icon)) icon = "steam_marker";

        var clipPriority = clip <= 0 ? TimelineEventClipPriority.None
            : clip == 1 ? TimelineEventClipPriority.Standard
            : TimelineEventClipPriority.Featured;
        var shown = (uint)Math.Clamp(priority, 0, 1000);

        Timeline(() => SteamTimeline.AddInstantaneousTimelineEvent(title, description ?? "", icon, shown, 0, clipPriority));
    }

    // the stretches of the timeline that have been started and not ended yet, by the id that the
    // program gave each one
    static readonly Dictionary<int, TimelineEventHandle> OpenRanges = new();

    /// <summary>
    /// <para>Starts a stretch of the player's Steam timeline, from this moment until it is ended.</para>
    /// </summary>
    /// <remarks>
    /// Where <see cref="AddSteamTimelineEvent">add steam timeline event</see> marks one
    /// moment, this marks a stretch of time, like a round or a shop, and it is drawn on the
    /// bar from where it starts to where it ends, with its title and icon. End it with
    /// <see cref="EndSteamTimelineRange">end steam timeline range</see>.
    ///
    /// The id is any number that you choose, and it is how the stretch is ended. Starting a
    /// stretch with an id that already has one open ends the one that was open first, so a
    /// game can start "Round 2" with the same id as "Round 1" and not end the first by hand.
    ///
    /// The icon and the priority are the same as for
    /// <see cref="AddSteamTimelineEvent">add steam timeline event</see>.
    ///
    /// Nothing happens for a player who has recording turned off, or whose Steam is too
    /// old to have a timeline.
    /// </remarks>
    /// <example>
    /// A stretch for every round, and a new one with every press of a key:
    /// <code>
    /// round = 1
    /// start steam timeline range 1, "Round 1", "", "steam_flag", 50
    ///
    /// DO
    ///   IF new spaceKey()
    ///     round = round + 1
    ///     start steam timeline range 1, "Round " + str$(round), "", "steam_flag", 50
    ///   ENDIF
    ///   IF new returnKey() THEN end steam timeline range 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Any number that you choose, to end the stretch by.</param>
    /// <param name="title">The words that show with the stretch.</param>
    /// <param name="description">More about it, shown when the player points at it.</param>
    /// <param name="icon">The name of the icon, like <c>steam_flag</c>.</param>
    /// <param name="priority">From <c>0</c> to <c>1000</c>. The higher of two that overlap is shown.</param>
    /// <seealso cref="EndSteamTimelineRange">end steam timeline range</seealso>
    /// <seealso cref="AddSteamTimelineEvent">add steam timeline event</seealso>
    [FadeBasicCommand("start steam timeline range")]
    public static void StartSteamTimelineRange(int id, string title, string description, string icon, int priority)
    {
        if (string.IsNullOrEmpty(title)) return;
        if (string.IsNullOrEmpty(icon)) icon = "steam_marker";
        var shown = (uint)Math.Clamp(priority, 0, 1000);

        Timeline(() =>
        {
            if (OpenRanges.Remove(id, out var open)) SteamTimeline.EndRangeTimelineEvent(open, 0);
            OpenRanges[id] = SteamTimeline.StartRangeTimelineEvent(title, description ?? "", icon, shown, 0, TimelineEventClipPriority.None);
        });
    }

    /// <summary>
    /// <para>Ends a stretch of the Steam timeline that was started with <see cref="StartSteamTimelineRange">start steam timeline range</see>.</para>
    /// </summary>
    /// <remarks>
    /// The stretch ends at this moment. Ending an id that has no stretch open does nothing,
    /// so it is safe to end one just in case.
    /// </remarks>
    /// <example>
    /// Start a stretch with one key, and end it with another:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN start steam timeline range 1, "In the shop", "", "steam_cart", 50
    ///   IF new returnKey() THEN end steam timeline range 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id that the stretch was started with.</param>
    /// <seealso cref="StartSteamTimelineRange">start steam timeline range</seealso>
    [FadeBasicCommand("end steam timeline range")]
    public static void EndSteamTimelineRange(int id)
    {
        Timeline(() =>
        {
            if (OpenRanges.Remove(id, out var open)) SteamTimeline.EndRangeTimelineEvent(open, 0);
        });
    }

    /// <summary>
    /// <para>Sets the words that the Steam timeline shows for the part of the game that starts now.</para>
    /// </summary>
    /// <remarks>
    /// Where a mark is one moment, this is a stretch of time: "Round 4", "The boss". The
    /// words show when the player points anywhere along the bar from here on, until they
    /// are set again or taken away with
    /// <see cref="ClearSteamTimelineTooltip">clear steam timeline tooltip</see>.
    /// </remarks>
    /// <example>
    /// Say which round the player is on:
    /// <code>
    /// round = 1
    /// set steam timeline tooltip "Round " + str$(round)
    ///
    /// DO
    ///   IF new spaceKey()
    ///     round = round + 1
    ///     set steam timeline tooltip "Round " + str$(round)
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="text">The words to show. An empty string takes them away.</param>
    /// <seealso cref="ClearSteamTimelineTooltip">clear steam timeline tooltip</seealso>
    /// <seealso cref="AddSteamTimelineEvent">add steam timeline event</seealso>
    [FadeBasicCommand("set steam timeline tooltip")]
    public static void SetSteamTimelineTooltip(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            ClearSteamTimelineTooltip();
            return;
        }

        Timeline(() => SteamTimeline.SetTimelineTooltip(text, 0));
    }

    /// <summary>
    /// <para>Takes away the words that were set with <see cref="SetSteamTimelineTooltip">set steam timeline tooltip</see>.</para>
    /// </summary>
    /// <remarks>
    /// From here on the bar has no words of its own, until some are set again. The part of
    /// the bar that came before keeps the words that it had.
    /// </remarks>
    /// <example>
    /// Show words with one key, and take them away with another:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN set steam timeline tooltip "In the cave"
    ///   IF new returnKey() THEN clear steam timeline tooltip
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetSteamTimelineTooltip">set steam timeline tooltip</seealso>
    [FadeBasicCommand("clear steam timeline tooltip")]
    public static void ClearSteamTimelineTooltip()
    {
        Timeline(() => SteamTimeline.ClearTimelineTooltip(0));
    }

    /// <summary>
    /// <para>Tells the Steam timeline what kind of thing the player is doing from now on, which sets the color of the bar.</para>
    /// </summary>
    /// <remarks>
    /// The kinds are <c>1</c> for playing, <c>2</c> for getting ready to play, like a shop
    /// or a screen between rounds, <c>3</c> for a menu, and <c>4</c> for a loading screen.
    /// Each has a color of its own on the bar, so a player can see at a glance where the
    /// game was and where the menus were.
    ///
    /// Any other number is ignored.
    /// </remarks>
    /// <example>
    /// Start in the menu, and switch to playing with a key:
    /// <code>
    /// set steam timeline mode 3
    ///
    /// DO
    ///   IF new spaceKey() THEN set steam timeline mode 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="mode"><c>1</c> playing, <c>2</c> getting ready, <c>3</c> a menu, <c>4</c> loading.</param>
    /// <seealso cref="AddSteamTimelineEvent">add steam timeline event</seealso>
    [FadeBasicCommand("set steam timeline mode")]
    public static void SetSteamTimelineMode(int mode)
    {
        if (mode < 1 || mode > 4) return;
        Timeline(() => SteamTimeline.SetTimelineGameMode((TimelineGameMode)mode));
    }
}
