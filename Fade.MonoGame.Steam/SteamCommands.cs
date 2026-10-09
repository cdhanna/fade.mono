using FadeBasic.SourceGenerators;
using Steamworks;

namespace Fade.MonoGame.Steam;

/// <summary>
/// The Steam commands: who is playing, achievements, stats, leaderboards, rich presence
/// and the overlay.
///
/// None of them needs Steam to be there. When it is not, the ones that return something
/// return 0 or an empty string, and the rest do nothing. A game can call them freely, and
/// ask <c>steam available()</c> only where it wants to show something different.
///
/// ADDING THIS GROUP TO A GAME TAKES TWO EDITS, and the second is the one people miss:
///   1. &lt;FadeCommand Include="Fade.MonoGame.Steam"
///                     FullName="Fade.MonoGame.Steam.FadeSteamCommands" /&gt;
///      in the game's csproj, which is what the build-time compiler sees.
///   2. the CommandCollection in the game's Program.cs, which is what the Debug runtime
///      compile sees.
/// And a <see cref="SteamSystem"/> has to be registered, or Steam is never started.
/// </summary>
public partial class FadeSteamCommands
{
    /// <summary>
    /// <para>Checks whether Steam is up and the game is talking to it.</para>
    /// <para>Returns <c>1</c> if it is, and <c>0</c> if the game is running without Steam.</para>
    /// </summary>
    /// <remarks>
    /// A game can be started without Steam: by running its executable directly, on a
    /// machine where Steam is closed, or in the editor on a machine that does not own the
    /// game. Nothing breaks when that happens. Every other Steam command quietly does
    /// nothing, and the ones that return a value return <c>0</c> or an empty string.
    ///
    /// So you do not have to wrap every Steam command in a check. Use this where the
    /// game should look different without Steam, like hiding the online leaderboard tab,
    /// and use <see cref="GetSteamError">steam error$</see> to find out why Steam is missing.
    ///
    /// The answer does not change while the game runs. Steam is started once, before the
    /// first frame.
    /// </remarks>
    /// <example>
    /// Say hello to the player by their Steam name, when there is one:
    /// <code>
    /// font 1, "font"
    /// IF steam available()
    ///   text 1, 470, 200, 1, "hello, " + steam user name$()
    /// ELSE
    ///   text 1, 470, 200, 1, "playing offline"
    /// ENDIF
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if Steam is up, <c>0</c> if not.</returns>
    /// <seealso cref="GetSteamError">steam error$</seealso>
    /// <seealso cref="GetSteamUserName">steam user name$</seealso>
    [FadeBasicCommand("steam available")]
    public static int IsSteamAvailable()
    {
        return SteamSystem.Available ? 1 : 0;
    }

    /// <summary>
    /// <para>Returns the reason that Steam is not up, as a sentence.</para>
    /// <para>Returns an empty string when Steam is up.</para>
    /// </summary>
    /// <remarks>
    /// This is for you, not for the player. The usual reasons are that the Steam client is
    /// not running, that the account that is logged in does not own the game, or that the
    /// game does not know which Steam game it is. A game that Steam starts is told which
    /// one it is. A build that you start yourself needs a file called
    /// <c>steam_appid.txt</c> next to its executable, with the app id in it.
    ///
    /// The same sentence is printed to the console when the game starts.
    /// </remarks>
    /// <example>
    /// Show why Steam is missing while you are developing:
    /// <code>
    /// font 1, "font"
    /// IF steam available()
    ///   text 1, 470, 200, 1, "steam is up"
    /// ELSE
    ///   text 1, 470, 200, 1, steam error$()
    /// ENDIF
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>Why Steam is not up, or an empty string if it is.</returns>
    /// <seealso cref="IsSteamAvailable">steam available</seealso>
    [FadeBasicCommand("steam error$")]
    public static string GetSteamError()
    {
        return SteamSystem.InitError;
    }

    /// <summary>
    /// <para>Returns the Steam app id that the game is running as.</para>
    /// <para>Returns <c>0</c> when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// Every game on Steam has an app id, and it is the number in the address of its
    /// store page. This is mostly useful for checking that a test build is running as
    /// the game that you think it is.
    /// </remarks>
    /// <example>
    /// Print the app id when the game starts:
    /// <code>
    /// print "running as steam app " + str$(steam app id())
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The app id, or <c>0</c> without Steam.</returns>
    /// <seealso cref="IsSteamAvailable">steam available</seealso>
    [FadeBasicCommand("steam app id")]
    public static int GetSteamAppId()
    {
        if (!SteamSystem.Available) return 0;
        return (int)SteamClient.AppId.Value;
    }

