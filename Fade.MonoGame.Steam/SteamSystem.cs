using Fade.MonoGame.Contracts;
using Steamworks;

namespace Fade.MonoGame.Steam;

/// <summary>
/// Starts Steam, pumps its callbacks once a frame, and shuts it down with the game.
///
/// Register one before the game runs, and the commands in <see cref="FadeSteamCommands"/>
/// come alive:
///
/// <code>
/// game.Services.AddService(typeof(IFadeHostSystem), new SteamSystem());
/// </code>
///
/// Without one, or when Steam cannot be reached, nothing throws. <c>steam available()</c>
/// is 0 and every other command does nothing, so a game that is started outside of Steam
/// still runs.
/// </summary>
public sealed class SteamSystem : FadeHostSystem
{
    readonly uint _appId;

    /// <summary>True once Steam is up. Every command checks this first.</summary>
    public static bool Available { get; private set; }

    /// <summary>Why Steam is not up, in words a person can act on. Empty when it is.</summary>
    public static string InitError { get; private set; } = "Steam was never started. Register a SteamSystem in Program.cs.";

    /// <summary>
    /// Put in front of the name of every leaderboard that the game asks for. It is how one
    /// kind of build is kept off of the leaderboards of another: a build from a dev machine
    /// that says "dev_" can submit all the test scores it likes, and no player ever sees them.
    /// </summary>
    public static string LeaderboardPrefix { get; private set; } = "";

    /// <summary>
    /// The secret that score checks are made with. See <c>set steam leaderboard check</c>.
    /// Empty is allowed, and makes a check that anyone could work out.
    /// </summary>
    internal static byte[] ScoreKey { get; private set; } = Array.Empty<byte>();

    /// <param name="appId">
    /// The game's Steam app id. Leave it at 0 to have it found: from the <c>SteamAppId</c>
    /// environment variable, which Steam sets for a game that it launches, and then from a
    /// <c>steam_appid.txt</c> next to the executable, which is how a build on a dev machine
    /// says which game it is. That file must never be shipped in a depot, because it lets
    /// the game start without a license.
    /// </param>
    /// <param name="leaderboardPrefix">See <see cref="LeaderboardPrefix"/>.</param>
    /// <param name="scoreKey">
    /// The secret for score checks. Give a shipped build one that is not in the source: a
    /// score that was made with one key fails the check of a build that has another.
    /// </param>
    public SteamSystem(uint appId = 0, string leaderboardPrefix = "", string scoreKey = "")
    {
        _appId = appId;
        LeaderboardPrefix = leaderboardPrefix ?? "";
        ScoreKey = System.Text.Encoding.UTF8.GetBytes(scoreKey ?? "");
    }

    public override void Initialize(IServiceProvider services)
    {
        if (Available) return;

        var appId = _appId != 0 ? _appId : FindAppId();
        if (appId == 0)
        {
            InitError = "No Steam app id. The game was not started by Steam, and there is no steam_appid.txt next to the executable.";
            Console.WriteLine("[steam] " + InitError);
            return;
        }

        try
        {
            // asyncCallbacks: false, so that callbacks only ever fire from RunCallbacks
            // below, on the game's own thread, at a known point in the frame.
            SteamClient.Init(appId, asyncCallbacks: false);
            Available = true;
            InitError = "";
            Console.WriteLine($"[steam] up. app {SteamClient.AppId}, user {SteamClient.Name}");
        }
        catch (Exception ex)
        {
            // No Steam client running, no license for the app, or the native library is
            // missing. None of them should stop the game from starting.
            Available = false;
            InitError = "Steam could not start: " + ex.Message;
            Console.WriteLine("[steam] " + InitError);
        }
    }

    public override void BeforeVmTick(double elapsedSeconds, double totalSeconds)
    {
        if (!Available) return;
        SteamClient.RunCallbacks();
    }

    public override void Shutdown()
    {
        if (!Available) return;
        Available = false;
        InitError = "Steam has been shut down.";
        SteamLeaderboards.Reset();
        SteamAchievements.Reset();
        SteamClient.Shutdown();
    }

    static uint FindAppId()
    {
        if (uint.TryParse(Environment.GetEnvironmentVariable("SteamAppId"), out var fromEnv) && fromEnv != 0)
        {
            return fromEnv;
        }

        // Next to the executable, and not the working directory: a shipped game may have
        // moved its working directory to wherever it keeps its saves.
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "steam_appid.txt");
            if (File.Exists(path) && uint.TryParse(File.ReadAllText(path).Trim(), out var fromFile))
            {
                return fromFile;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[steam] could not read steam_appid.txt: " + ex.Message);
        }

        return 0;
    }
}
