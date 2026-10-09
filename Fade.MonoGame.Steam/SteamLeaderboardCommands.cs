using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Steam;

public partial class FadeSteamCommands
{
    /// <summary>
    /// <para>Returns the next leaderboard id that is not taken, without taking it.</para>
    /// </summary>
    /// <remarks>
    /// A leaderboard is known to the game by an id that you choose, the same way that a
    /// sprite is. You can write the ids into the program yourself, or ask for a free one
    /// here. To take the id as well, so that the next call gives a different one, use
    /// <see cref="ReserveSteamLeaderboardId">reserve steam leaderboard id</see>.
    /// </remarks>
    /// <example>
    /// Find a leaderboard under whichever id is free:
    /// <code>
    /// boardId = free steam leaderboard id(boardId)
    /// steam leaderboard boardId, "Best Times"
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Receives the next free leaderboard id.</param>
    /// <returns>The next free leaderboard id.</returns>
    /// <seealso cref="ReserveSteamLeaderboardId">reserve steam leaderboard id</seealso>
    /// <seealso cref="FindSteamLeaderboard(int, string)">steam leaderboard</seealso>
    [FadeBasicCommand("free steam leaderboard id")]
    public static int GetFreeSteamLeaderboardId(ref int id)
    {
        id = SteamLeaderboards.FreeId();
        return id;
    }

    /// <summary>
    /// <para>Takes the next free leaderboard id, so that nothing else is given it.</para>
    /// </summary>
    /// <remarks>
    /// Use this when a game has several leaderboards and you would sooner not number
    /// them by hand. Reserve an id for each, keep the ids in variables, and then find
    /// each one with <see cref="FindSteamLeaderboard(int, string)">steam leaderboard</see>.
    /// </remarks>
    /// <example>
    /// Reserve ids for two leaderboards and find them both:
    /// <code>
    /// shortBoard = reserve steam leaderboard id(shortBoard)
    /// longBoard = reserve steam leaderboard id(longBoard)
    ///
    /// steam leaderboard shortBoard, "Short Game"
    /// steam leaderboard longBoard, "Long Game"
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">Receives the leaderboard id that was taken.</param>
    /// <returns>The leaderboard id that was taken.</returns>
    /// <seealso cref="GetFreeSteamLeaderboardId">free steam leaderboard id</seealso>
    /// <seealso cref="FindSteamLeaderboard(int, string)">steam leaderboard</seealso>
    [FadeBasicCommand("reserve steam leaderboard id")]
    public static int ReserveSteamLeaderboardId(ref int id)
    {
        id = SteamLeaderboards.FreeId();
        SteamLeaderboards.Reserve(id);
        return id;
    }