    /// <summary>
    /// <para>Returns the name that the player goes by on Steam.</para>
    /// <para>Returns an empty string when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// This is the name that their friends see, and they can change it whenever they
    /// like, so do not use it to tell players apart. Use
    /// <see cref="GetSteamUserId">steam user id$</see> for that.
    ///
    /// A Steam name can hold any character at all, in any alphabet, and emoji too. A
    /// font only draws the characters that it has, so a name may show with gaps in it.
    /// </remarks>
    /// <example>
    /// Show the player's name:
    /// <code>
    /// font 1, "font"
    /// text 1, 470, 200, 1, "player: " + steam user name$()
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The player's Steam name, or an empty string without Steam.</returns>
    /// <seealso cref="GetSteamUserId">steam user id$</seealso>
    /// <seealso cref="IsSteamAvailable">steam available</seealso>
    [FadeBasicCommand("steam user name$")]
    public static string GetSteamUserName()
    {
        if (!SteamSystem.Available) return "";
        return SteamClient.Name ?? "";
    }

    /// <summary>
    /// <para>Returns the player's Steam id, as text.</para>
    /// <para>Returns an empty string when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// A Steam id is the number that Steam knows an account by. It never changes, and no
    /// two accounts share one, so it is the right thing to tell players apart with. It is
    /// 17 digits long, which is too big for a whole number in Fade, and that is why it
    /// comes back as text. Compare two of them the way you compare any two strings.
    /// </remarks>
    /// <example>
    /// Print the id of whoever is playing:
    /// <code>
    /// print "steam id " + steam user id$()
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The player's Steam id, or an empty string without Steam.</returns>
    /// <seealso cref="GetSteamUserName">steam user name$</seealso>
    /// <seealso cref="GetSteamScoreUserId">steam score user id$</seealso>
    [FadeBasicCommand("steam user id$")]
    public static string GetSteamUserId()
    {
        if (!SteamSystem.Available) return "";
        return SteamClient.SteamId.Value.ToString();
    }

    /// <summary>
    /// <para>Returns the language that the player has picked for the game in Steam, like <c>english</c> or <c>german</c>.</para>
    /// <para>Returns an empty string when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// The name is in lower case and in English, whatever the language is:
    /// <c>french</c>, <c>japanese</c>, <c>brazilian</c>. A player picks from the languages
    /// that the game's store page says it has, so a game that lists only English always
    /// gets <c>english</c>.
    /// </remarks>
    /// <example>
    /// Pick a greeting by language:
    /// <code>
    /// greeting$ = "hello"
    /// IF steam language$() = "french" THEN greeting$ = "bonjour"
    /// IF steam language$() = "german" THEN greeting$ = "hallo"
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, greeting$
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The name of the language, or an empty string without Steam.</returns>
    /// <seealso cref="IsSteamAvailable">steam available</seealso>
    [FadeBasicCommand("steam language$")]
    public static string GetSteamLanguage()
    {
        if (!SteamSystem.Available) return "";
        return SteamApps.GameLanguage ?? "";
    }

    /// <summary>
    /// <para>Checks whether the game is running on a Steam Deck.</para>
    /// <para>Returns <c>1</c> if it is, and <c>0</c> if not, or when Steam is not up.</para>
    /// </summary>
    /// <remarks>
    /// A Deck has a small screen, no keyboard, and its own buttons. A game can use this
    /// to start in fullscreen, to make its text bigger, or to show pad buttons in its
    /// hints from the first frame instead of waiting for a button to be pressed.
    /// </remarks>
    /// <example>
    /// Go fullscreen on a Deck:
    /// <code>
    /// IF is steam deck() THEN set fullscreen 1
    ///
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on a Steam Deck, <c>0</c> anywhere else.</returns>
    /// <seealso cref="IsSteamAvailable">steam available</seealso>
    [FadeBasicCommand("is steam deck")]
    public static int IsSteamDeck()
    {
        if (!SteamSystem.Available) return 0;
        return SteamUtils.IsRunningOnSteamDeck ? 1 : 0;
    }

    /// <summary>
    /// <para>Checks whether the Steam overlay can be opened over the game.</para>
    /// <para>Returns <c>1</c> if it can, and <c>0</c> if not.</para>
    /// </summary>
    /// <remarks>
    /// The overlay is the layer that Steam draws over a game, with the friends list and
    /// the browser in it. A player can turn it off, and it is also off for a game that
    /// was not started by Steam. It takes a moment to hook into the game after the game
    /// starts, so this can be <c>0</c> for the first few frames and <c>1</c> after that.
    ///
    /// <see cref="OpenSteamOverlay">open steam overlay</see> does nothing while this is <c>0</c>.
    /// </remarks>
    /// <example>
    /// Only offer a button for the overlay when it will work:
    /// <code>
    /// font 1, "font"
    /// text 1, 470, 200, 1, ""
    ///
    /// DO
    ///   IF steam overlay enabled()
    ///     set text 1, "press space for your friends list"
    ///     IF new spaceKey() THEN open steam overlay "friends"
    ///   ELSE
    ///     set text 1, ""
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if the overlay can be opened, <c>0</c> if not.</returns>
    /// <seealso cref="OpenSteamOverlay">open steam overlay</seealso>
    /// <seealso cref="OpenSteamOverlayUrl">open steam overlay url</seealso>
    [FadeBasicCommand("steam overlay enabled")]
    public static int IsSteamOverlayEnabled()
    {
        if (!SteamSystem.Available) return 0;
        return SteamUtils.IsOverlayEnabled ? 1 : 0;
    }

