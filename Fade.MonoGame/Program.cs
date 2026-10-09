using System;
using System.Linq;
using System.Threading.Tasks;
using Fade.MonoGame;
using Fade.MonoGame.Contracts;
using Fade.MonoGame.Core;
using Fade.MonoGame.Lib;
using Fade.MonoGame.Steam;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Lib.Standard;
using FadeBasic.Testing;

public class Program
{
    // Release has nothing to await: the whole dev path below is compiled out.
#pragma warning disable CS1998
    public static async Task<int> Main(string[] args)
#pragma warning restore CS1998
    {
        SetVersionForGame();

#if FADE_CONTENT_HOTRELOAD
        // Debug desktop: compile the .fbasic from source at runtime and watch it, so the
        // game hot-reloads. Compiled out of Release, which runs GeneratedFade (below).
        var csProjPath = GameReloader.GetCsprojPath();

        Console.WriteLine("STARTING:");
        if (!string.IsNullOrEmpty(csProjPath))
        {
            var commandCollection = new CommandCollection(
                new StandardCommands(),
                new FadeMonoGameCommands(),
                new FadeSteamCommands()
            );

            // ILaunchable fade = new GeneratedFade();
          
            if (FadeTestApplicationBuilder.IsTestInvocation(args))
            {
                GameReloader.Build(csProjPath, commandCollection);
                var fade = GameReloader.LatestBuild;

                // --help, --list-tests, --info, etc. don't run a test session,
                // so skip the (expensive, window-popping) MonoGame boot and
                // just let MTP print and exit.
                if (FadeTestApplicationBuilder.IsInfoOnlyInvocation(args))
                {
                    return await FadeTestApplicationBuilder.RunAsync((ITestLaunchable)fade, args);
                }
                //
                // var selected = FadeTestApplicationBuilder.SelectTests((ITestLaunchable)fade, args);
                // FileLog.WriteLine($"args: {string.Join(" | ", args)}");
                // FileLog.WriteLine($"selected ({selected.Count}): {string.Join(",", selected.Select(t => t.name))}");
                // if (selected.Count == 0)
                // {
                //     // Filter selected nothing — let MTP report 0-tests-passed without
                //     // booting a window.
                //     return await FadeTestApplicationBuilder.RunAsync((ITestLaunchable)fade, args);
                // }

                var game = new Game1(fade, testMode: true);
#if FADE_CONTENT_HOTRELOAD
                game.Services.AddService(typeof(Fade.MonoGame.Content.IContentBuilder),
                    new Fade.MonoGame.Content.FadeContentBuilder());
#endif
                var host = new MonoGameTestHost(game);
                var mtp = FadeTestApplicationBuilder.RunAsync((ITestLaunchable)fade, args, host);
                if (mtp.IsCompleted)
                {
                    return 0;
                }
                game.Run();
                FileLog.WriteLine("MONOGAME IS OVER");
                await mtp;
                FileLog.WriteLine("MTP IS OVER");
                
                //
                // MonoGameTestHost.onBeforeAll = () =>
                // {
                //     game.Run();
                // };
                // var mtp = 
                //     FadeTestApplicationBuilder.RunAsync((ITestLaunchable)fade, args);
                //
                // return await mtp;
            }
            else
            {
                GameReloader.WatchFiles(csProjPath, commandCollection);
                var fade = GameReloader.LatestBuild;
                var game = new Game1(fade);
                game.Services.AddService(typeof(IFadeHostSystem), NewSteamSystem());
#if FADE_CONTENT_HOTRELOAD
                game.Services.AddService(typeof(Fade.MonoGame.Content.IContentBuilder),
                    new Fade.MonoGame.Content.FadeContentBuilder());
#endif
                game.Run();

            }

            return 0;
        }
#endif

        // Release, or no project on disk: run the program compiled at build time.
        // This is what ships. See steam/SETUP.md.
#if !FADE_CONTENT_HOTRELOAD
        UseSaveDirectory();
        StartLog();
#endif
        using var shipped = new Game1(new GeneratedFade());
        shipped.Services.AddService(typeof(IFadeHostSystem), NewSteamSystem());
        shipped.Run();
        return 0;
    }

    // The version that the main menu shows in its corner. The game reads it from the
    // FISH_VERSION environment variable, with env$. A CI build has the commit that it was made
    // from baked in as assembly metadata, by Fade.MonoGame.csproj, and shows the last 8
    // characters of it. A build from a dev machine has none, and shows "local".
    static void SetVersionForGame()
    {
        var commit = "";
        foreach (var attribute in System.Reflection.CustomAttributeExtensions
                     .GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>(typeof(Program).Assembly))
        {
            if (attribute.Key == "BuildCommit" && !string.IsNullOrEmpty(attribute.Value)) commit = attribute.Value.Trim();
        }

        var version = "local";
        if (commit.Length > 0) version = commit.Length > 8 ? commit.Substring(commit.Length - 8) : commit;
        Environment.SetEnvironmentVariable("FISH_VERSION", version);
    }