    /// <summary>
    /// <para>Starts finding a leaderboard that already exists on Steam, and gives it an id.</para>
    /// <para>Finding takes a moment. Other leaderboard commands can be called straight away, and they wait for it.</para>
    /// </summary>
    /// <remarks>
    /// Leaderboards are made in Steamworks, where each one has a name. This looks one up
    /// by that name. The name has to match exactly, capitals and all. Do this once, when
    /// the game starts, for each leaderboard that the game has.
    ///
    /// Nothing in Steam answers straight away, so this only starts the search.
    /// <see cref="GetSteamLeaderboardState">steam leaderboard state</see> says how it
    /// is going. You do not have to wait for it, though: a score that is submitted, or
    /// scores that are loaded, before the leaderboard has been found are held until it
    /// has been.
    ///
    /// If there is no leaderboard with that name, the search fails, and so does
    /// everything that was waiting on it. To have a missing leaderboard made instead,
    /// give a third value. See the other form of this command.
    /// </remarks>
    /// <example>
    /// Find a leaderboard and say when it is ready:
    /// <code>
    /// steam leaderboard 1, "Best Times"
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   ` 1 is still looking, 2 is found, 3 is failed
    ///   state = steam leaderboard state(1)
    ///   IF state = 1 THEN set text 1, "looking for the leaderboard"
    ///   IF state = 2 THEN set text 1, "found it"
    ///   IF state = 3 THEN set text 1, "there is no such leaderboard"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id to know this leaderboard by. Any whole number that you choose.</param>
    /// <param name="name">The name of the leaderboard, exactly as it is in Steamworks.</param>
    /// <seealso cref="GetSteamLeaderboardState">steam leaderboard state</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    [FadeBasicCommand("steam leaderboard")]
    public static void FindSteamLeaderboard(int id, string name)
    {
        SteamLeaderboards.Find(id, name, create: false, lowestFirst: false);
    }

    /// <summary>
    /// <para>Starts finding a leaderboard on Steam, makes it if it is not there, and gives it an id.</para>
    /// <para>The third value says which scores are best: <c>1</c> if the lowest score wins, and <c>0</c> if the highest does.</para>
    /// </summary>
    /// <remarks>
    /// This is the easy way to get going: the first player to run the game makes the
    /// leaderboard, and nothing has to be set up in Steamworks first. The cost is that a
    /// typo in the name makes a second, empty leaderboard instead of an error.
    ///
    /// Which scores are best only matters when the leaderboard is made. One that is
    /// already there keeps the order that it was made with, whatever you say here.
    ///
    /// Pick <c>1</c> for a game where less is better, like a time or a number of moves,
    /// and <c>0</c> for points.
    /// </remarks>
    /// <example>
    /// Use a leaderboard where the fewest moves wins, and make it if this is the first run:
    /// <code>
    /// steam leaderboard 1, "Fewest Moves", 1
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id to know this leaderboard by. Any whole number that you choose.</param>
    /// <param name="name">The name of the leaderboard.</param>
    /// <param name="lowestFirst"><c>1</c> if the lowest score is the best one, <c>0</c> if the highest is.</param>
    /// <seealso cref="GetSteamLeaderboardState">steam leaderboard state</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    [FadeBasicCommand("steam leaderboard")]
    public static void FindSteamLeaderboard(int id, string name, int lowestFirst)
    {
        SteamLeaderboards.Find(id, name, create: true, lowestFirst: lowestFirst != 0);
    }

    /// <summary>
    /// <para>Returns how the search for a leaderboard is going.</para>
    /// <para><c>0</c> is not started, <c>1</c> is still looking, <c>2</c> is found, and <c>3</c> is failed.</para>
    /// </summary>
    /// <remarks>
    /// The search is started by <see cref="FindSteamLeaderboard(int, string)">steam leaderboard</see>.
    /// It fails when there is no leaderboard with that name, when Steam is not up, and
    /// when Steam does not answer for a long time. To try again, call
    /// <see cref="FindSteamLeaderboard(int, string)">steam leaderboard</see> again.
    ///
    /// The other things that take a while use the same four numbers:
    /// <see cref="GetSteamSubmitState">steam submit state</see> and
    /// <see cref="GetSteamScoresState">steam scores state</see>.
    /// </remarks>
    /// <example>
    /// Hide the online scores when the leaderboard cannot be reached:
    /// <code>
    /// steam leaderboard 1, "Best Times"
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam leaderboard state(1) = 3
    ///     set text 1, "online scores are not available"
    ///   ELSE
    ///     set text 1, "online scores"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns><c>0</c> for not started, <c>1</c> for looking, <c>2</c> for found, <c>3</c> for failed.</returns>
    /// <seealso cref="FindSteamLeaderboard(int, string)">steam leaderboard</seealso>
    /// <seealso cref="GetSteamSubmitState">steam submit state</seealso>
    /// <seealso cref="GetSteamScoresState">steam scores state</seealso>
    [FadeBasicCommand("steam leaderboard state")]
    public static int GetSteamLeaderboardState(int id)
    {
        return SteamLeaderboards.FindState(id);
    }

    /// <summary>
    /// <para>Returns how many players have a score on a leaderboard.</para>
    /// <para>Returns <c>0</c> until the leaderboard has been found.</para>
    /// </summary>
    /// <remarks>
    /// Every player has at most one score on a leaderboard, their best, so this is a
    /// number of players. It is read when the leaderboard is found and again each time
    /// that scores are loaded, so it can be a little behind.
    ///
    /// It is what you need to page through a leaderboard: with
    /// <see cref="LoadSteamScores">load steam scores</see> fetching ten at a time, this
    /// says when the last page has been reached.
    /// </remarks>
    /// <example>
    /// Show how many players are on a leaderboard:
    /// <code>
    /// steam leaderboard 1, "Best Times"
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   set text 1, str$(steam leaderboard size(1)) + " players"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns>How many players have a score on it.</returns>
    /// <seealso cref="FindSteamLeaderboard(int, string)">steam leaderboard</seealso>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    [FadeBasicCommand("steam leaderboard size")]
    public static int GetSteamLeaderboardSize(int id)
    {
        return SteamLeaderboards.Size(id);
    }

    /// <summary>
    /// <para>Forgets the details that were set for the next score.</para>
    /// <para>Call it before setting the details of a new score, so that none are left over from the last one.</para>
    /// </summary>
    /// <remarks>
    /// The details of a score are extra numbers that are kept with it. See
    /// <see cref="SetSteamScoreDetail">set steam score detail</see>. They are set one at
    /// a time and stay set after a score has been submitted, so that the same details
    /// can go to more than one leaderboard. That also means that a score with fewer
    /// details than the one before it would carry the old ones along, unless this is
    /// called first.
    /// </remarks>
    /// <example>
    /// Submit a score with two details:
    /// <code>
    /// steam leaderboard 1, "Best Times"
    ///
    /// clear steam score details
    /// set steam score detail 0, 7
    /// set steam score detail 1, 42
    /// submit steam score 1, 1200
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    [FadeBasicCommand("clear steam score details")]
    public static void ClearSteamScoreDetails()
    {
        SteamLeaderboards.ClearDetails();
    }

    /// <summary>
    /// <para>Sets one of the extra numbers that go along with the next score that is submitted.</para>
    /// <para>A score can carry up to 64 of them, numbered <c>0</c> to <c>63</c>.</para>
    /// </summary>
    /// <remarks>
    /// Steam ranks players by one number, the score. The details are whatever else you
    /// want to keep with it: the level that was reached, the character that was played,
    /// how long it took. Steam does not look at them. It keeps them with the score and
    /// hands them back with it, to anyone who loads that score. Read them with
    /// <see cref="GetSteamScoreDetail">steam score detail</see>.
    ///
    /// Set them before <see cref="SubmitSteamScore">submit steam score</see>, and call
    /// <see cref="ClearSteamScoreDetails">clear steam score details</see> first. As many
    /// are sent as the highest number that was set, so setting only detail <c>5</c>
    /// sends six of them, and the ones that were skipped are <c>0</c>.
    ///
    /// Each detail is a whole number. More can be kept by packing several small values
    /// into one number.
    /// </remarks>
    /// <example>
    /// Keep the level and the lives that were left with a score:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    ///
    /// level = 4
    /// lives = 2
    ///
    /// clear steam score details
    /// set steam score detail 0, level
    /// set steam score detail 1, lives
    /// submit steam score 1, 9000
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="index">Which detail to set, from <c>0</c> to <c>63</c>.</param>
    /// <param name="value">The number to keep there.</param>
    /// <seealso cref="ClearSteamScoreDetails">clear steam score details</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="GetSteamScoreDetail">steam score detail</seealso>
    [FadeBasicCommand("set steam score detail")]
    public static void SetSteamScoreDetail(int index, int value)
    {
        SteamLeaderboards.SetDetail(index, value);
    }

    /// <summary>
    /// <para>Starts sending the player's score to a leaderboard.</para>
    /// <para>Steam only keeps it if it is better than the score that the player already has there.</para>
    /// </summary>
    /// <remarks>
    /// A player has one score on a leaderboard, their best. Send every score that a
    /// player makes and let Steam decide. Whether a score is better depends on the
    /// leaderboard: on one where the lowest wins, a lower score replaces a higher one.
    ///
    /// The details that were set with
    /// <see cref="SetSteamScoreDetail">set steam score detail</see> go along with the
    /// score, and are kept only if the score is.
    ///
    /// Sending takes a moment. <see cref="GetSteamSubmitState">steam submit state</see>
    /// says how it is going, and once it is done,
    /// <see cref="GetSteamSubmitImproved">steam submit improved</see> says whether the
    /// score was kept and <see cref="GetSteamSubmitRank">steam submit rank</see> says
    /// where the player now stands.
    ///
    /// Steam limits how often a game may send scores, to about ten in ten minutes for
    /// one leaderboard. Send a score when a game ends, and not as it changes.
    /// </remarks>
    /// <example>
    /// Send a score and say how it went:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// submit steam score 1, 9000
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "sending your score"
    ///
    /// DO
    ///   IF steam submit state(1) = 2
    ///     IF steam submit improved(1)
    ///       set text 1, "a new best. you are number " + str$(steam submit rank(1))
    ///     ELSE
    ///       set text 1, "not your best this time"
    ///     ENDIF
    ///   ENDIF
    ///   IF steam submit state(1) = 3 THEN set text 1, "could not reach steam"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="score">The score to send.</param>
    /// <seealso cref="ReplaceSteamScore">replace steam score</seealso>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    /// <seealso cref="GetSteamSubmitState">steam submit state</seealso>
    /// <seealso cref="GetSteamSubmitImproved">steam submit improved</seealso>
    /// <seealso cref="GetSteamSubmitRank">steam submit rank</seealso>
    [FadeBasicCommand("submit steam score")]
    public static void SubmitSteamScore(int id, int score)
    {
        SteamLeaderboards.Submit(id, score, replace: false);
    }

    /// <summary>
    /// <para>Starts sending the player's score to a leaderboard, and replaces the score that they have there even if it was better.</para>
    /// </summary>
    /// <remarks>
    /// This is for a leaderboard that shows where a player is now, and not the best that
    /// they have ever done: a current streak, a rating that goes up and down. For an
    /// ordinary high score table use
    /// <see cref="SubmitSteamScore">submit steam score</see>, or a bad game wipes out a
    /// good one.
    ///
    /// It is also handy while testing, for putting your own score back to something low.
    ///
    /// Everything else is the same as <see cref="SubmitSteamScore">submit steam score</see>:
    /// the details go along, and <see cref="GetSteamSubmitState">steam submit state</see>
    /// says how it is going.
    /// </remarks>
    /// <example>
    /// Keep a leaderboard of how many games in a row each player has won:
    /// <code>
    /// steam leaderboard 1, "Win Streak"
    /// streak = 0
    ///
    /// DO
    ///   ` space is a win, and return is a loss
    ///   IF new spaceKey()
    ///     streak = streak + 1
    ///     replace steam score 1, streak
    ///   ENDIF
    ///   IF new returnKey()
    ///     streak = 0
    ///     replace steam score 1, streak
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="score">The score to send.</param>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="GetSteamSubmitState">steam submit state</seealso>
    [FadeBasicCommand("replace steam score")]
    public static void ReplaceSteamScore(int id, int score)
    {
        SteamLeaderboards.Submit(id, score, replace: true);
    }