    /// <summary>
    /// <para>Opens the Steam overlay over the game, on the page that you name.</para>
    /// </summary>
    /// <remarks>
    /// The pages are <c>friends</c>, <c>community</c>, <c>players</c>, <c>settings</c>,
    /// <c>officialgamegroup</c>, <c>stats</c> and <c>achievements</c>.
    ///
    /// The game keeps running underneath the overlay, and it still gets no keys while the
    /// overlay is up. If the game should pause, pause it yourself before you call this.
    ///
    /// Nothing happens when the overlay is off. See
    /// <see cref="IsSteamOverlayEnabled">steam overlay enabled</see>.
    /// </remarks>
    /// <example>
    /// Open the achievements page with a key:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN open steam overlay "achievements"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="page">Which page to open, like <c>friends</c> or <c>achievements</c>.</param>
    /// <seealso cref="IsSteamOverlayEnabled">steam overlay enabled</seealso>
    /// <seealso cref="OpenSteamOverlayUrl">open steam overlay url</seealso>
    [FadeBasicCommand("open steam overlay")]
    public static void OpenSteamOverlay(string page)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(page)) return;
        SteamFriends.OpenOverlay(page);
    }

    /// <summary>
    /// <para>Opens the Steam overlay's browser over the game, on a web page.</para>
    /// </summary>
    /// <remarks>
    /// This is how a game sends a player to its website, its patch notes or its
    /// community page without them leaving the game. Give the whole address, starting
    /// with <c>https://</c>.
    ///
    /// Nothing happens when the overlay is off. See
    /// <see cref="IsSteamOverlayEnabled">steam overlay enabled</see>.
    /// </remarks>
    /// <example>
    /// Open a web page with a key:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN open steam overlay url "https://store.steampowered.com"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="url">The address of the page, starting with <c>https://</c>.</param>
    /// <seealso cref="IsSteamOverlayEnabled">steam overlay enabled</seealso>
    /// <seealso cref="OpenSteamOverlay">open steam overlay</seealso>
    [FadeBasicCommand("open steam overlay url")]
    public static void OpenSteamOverlayUrl(string url)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(url)) return;
        SteamFriends.OpenWebOverlay(url);
    }

    /// <summary>
    /// <para>Sets one piece of the player's rich presence, which is what their friends see them doing in the game.</para>
    /// </summary>
    /// <remarks>
    /// Rich presence is the line under a player's name in the friends list: "Round 12",
    /// "In the menu". It is made of keys, each with a value. The one that most games
    /// want is the key <c>steam_display</c>, whose value names a line of text that was
    /// set up for the game in Steamworks. The other keys fill in the blanks of that line.
    ///
    /// Setting a key to an empty string takes it away. Set a key again whenever what it
    /// says changes. It is cheap, and setting it to what it already is does no harm.
    ///
    /// Until the text has been set up in Steamworks, friends see nothing, even though
    /// this command works.
    /// </remarks>
    /// <example>
    /// Tell friends which round the player is on:
    /// <code>
    /// round = 1
    /// set steam presence "round", str$(round)
    /// set steam presence "steam_display", "#Status_InRound"
    ///
    /// DO
    ///   IF new spaceKey()
    ///     round = round + 1
    ///     set steam presence "round", str$(round)
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="key">Which piece to set, like <c>steam_display</c>.</param>
    /// <param name="value">What to set it to. An empty string takes the key away.</param>
    /// <seealso cref="ClearSteamPresence">clear steam presence</seealso>
    [FadeBasicCommand("set steam presence")]
    public static void SetSteamPresence(string key, string value)
    {
        if (!SteamSystem.Available || string.IsNullOrEmpty(key)) return;
        SteamFriends.SetRichPresence(key, value ?? "");
    }

    /// <summary>
    /// <para>Takes away all of the player's rich presence.</para>
    /// </summary>
    /// <remarks>
    /// After this, friends only see that the player is in the game. Steam does this by
    /// itself when the game closes, so this is for going back to nothing while the game
    /// is still running, like when the player returns to the main menu.
    /// </remarks>
    /// <example>
    /// Show a status with one key, and take it away with another:
    /// <code>
    /// DO
    ///   IF new spaceKey() THEN set steam presence "steam_display", "#Status_Playing"
    ///   IF new returnKey() THEN clear steam presence
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetSteamPresence">set steam presence</seealso>
    [FadeBasicCommand("clear steam presence")]
    public static void ClearSteamPresence()
    {
        if (!SteamSystem.Available) return;
        SteamFriends.ClearRichPresence();
    }
}