    // Steam, with the two things that the build decided for the leaderboards. Both are baked
    // into this assembly by Fade.MonoGame.csproj, as assembly metadata:
    //
    //   LeaderboardPrefix   goes in front of the name of every leaderboard. A CI build for
    //                       the default Steam branch says "live_", one for the test branch
    //                       says "test_", and a build from a dev machine says "dev_", so
    //                       test scores never land where players can see them.
    //   ScoreKey            the secret that the check on every score is made with. CI makes
    //                       it from a GitHub secret and the name of the Steam branch. A dev
    //                       build has the word "dev".
    //
    // See "Leaderboards" in steam/SETUP.md.
    static SteamSystem NewSteamSystem()
    {
        string Baked(string key, string fallback)
        {
            foreach (var attribute in System.Reflection.CustomAttributeExtensions
                         .GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>(typeof(Program).Assembly))
            {
                if (attribute.Key == key && !string.IsNullOrEmpty(attribute.Value)) return attribute.Value;
            }
            return fallback;
        }

        return new SteamSystem(
            leaderboardPrefix: Baked("LeaderboardPrefix", "dev_"),
            scoreKey: Baked("ScoreKey", "dev"));
    }

    // The engine saves prefs.json (the saved game and the options) in the working directory.
    // A shipped game cannot use the one it is launched with: a macOS .app starts in "/", which
    // cannot be written, and on Windows it is the Steam install folder, which is wiped by an
    // uninstall. So a shipped game moves to a folder of its own first:
    //
    //   Windows   %APPDATA%\FishFishPop
    //   macOS     ~/Library/Application Support/FishFishPop
    //
    // Content is not affected: it loads from next to the executable, not the working directory.
    // Steam Auto-Cloud can be pointed at these two folders.
    static void UseSaveDirectory()
    {
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FishFishPop");
            System.IO.Directory.CreateDirectory(dir);
            Environment.CurrentDirectory = dir;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("could not use the save directory: " + ex.Message);
        }
    }

    // A shipped game has no console, so what it prints, and what it dies of, is gone unless it is
    // written down. This writes both into the save directory (see UseSaveDirectory):
    //
    //   log.txt        the run that is going on, or the last one
    //   log.prev.txt   the run before that
    //
    // After a crash, log.txt ends with the exception. Starting the game again moves it to
    // log.prev.txt, so a player who has already restarted should send both.
    //
    // A warning that repeats on every frame would grow the file without end, so the log stops
    // taking lines at LogLimit characters. A crash is always written, whatever the size.
    const long LogLimit = 2_000_000;
    static long _logLength;

    static void StartLog()
    {
        try
        {
            if (System.IO.File.Exists("log.txt"))
            {
                System.IO.File.Copy("log.txt", "log.prev.txt", overwrite: true);
            }

            var file = new System.IO.StreamWriter("log.txt", append: false) { AutoFlush = true };
            Console.SetOut(new LogWriter(Console.Out, file));
            Console.SetError(new LogWriter(Console.Error, file));

            var version = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(Program).Assembly)
                ?.InformationalVersion ?? "unknown";
            Console.WriteLine($"[log] started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"[log] version {version}");
            Console.WriteLine($"[log] os {System.Runtime.InteropServices.RuntimeInformation.OSDescription} " +
                              $"{System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}");

            // The runtime prints an unhandled exception straight to the real stderr, which the
            // log does not see, so it is written here too. This does not stop the crash.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                lock (file)
                {
                    file.WriteLine($"[log] CRASHED {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    file.WriteLine(e.ExceptionObject?.ToString() ?? "no exception object");
                }
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("could not start the log: " + ex.Message);
        }
    }

    // Writes everything to the log file, and on to wherever it was going before.
    sealed class LogWriter : System.IO.TextWriter
    {
        readonly System.IO.TextWriter _inner;
        readonly System.IO.TextWriter _file;

        public LogWriter(System.IO.TextWriter inner, System.IO.TextWriter file)
        {
            _inner = inner;
            _file = file;
        }

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        // Is there room in the log for this many more characters? The line that goes over the
        // limit is replaced by a note that says so, and nothing is written after it.
        bool HasRoom(int length)
        {
            if (_logLength > LogLimit) return false;
            _logLength += length;
            if (_logLength > LogLimit)
            {
                _file.WriteLine();
                _file.WriteLine("[log] the log is full. nothing more is written, except a crash.");
                return false;
            }
            return true;
        }

        public override void Write(char value)
        {
            _inner.Write(value);
            lock (_file)
            {
                if (HasRoom(1)) _file.Write(value);
            }
        }

        public override void Write(string value)
        {
            _inner.Write(value);
            lock (_file)
            {
                if (HasRoom(value?.Length ?? 0)) _file.Write(value);
            }
        }

        public override void WriteLine(string value)
        {
            _inner.WriteLine(value);
            lock (_file)
            {
                if (HasRoom((value?.Length ?? 0) + 1)) _file.WriteLine(value);
            }
        }

        public override void Flush()
        {
            _inner.Flush();
            lock (_file) _file.Flush();
        }
    }
}