    /// <summary>
    /// <para>Returns how the sending of a score is going.</para>
    /// <para><c>0</c> is nothing sent, <c>1</c> is sending, <c>2</c> is done, and <c>3</c> is failed.</para>
    /// </summary>
    /// <remarks>
    /// This is about the last score that was sent to this leaderboard with
    /// <see cref="SubmitSteamScore">submit steam score</see> or
    /// <see cref="ReplaceSteamScore">replace steam score</see>. Done means that Steam
    /// has answered, and not that the score was kept. Ask
    /// <see cref="GetSteamSubmitImproved">steam submit improved</see> for that.
    ///
    /// Sending fails when the leaderboard could not be found, when Steam is not up, and
    /// when Steam does not answer for a long time. A score that failed to send is not
    /// tried again. Send it again yourself if it matters.
    /// </remarks>
    /// <example>
    /// Wait for a score to be sent before showing the table:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// submit steam score 1, 9000
    ///
    /// loaded = 0
    /// DO
    ///   ` once the score is in, fetch the top ten, which now has it
    ///   IF loaded = 0
    ///     IF steam submit state(1) &gt; 1
    ///       load steam scores 1, 1, 10
    ///       loaded = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns><c>0</c> for nothing sent, <c>1</c> for sending, <c>2</c> for done, <c>3</c> for failed.</returns>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="GetSteamSubmitImproved">steam submit improved</seealso>
    /// <seealso cref="GetSteamSubmitRank">steam submit rank</seealso>
    [FadeBasicCommand("steam submit state")]
    public static int GetSteamSubmitState(int id)
    {
        return SteamLeaderboards.SubmitState(id);
    }

    /// <summary>
    /// <para>Checks whether the last score that was sent became the player's score on the leaderboard.</para>
    /// <para>Returns <c>1</c> if it did, and <c>0</c> if their old score was better, or if sending is not done.</para>
    /// </summary>
    /// <remarks>
    /// Only ask once <see cref="GetSteamSubmitState">steam submit state</see> is <c>2</c>.
    /// Before that the answer is always <c>0</c>.
    ///
    /// A player's first score on a leaderboard always counts as an improvement.
    /// </remarks>
    /// <example>
    /// Congratulate the player on a new best:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// submit steam score 1, 9000
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam submit improved(1) THEN set text 1, "a new personal best"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns><c>1</c> if the score was kept, <c>0</c> if not.</returns>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="GetSteamSubmitState">steam submit state</seealso>
    /// <seealso cref="GetSteamSubmitRank">steam submit rank</seealso>
    [FadeBasicCommand("steam submit improved")]
    public static int GetSteamSubmitImproved(int id)
    {
        return SteamLeaderboards.SubmitImproved(id);
    }

    /// <summary>
    /// <para>Returns where the player stands on a leaderboard, out of everyone, after the last score that was sent.</para>
    /// <para>The best player is number <c>1</c>. Returns <c>0</c> if sending is not done.</para>
    /// </summary>
    /// <remarks>
    /// Only ask once <see cref="GetSteamSubmitState">steam submit state</see> is <c>2</c>.
    /// The rank is the player's standing with their best score, so it is the same as
    /// before when the score that was sent was not an improvement.
    /// </remarks>
    /// <example>
    /// Show the player's rank after a game:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// submit steam score 1, 9000
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam submit state(1) = 2
    ///     set text 1, "you are number " + str$(steam submit rank(1)) + " in the world"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns>The player's rank among everyone, or <c>0</c> if it is not known.</returns>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="GetSteamSubmitState">steam submit state</seealso>
    /// <seealso cref="GetSteamSubmitImproved">steam submit improved</seealso>
    [FadeBasicCommand("steam submit rank")]
    public static int GetSteamSubmitRank(int id)
    {
        return SteamLeaderboards.SubmitRank(id);
    }

    /// <summary>
    /// <para>Starts fetching a run of scores from a leaderboard, counted from the top of everyone's scores.</para>
    /// <para>The best score is rank <c>1</c>. The scores can be read once <see cref="GetSteamScoresState">steam scores state</see> is <c>2</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is the world table. <c>load steam scores 1, 1, 10</c> fetches the top ten, and
    /// <c>load steam scores 1, 11, 10</c> fetches the ten after that.
    ///
    /// A leaderboard holds one set of fetched scores at a time. Fetching again, with this
    /// or with <see cref="LoadSteamFriendScores">load steam friend scores</see> or
    /// <see cref="LoadSteamScoresNearMe">load steam scores near me</see>, throws away
    /// what was there and puts the new scores in its place.
    ///
    /// Fetching takes a moment, and it also waits for the names of the players to
    /// arrive. Read the scores a row at a time, with
    /// <see cref="GetSteamScoreCount">steam score count</see> saying how many rows came
    /// back. Fewer come back than were asked for when the leaderboard runs out.
    /// </remarks>
    /// <example>
    /// Show the top five scores in the world:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 5
    ///
    /// font 1, "font"
    /// FOR row = 0 TO 4
    ///   text row + 1, 470, 100 + row * 30, 1, ""
    /// NEXT
    ///
    /// DO
    ///   IF steam scores state(1) = 2
    ///     FOR row = 0 TO steam score count(1) - 1
    ///       line$ = str$(steam score rank(1, row)) + ". " + steam score name$(1, row)
    ///       set text row + 1, line$ + "  " + str$(steam score(1, row))
    ///     NEXT
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="firstRank">The rank of the first score to fetch. The best score is rank <c>1</c>.</param>
    /// <param name="count">How many scores to fetch.</param>
    /// <seealso cref="LoadSteamFriendScores">load steam friend scores</seealso>
    /// <seealso cref="LoadSteamScoresNearMe">load steam scores near me</seealso>
    /// <seealso cref="GetSteamScoresState">steam scores state</seealso>
    /// <seealso cref="GetSteamScoreCount">steam score count</seealso>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    [FadeBasicCommand("load steam scores")]
    public static void LoadSteamScores(int id, int firstRank, int count)
    {
        SteamLeaderboards.Load(id, SteamLeaderboards.KindGlobal, firstRank, count);
    }

    /// <summary>
    /// <para>Starts fetching the scores of the player and of every Steam friend of theirs who is on a leaderboard.</para>
    /// <para>The scores can be read once <see cref="GetSteamScoresState">steam scores state</see> is <c>2</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is the table that most players care about. It has the player's own score in
    /// it, if they have one, and it comes back best first.
    ///
    /// All of them are fetched at once, however many friends there are, so check
    /// <see cref="GetSteamScoreCount">steam score count</see> and show as many as fit.
    /// The rank of each row, from <see cref="GetSteamScoreRank">steam score rank</see>,
    /// is still the rank among everyone in the world. To number friends 1, 2, 3, use
    /// the row.
    ///
    /// A player with no friends on the leaderboard and no score of their own gets back
    /// no rows. That is not a failure: the state is <c>2</c> and the count is <c>0</c>.
    /// </remarks>
    /// <example>
    /// Show how the player's friends are doing:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam friend scores 1
    ///
    /// font 1, "font"
    /// FOR row = 0 TO 4
    ///   text row + 1, 470, 100 + row * 30, 1, ""
    /// NEXT
    ///
    /// DO
    ///   IF steam scores state(1) = 2
    ///     shown = min(steam score count(1), 5)
    ///     FOR row = 0 TO shown - 1
    ///       set text row + 1, str$(row + 1) + ". " + steam score name$(1, row) + "  " + str$(steam score(1, row))
    ///     NEXT
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    /// <seealso cref="LoadSteamScoresNearMe">load steam scores near me</seealso>
    /// <seealso cref="GetSteamScoresState">steam scores state</seealso>
    /// <seealso cref="GetSteamScoreCount">steam score count</seealso>
    /// <seealso cref="IsSteamScoreMe">steam score is me</seealso>
    [FadeBasicCommand("load steam friend scores")]
    public static void LoadSteamFriendScores(int id)
    {
        SteamLeaderboards.Load(id, SteamLeaderboards.KindFriends, 0, 0);
    }

    /// <summary>
    /// <para>Starts fetching the scores around the player's own, out of everyone's.</para>
    /// <para>The scores can be read once <see cref="GetSteamScoresState">steam scores state</see> is <c>2</c>.</para>
    /// </summary>
    /// <remarks>
    /// This shows a player where they stand: the few players just ahead of them, their
    /// own score, and the few just behind. <c>load steam scores near me 1, 3, 3</c>
    /// fetches seven rows, with the player in the middle.
    ///
    /// When the player is near the top or the bottom, Steam shifts the window so that as
    /// many rows come back as were asked for, and the player is then not in the middle.
    /// Find their row with <see cref="IsSteamScoreMe">steam score is me</see>.
    ///
    /// A player who has no score on the leaderboard gets back no rows.
    /// </remarks>
    /// <example>
    /// Show the player and the two players on either side of them:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores near me 1, 2, 2
    ///
    /// font 1, "font"
    /// FOR row = 0 TO 4
    ///   text row + 1, 470, 100 + row * 30, 1, ""
    /// NEXT
    ///
    /// DO
    ///   IF steam scores state(1) = 2
    ///     FOR row = 0 TO steam score count(1) - 1
    ///       line$ = str$(steam score rank(1, row)) + ". " + steam score name$(1, row)
    ///       ` point out which row is the player
    ///       IF steam score is me(1, row) THEN line$ = line$ + "  (you)"
    ///       set text row + 1, line$
    ///     NEXT
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="before">How many scores to fetch that are better than the player's.</param>
    /// <param name="after">How many scores to fetch that are worse than the player's.</param>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    /// <seealso cref="LoadSteamFriendScores">load steam friend scores</seealso>
    /// <seealso cref="GetSteamScoresState">steam scores state</seealso>
    /// <seealso cref="IsSteamScoreMe">steam score is me</seealso>
    [FadeBasicCommand("load steam scores near me")]
    public static void LoadSteamScoresNearMe(int id, int before, int after)
    {
        SteamLeaderboards.Load(id, SteamLeaderboards.KindNearMe, before, after);
    }

    /// <summary>
    /// <para>Returns how the fetching of scores is going.</para>
    /// <para><c>0</c> is nothing fetched, <c>1</c> is fetching, <c>2</c> is done, and <c>3</c> is failed.</para>
    /// </summary>
    /// <remarks>
    /// This is about the last fetch that was started on this leaderboard, by
    /// <see cref="LoadSteamScores">load steam scores</see>,
    /// <see cref="LoadSteamFriendScores">load steam friend scores</see> or
    /// <see cref="LoadSteamScoresNearMe">load steam scores near me</see>. While it is
    /// <c>1</c> there are no rows to read, so this is the moment to show "loading".
    ///
    /// Done with no rows is not a failure. It means that nobody is on that part of the
    /// leaderboard. Failed means that the leaderboard could not be found, that Steam is
    /// not up, or that Steam did not answer for a long time.
    /// </remarks>
    /// <example>
    /// Show a message for each thing that can happen:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam friend scores 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   state = steam scores state(1)
    ///   IF state = 1 THEN set text 1, "loading"
    ///   IF state = 3 THEN set text 1, "could not reach steam"
    ///   IF state = 2
    ///     IF steam score count(1) = 0
    ///       set text 1, "none of your friends have played yet"
    ///     ELSE
    ///       set text 1, str$(steam score count(1)) + " scores"
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns><c>0</c> for nothing fetched, <c>1</c> for fetching, <c>2</c> for done, <c>3</c> for failed.</returns>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    /// <seealso cref="GetSteamScoreCount">steam score count</seealso>
    /// <seealso cref="GetSteamLeaderboardState">steam leaderboard state</seealso>
    [FadeBasicCommand("steam scores state")]
    public static int GetSteamScoresState(int id)
    {
        return SteamLeaderboards.ScoresState(id);
    }

    /// <summary>
    /// <para>Returns how many scores the last fetch brought back.</para>
    /// <para>The rows are numbered from <c>0</c> to one less than this, best first.</para>
    /// </summary>
    /// <remarks>
    /// It is <c>0</c> while a fetch is still going, and also when the fetch came back
    /// empty. <see cref="GetSteamScoresState">steam scores state</see> tells those apart.
    ///
    /// It can be less than what was asked for, when the leaderboard does not have that
    /// many scores.
    /// </remarks>
    /// <example>
    /// Say how many of the top ten places are taken:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 10
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   set text 1, str$(steam score count(1)) + " of 10 places are taken"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <returns>How many rows there are to read.</returns>
    /// <seealso cref="LoadSteamScores">load steam scores</seealso>
    /// <seealso cref="GetSteamScoresState">steam scores state</seealso>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    [FadeBasicCommand("steam score count")]
    public static int GetSteamScoreCount(int id)
    {
        return SteamLeaderboards.RowCount(id);
    }

    /// <summary>
    /// <para>Returns the rank, among everyone in the world, of one row of the fetched scores.</para>
    /// <para>The best score is rank <c>1</c>. Returns <c>0</c> for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// The rank is always the place on the whole leaderboard, even in a table of friends.
    /// A friend in row <c>0</c> of a friends table may be rank 5000 in the world.
    /// </remarks>
    /// <example>
    /// Show the world rank of the best of the player's friends:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam friend scores 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score count(1) &gt; 0
    ///     set text 1, steam score name$(1, 0) + " is number " + str$(steam score rank(1, 0)) + " in the world"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns>The rank of that score on the whole leaderboard.</returns>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    /// <seealso cref="GetSteamScoreName">steam score name$</seealso>
    /// <seealso cref="GetSteamScoreCount">steam score count</seealso>
    [FadeBasicCommand("steam score rank")]
    public static int GetSteamScoreRank(int id, int row)
    {
        return SteamLeaderboards.RowRank(id, row);
    }

    /// <summary>
    /// <para>Returns the score in one row of the fetched scores.</para>
    /// <para>Returns <c>0</c> for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// This is the number that was given to
    /// <see cref="SubmitSteamScore">submit steam score</see>, and the one that Steam
    /// ranks by. The other things about the row come from
    /// <see cref="GetSteamScoreName">steam score name$</see>,
    /// <see cref="GetSteamScoreRank">steam score rank</see> and
    /// <see cref="GetSteamScoreDetail">steam score detail</see>.
    /// </remarks>
    /// <example>
    /// Show the best score in the world:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score count(1) &gt; 0
    ///     set text 1, "the score to beat is " + str$(steam score(1, 0))
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns>The score in that row.</returns>
    /// <seealso cref="GetSteamScoreName">steam score name$</seealso>
    /// <seealso cref="GetSteamScoreRank">steam score rank</seealso>
    /// <seealso cref="GetSteamScoreDetail">steam score detail</seealso>
    /// <seealso cref="GetSteamScoreCount">steam score count</seealso>
    [FadeBasicCommand("steam score")]
    public static int GetSteamScore(int id, int row)
    {
        return SteamLeaderboards.RowScore(id, row);
    }

    /// <summary>
    /// <para>Returns the Steam name of the player in one row of the fetched scores.</para>
    /// <para>Returns an empty string for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// The name is what the player went by when the scores were fetched. A Steam name can
    /// hold any character, in any alphabet, and a font only draws the ones that it has,
    /// so a name may show with gaps in it. Names can also be long. Leave room, or cut
    /// them short with <c>left$</c>.
    /// </remarks>
    /// <example>
    /// Show who holds the top score:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score count(1) &gt; 0
    ///     ` at most 16 letters of the name, so that it fits
    ///     set text 1, "the champion is " + left$(steam score name$(1, 0), 16)
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns>The name of the player in that row.</returns>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    /// <seealso cref="GetSteamScoreUserId">steam score user id$</seealso>
    /// <seealso cref="IsSteamScoreMe">steam score is me</seealso>
    [FadeBasicCommand("steam score name$")]
    public static string GetSteamScoreName(int id, int row)
    {
        return SteamLeaderboards.RowName(id, row);
    }

    /// <summary>
    /// <para>Returns the Steam id of the player in one row of the fetched scores, as text.</para>
    /// <para>Returns an empty string for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// Names change and two players can share one. The id is what says for certain who
    /// a score belongs to. It is the same kind of id that
    /// <see cref="GetSteamUserId">steam user id$</see> returns for the player themselves.
    /// To find the player's own row there is a shortcut:
    /// <see cref="IsSteamScoreMe">steam score is me</see>.
    /// </remarks>
    /// <example>
    /// Print the id of whoever is in first place:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// printed = 0
    /// DO
    ///   IF printed = 0
    ///     IF steam score count(1) &gt; 0
    ///       print "first place is " + steam score user id$(1, 0)
    ///       printed = 1
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns>The Steam id of the player in that row.</returns>
    /// <seealso cref="GetSteamScoreName">steam score name$</seealso>
    /// <seealso cref="IsSteamScoreMe">steam score is me</seealso>
    /// <seealso cref="GetSteamUserId">steam user id$</seealso>
    [FadeBasicCommand("steam score user id$")]
    public static string GetSteamScoreUserId(int id, int row)
    {
        return SteamLeaderboards.RowUserId(id, row);
    }

    /// <summary>
    /// <para>Checks whether one row of the fetched scores is the player's own.</para>
    /// <para>Returns <c>1</c> if it is, and <c>0</c> if it is somebody else's, or if the row is not there.</para>
    /// </summary>
    /// <remarks>
    /// Use it to light up the player's row in a table, which is what makes a leaderboard
    /// readable at a glance.
    /// </remarks>
    /// <example>
    /// Find which row of a friends table the player is in:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam friend scores 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   FOR row = 0 TO steam score count(1) - 1
    ///     IF steam score is me(1, row)
    ///       set text 1, "you are number " + str$(row + 1) + " among your friends"
    ///     ENDIF
    ///   NEXT
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns><c>1</c> if the row is the player's, <c>0</c> if not.</returns>
    /// <seealso cref="GetSteamScoreName">steam score name$</seealso>
    /// <seealso cref="GetSteamScoreUserId">steam score user id$</seealso>
    /// <seealso cref="LoadSteamScoresNearMe">load steam scores near me</seealso>
    [FadeBasicCommand("steam score is me")]
    public static int IsSteamScoreMe(int id, int row)
    {
        return SteamLeaderboards.RowIsMe(id, row);
    }

    /// <summary>
    /// <para>Returns how many extra numbers came with the score in one row.</para>
    /// <para>Returns <c>0</c> for a score that has none, and for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// The details are the numbers that were set with
    /// <see cref="SetSteamScoreDetail">set steam score detail</see> when the score was
    /// submitted. Different scores on one leaderboard can have different numbers of
    /// them, for example when an older version of the game sent fewer. Check this before
    /// trusting what <see cref="GetSteamScoreDetail">steam score detail</see> returns.
    /// </remarks>
    /// <example>
    /// Only show the level for scores that came with one:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score detail count(1, 0) &gt; 0
    ///     set text 1, "reached level " + str$(steam score detail(1, 0, 0))
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns>How many details that score has, from <c>0</c> to <c>64</c>.</returns>
    /// <seealso cref="GetSteamScoreDetail">steam score detail</seealso>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    [FadeBasicCommand("steam score detail count")]
    public static int GetSteamScoreDetailCount(int id, int row)
    {
        return SteamLeaderboards.RowDetailCount(id, row);
    }

    /// <summary>
    /// <para>Returns one of the extra numbers that came with the score in one row.</para>
    /// <para>Returns <c>0</c> for a detail that the score does not have.</para>
    /// </summary>
    /// <remarks>
    /// This is the other end of
    /// <see cref="SetSteamScoreDetail">set steam score detail</see>: what was set as
    /// detail <c>3</c> when the score was submitted is read as detail <c>3</c> here, by
    /// any player who fetches that score.
    ///
    /// A detail that was never set reads as <c>0</c>, the same as one that was set to
    /// <c>0</c>. <see cref="GetSteamScoreDetailCount">steam score detail count</see>
    /// tells them apart.
    ///
    /// The numbers were written by somebody else's copy of the game, and maybe not by
    /// the game at all. Check that they make sense before using them for anything that
    /// could go wrong, like the index of an array.
    /// </remarks>
    /// <example>
    /// Show the level and the lives that came with the top score:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score detail count(1, 0) &gt; 1
    ///     level = steam score detail(1, 0, 0)
    ///     lives = steam score detail(1, 0, 1)
    ///     set text 1, "level " + str$(level) + " with " + str$(lives) + " lives left"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <param name="index">Which detail, from <c>0</c> to <c>63</c>.</param>
    /// <returns>The detail, or <c>0</c> if the score does not have it.</returns>
    /// <seealso cref="GetSteamScoreDetailCount">steam score detail count</seealso>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    [FadeBasicCommand("steam score detail")]
    public static int GetSteamScoreDetail(int id, int row, int index)
    {
        return SteamLeaderboards.RowDetail(id, row, index);
    }

    /// <summary>
    /// <para>Sets a run of bits in the details of the next score that is submitted.</para>
    /// <para>This is for packing many small numbers into the 64 details that a score can carry.</para>
    /// </summary>
    /// <remarks>
    /// A detail is a whole number, which is 32 bits, and a score has 64 of them. That is
    /// 2048 bits, numbered from <c>0</c>. Detail <c>0</c> is bits <c>0</c> to <c>31</c>,
    /// detail <c>1</c> is bits <c>32</c> to <c>63</c>, and so on. This writes a number
    /// into any run of them, and the run may go across from one detail into the next.
    ///
    /// A number that is never more than 31 fits in 5 bits, so a hundred of them fit in
    /// 500 bits, which is 16 details and not 100. You keep track of where each number
    /// starts. Read one back with <see cref="GetSteamScoreBits">steam score bits</see>,
    /// with the same start and the same count.
    ///
    /// The number has to fit in the bits that it is given: <c>bitCount</c> bits hold
    /// from <c>0</c> up to one less than 2 to the power of <c>bitCount</c>. Anything
    /// higher has its top cut off. Bits and whole details can be mixed in one score, as
    /// long as they do not land on each other. Call
    /// <see cref="ClearSteamScoreDetails">clear steam score details</see> first, the same
    /// as with <see cref="SetSteamScoreDetail">set steam score detail</see>.
    /// </remarks>
    /// <example>
    /// Pack a level (up to 31), lives (up to 7) and a time in seconds (up to 4095) into one detail:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    ///
    /// level = 12
    /// lives = 3
    /// seconds = 754
    ///
    /// clear steam score details
    /// set steam score bits 0, 5, level
    /// set steam score bits 5, 3, lives
    /// set steam score bits 8, 12, seconds
    /// submit steam score 1, 9000
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="bitIndex">The first bit to set, from <c>0</c> to <c>2047</c>.</param>
    /// <param name="bitCount">How many bits to set, from <c>1</c> to <c>32</c>.</param>
    /// <param name="value">The number to write into them.</param>
    /// <seealso cref="GetSteamScoreBits">steam score bits</seealso>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    /// <seealso cref="ClearSteamScoreDetails">clear steam score details</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    [FadeBasicCommand("set steam score bits")]
    public static void SetSteamScoreBits(int bitIndex, int bitCount, int value)
    {
        SteamLeaderboards.SetBits(bitIndex, bitCount, value);
    }

    /// <summary>
    /// <para>Returns the number that is in a run of bits of the details of the score in one row.</para>
    /// <para>Returns <c>0</c> for bits that the score did not come with.</para>
    /// </summary>
    /// <remarks>
    /// This is the other end of <see cref="SetSteamScoreBits">set steam score bits</see>.
    /// Give it the same first bit and the same count that the number was written with.
    /// The bits are numbered across all of the details: detail <c>0</c> is bits
    /// <c>0</c> to <c>31</c>, detail <c>1</c> is bits <c>32</c> to <c>63</c>, and so on.
    ///
    /// The numbers were written by somebody else's copy of the game, and maybe not by
    /// the game at all. Check that they make sense before using them for anything that
    /// could go wrong, like the index of an array.
    /// </remarks>
    /// <example>
    /// Read back a level, lives and a time that were packed into one detail:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score count(1) &gt; 0
    ///     level = steam score bits(1, 0, 0, 5)
    ///     lives = steam score bits(1, 0, 5, 3)
    ///     seconds = steam score bits(1, 0, 8, 12)
    ///     set text 1, "level " + str$(level) + ", " + str$(lives) + " lives, " + str$(seconds) + " seconds"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <param name="bitIndex">The first bit to read, from <c>0</c> to <c>2047</c>.</param>
    /// <param name="bitCount">How many bits to read, from <c>1</c> to <c>32</c>.</param>
    /// <returns>The number in those bits.</returns>
    /// <seealso cref="SetSteamScoreBits">set steam score bits</seealso>
    /// <seealso cref="GetSteamScoreDetail">steam score detail</seealso>
    [FadeBasicCommand("steam score bits")]
    public static int GetSteamScoreBits(int id, int row, int bitIndex, int bitCount)
    {
        return SteamLeaderboards.RowBits(id, row, bitIndex, bitCount);
    }

    /// <summary>
    /// <para>Says that every score on a leaderboard carries a check in one of its details, which shows whether the score was made by the game.</para>
    /// <para>Scores that are submitted get the check written in, and scores that are fetched can be asked whether theirs is right.</para>
    /// </summary>
    /// <remarks>
    /// Anyone can send any number to a Steam leaderboard with a tool, and the top of a
    /// leaderboard fills up with scores that nobody played for. The check is how a game
    /// tells those apart. It is a number that is worked out from the score, its details,
    /// the leaderboard and the player, together with a secret that the game was built
    /// with. A tool that does not have the secret cannot work it out.
    ///
    /// Pick a detail that nothing else uses and say so once, when the game starts.
    /// After that, <see cref="SubmitSteamScore">submit steam score</see> fills that
    /// detail in by itself, and <see cref="IsSteamScoreChecked">steam score checked</see>
    /// says which of the fetched scores are good. The game cannot take the bad ones off
    /// of Steam. It can only leave them out of what it shows.
    ///
    /// The check does not stop somebody who takes the game apart to find the secret. It
    /// stops everybody else. A score that was submitted before the game used a check, or
    /// by a build with a different secret, fails it.
    ///
    /// Give <c>-1</c> to stop using a check on a leaderboard.
    /// </remarks>
    /// <example>
    /// Show the top scores, leaving out the ones that fail the check:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// set steam leaderboard check 1, 0
    /// load steam scores 1, 1, 20
    ///
    /// font 1, "font"
    /// FOR i = 0 TO 4
    ///   text i + 1, 470, 100 + i * 30, 1, ""
    /// NEXT
    ///
    /// DO
    ///   IF steam scores state(1) = 2
    ///     shown = 0
    ///     FOR row = 0 TO steam score count(1) - 1
    ///       IF steam score checked(1, row)
    ///         IF shown &lt; 5
    ///           set text shown + 1, str$(shown + 1) + ". " + steam score name$(1, row)
    ///           shown = shown + 1
    ///         ENDIF
    ///       ENDIF
    ///     NEXT
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="detailIndex">Which detail holds the check, from <c>0</c> to <c>63</c>, or <c>-1</c> for none.</param>
    /// <seealso cref="IsSteamScoreChecked">steam score checked</seealso>
    /// <seealso cref="SubmitSteamScore">submit steam score</seealso>
    /// <seealso cref="SetSteamScoreDetail">set steam score detail</seealso>
    [FadeBasicCommand("set steam leaderboard check")]
    public static void SetSteamLeaderboardCheck(int id, int detailIndex)
    {
        SteamLeaderboards.SetCheck(id, detailIndex);
    }

    /// <summary>
    /// <para>Checks whether the score in one row was made by the game, going by its check.</para>
    /// <para>Returns <c>1</c> if the check is right, and <c>0</c> if it is wrong, or if the leaderboard has no check.</para>
    /// </summary>
    /// <remarks>
    /// The leaderboard has to have been given a check with
    /// <see cref="SetSteamLeaderboardCheck">set steam leaderboard check</see> before the
    /// scores were fetched. A score fails when any part of it was changed after the game
    /// made it, when it was copied from another player or another leaderboard, and when
    /// it was never made by the game in the first place.
    ///
    /// The rank that Steam gives a score still counts the bad scores above it. To number
    /// a table, count the rows that pass.
    /// </remarks>
    /// <example>
    /// Count how many of the top twenty scores are good:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// set steam leaderboard check 1, 0
    /// load steam scores 1, 1, 20
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   good = 0
    ///   FOR row = 0 TO steam score count(1) - 1
    ///     IF steam score checked(1, row) THEN good = good + 1
    ///   NEXT
    ///   set text 1, str$(good) + " of " + str$(steam score count(1)) + " scores are good"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <returns><c>1</c> if the score passes its check, <c>0</c> if not.</returns>
    /// <seealso cref="SetSteamLeaderboardCheck">set steam leaderboard check</seealso>
    /// <seealso cref="GetSteamScore">steam score</seealso>
    [FadeBasicCommand("steam score checked")]
    public static int IsSteamScoreChecked(int id, int row)
    {
        return SteamLeaderboards.RowChecked(id, row);
    }

    /// <summary>
    /// <para>Returns the Steam name of the player in one row, with only the plain letters, digits and marks that every font has, cut to a length.</para>
    /// <para>Returns an empty string for a row that is not there.</para>
    /// </summary>
    /// <remarks>
    /// <see cref="GetSteamScoreName">steam score name$</see> returns a name exactly as
    /// the player wrote it, and a Steam name can have any character in it. A font that
    /// was made for a game mostly has the English letters and not much else. This is the
    /// name with everything else taken out, so that it is safe to draw with any font.
    ///
    /// A name that has nothing plain in it comes back as a question mark. Give
    /// <c>0</c> for the length to keep all of it.
    /// </remarks>
    /// <example>
    /// Show who holds the top score, in at most 12 letters:
    /// <code>
    /// steam leaderboard 1, "High Scores"
    /// load steam scores 1, 1, 1
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam score count(1) &gt; 0
    ///     set text 1, "the champion is " + steam score plain name$(1, 0, 12)
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="id">The id of the leaderboard.</param>
    /// <param name="row">Which row, from <c>0</c>.</param>
    /// <param name="maxLetters">The most letters to return, or <c>0</c> for no limit.</param>
    /// <returns>The plain name of the player in that row.</returns>
    /// <seealso cref="GetSteamScoreName">steam score name$</seealso>
    /// <seealso cref="IsSteamScoreMe">steam score is me</seealso>
    [FadeBasicCommand("steam score plain name$")]
    public static string GetSteamScorePlainName(int id, int row, int maxLetters)
    {
        return SteamLeaderboards.RowPlainName(id, row, maxLetters);
    }
}
