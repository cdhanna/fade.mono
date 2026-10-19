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

                // The test platform finishes within milliseconds of the game closing, once every
                // test has reported. If a test ended the game before reporting (an error, or the
                // program reaching `end` outside the test flow), the platform waits forever for
                // that result, and the run hangs. So it gets a while, and then the run is called
                // failed.
                var finished = await Task.WhenAny(mtp, Task.Delay(TimeSpan.FromSeconds(TestFinishTimeoutSeconds)));
                int testExitCode;
                if (finished == mtp)
                {
                    testExitCode = await mtp;
                }
                else
                {
                    Console.WriteLine($"[test] the test platform did not finish within {TestFinishTimeoutSeconds}s of the game closing. A test probably ended the game before it reported. The run is called failed.");
                    testExitCode = TestHungExitCode;
                }
                FileLog.WriteLine("MTP IS OVER " + testExitCode);

                // The results are out, so nothing is left for the process to do. It ends right
                // here, without the native libraries' own clean-up on exit, which has been seen to
                // deadlock now and then and leave the window hanging. Windows takes back
                // everything the process had either way.
                ExitNow(testExitCode);
                return testExitCode;

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
                game.SetGcSettings(4096, paranoid: false); // the same as the game that ships. see below.
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
        var launch = LaunchOptions.Read(args);
#if !FADE_CONTENT_HOTRELOAD
        UseSaveDirectory();
        StartLog(launch.perfLog ? GameFolder() : null);
#endif
        launch.Print();
        using var shipped = new Game1(new GeneratedFade());
        launch.Apply(shipped);

        // A player has no debugger to attach, and running under the debug session more than
        // doubles what every frame of the program costs. FISH_DEBUG keeps it, for the inspector.
        shipped.DebugProgram = Environment.GetEnvironmentVariable("FISH_DEBUG") != null;

        // The program's own heap is swept after this many allocations. The default of 64 means a
        // sweep or two on every frame; this is a sweep every few frames, for about a tenth less
        // time in the program, and a little more memory in between.
        shipped.SetGcSettings(4096, paranoid: false);

        // A build with $(FadeMonoEncodeAudioToOgg) caches its decoded sounds next to the game, or,
        // where that cannot be written, in the player's cache folder under this name -- the same
        // name the save directory uses. See FadeContentManager.
        if (shipped.Content is FadeContentManager oggContent) oggContent.CacheName = "FishFishPop";

        shipped.Services.AddService(typeof(IFadeHostSystem), NewSteamSystem());
        shipped.Run();
        return 0;
    }

    // A test run: how long the test platform gets to finish once the game has closed, and the exit
    // code of a run where it never does. Microsoft.Testing.Platform's own codes are small (0 is a
    // pass, 2 is a failed test), so this one stands apart from them.
    const int TestFinishTimeoutSeconds = 30;
    const int TestHungExitCode = 90;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool TerminateProcess(IntPtr process, uint exitCode);

    // End the process right now, with this exit code, once a test run has said all that it has to.
    // Everything that was written is flushed first. On Windows it skips the clean-up that the native
    // libraries do on the way out (the audio, the graphics driver, SDL), since one of them has been
    // seen to deadlock there now and then. Windows takes back the memory, the handles, the device
    // and the window either way. Elsewhere, it is a plain exit.
    static void ExitNow(int exitCode)
    {
        try { Console.Out.Flush(); } catch { }
        try { Console.Error.Flush(); } catch { }
        if (OperatingSystem.IsWindows())
        {
            TerminateProcess(GetCurrentProcess(), (uint)exitCode);
        }
        Environment.Exit(exitCode);
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
    //
    // With --perf-log (see LaunchOptions), the same log is also written to fishfishpop-log.txt in
    // the game's own folder, the one Steam opens with Manage > Browse local files, which is
    // easier to tell a player how to find. The perf log writes a few hundred characters a second,
    // so the limit is raised too, to cover a few hours of play.
    static long LogLimit = 2_000_000;
    static long _logLength;

    static void StartLog(string alsoInFolder = null)
    {
        try
        {
            if (System.IO.File.Exists("log.txt"))
            {
                System.IO.File.Copy("log.txt", "log.prev.txt", overwrite: true);
            }

            System.IO.TextWriter file = new System.IO.StreamWriter("log.txt", append: false) { AutoFlush = true };
            string copyNote = null;
            if (alsoInFolder != null)
            {
                LogLimit = 20_000_000;
                var copyPath = System.IO.Path.Combine(alsoInFolder, "fishfishpop-log.txt");
                try
                {
                    var copy = new System.IO.StreamWriter(copyPath, append: false) { AutoFlush = true };
                    file = new BothWriter(file, copy);
                    copyNote = "[log] also writing " + copyPath;
                }
                catch (Exception ex)
                {
                    // A game folder the player cannot write to: the log in the save folder still works.
                    copyNote = "[log] could not write " + copyPath + ": " + ex.Message;
                }
            }
            Console.SetOut(new LogWriter(Console.Out, file));
            Console.SetError(new LogWriter(Console.Error, file));

            var version = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(Program).Assembly)
                ?.InformationalVersion ?? "unknown";
            Console.WriteLine($"[log] started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("[log] writing " + System.IO.Path.GetFullPath("log.txt"));
            if (copyNote != null) Console.WriteLine(copyNote);
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

    // Writes everything to two files.
    sealed class BothWriter : System.IO.TextWriter
    {
        readonly System.IO.TextWriter _a;
        readonly System.IO.TextWriter _b;

        public BothWriter(System.IO.TextWriter a, System.IO.TextWriter b)
        {
            _a = a;
            _b = b;
        }

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void Write(char value) { _a.Write(value); _b.Write(value); }
        public override void Write(string value) { _a.Write(value); _b.Write(value); }
        public override void WriteLine(string value) { _a.WriteLine(value); _b.WriteLine(value); }
        public override void Flush() { _a.Flush(); _b.Flush(); }
    }

    // The folder the game is installed in, the one Steam opens with Manage > Browse local files.
    // On macOS the executable is inside FishFishPop.app/Contents/MacOS, and that folder is the one
    // the .app is in.
    static string GameFolder()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        if (dir.Name == "MacOS" && dir.Parent?.Name == "Contents" && dir.Parent.Parent != null
            && dir.Parent.Parent.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            && dir.Parent.Parent.Parent != null)
        {
            return dir.Parent.Parent.Parent.FullName;
        }
        return dir.FullName;
    }

    // Settings a player can give the game for a bug report, through Steam: right-click the game,
    // Properties, General, Launch Options. Steam puts them on the command line. Each is a word,
    // with one or two dashes or none, and maybe a value after "=":
    //
    //   --perf-log            the frame timing log (FADE_PERF_LOG in Game1), with the whole log
    //                         also written into the game's folder (see StartLog). --perf-log=20
    //                         counts a frame as slow past 20 ms of work instead of 14.
    //   --vsync=off           present each frame as soon as it is drawn, instead of waiting for
    //                         the display. It tears, but tells whether the waiting is the trouble.
    //   --fixed-timestep=off  one update per drawn frame, at the display's rate, instead of a
    //                         fixed 60 a second. What the program counts in frames (the tuna, the
    //                         diver) then runs fast on a screen above 60 Hz, so this is only for
    //                         finding out whether frame pacing is what a player sees.
    //   --gc=low-latency      ask .NET to avoid full collections that stop the game, and do them
    //                         alongside it instead (GCLatencyMode.SustainedLowLatency).
    //   --max-catchup=2       at most this many updates before a draw. On a machine that cannot
    //                         keep up, the game then runs slower than real time instead of
    //                         drawing twice a second. 3 when not given; 0 is MonoGame's own
    //                         limit (30). See PerfKnobs.
    //   --effect-scale=2      the screen effect (the water) on a picture this many times the
    //                         game's size, instead of the 3 the game asks for. 2 is 2.25 times
    //                         fewer pixels for the graphics card and a little softer; 1 is 9
    //                         times fewer, and blurry.
    //   --slow-update=10      add this many milliseconds to every update, and --slow-draw=10 to
    //   --slow-draw=10        every draw: a fast machine made to act like a slow one.
    //
    // The .NET GC's own settings (the size of gen 0, server GC) are read before any of this code
    // runs, so they cannot be launch options. On macOS and Linux, Steam's launch options can set
    // them as environment variables instead: DOTNET_GCgen0size=0x4000000 %command%
    sealed class LaunchOptions
    {
        public bool perfLog;
        public bool? vsync;
        public bool? fixedTimeStep;
        public bool lowLatencyGc;
        string[] _args = Array.Empty<string>();
        readonly System.Collections.Generic.List<string> _unknown = new();

        public static LaunchOptions Read(string[] args)
        {
            var options = new LaunchOptions { _args = args ?? Array.Empty<string>() };
            foreach (var arg in options._args)
            {
                var word = arg.TrimStart('-').ToLowerInvariant();
                var equals = word.IndexOf('=');
                var name = equals < 0 ? word : word.Substring(0, equals);
                var value = equals < 0 ? null : word.Substring(equals + 1);
                switch (name)
                {
                    case "perf-log":
                        options.perfLog = true;
                        // Game1 reads this when it is first used, which is after this.
                        Environment.SetEnvironmentVariable("FADE_PERF_LOG",
                            int.TryParse(value, out var slowMs) && slowMs > 1 ? slowMs.ToString() : "1");
                        break;
                    case "vsync":
                        options.vsync = OnOff(value);
                        break;
                    case "fixed-timestep":
                        options.fixedTimeStep = OnOff(value);
                        break;
                    case "gc":
                        if (value == "low-latency") options.lowLatencyGc = true;
                        else options._unknown.Add(arg);
                        break;
                    case "effect-scale":
                        // FADE_SCREEN_EFFECT_SCALE wins over the game's own `set screen effect scale`
                        // (RenderCommands), and RenderSystem reads it when it is first used, after this.
                        if (int.TryParse(value, out var effectScale) && effectScale >= 1)
                            Environment.SetEnvironmentVariable("FADE_SCREEN_EFFECT_SCALE", effectScale.ToString());
                        else options._unknown.Add(arg);
                        break;
                    case "max-catchup":
                        if (int.TryParse(value, out var catchUp) && catchUp >= 0) PerfKnobs.maxCatchUp = catchUp;
                        else options._unknown.Add(arg);
                        break;
                    case "slow-update":
                        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var slowUpdate) && slowUpdate >= 0)
                            PerfKnobs.updateDelayMs = slowUpdate;
                        else options._unknown.Add(arg);
                        break;
                    case "slow-draw":
                        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var slowDraw) && slowDraw >= 0)
                            PerfKnobs.drawDelayMs = slowDraw;
                        else options._unknown.Add(arg);
                        break;
                    default:
                        options._unknown.Add(arg);
                        break;
                }
            }
            return options;
        }

        // "off", "false", "no" and "0" are off. Anything else, or no value at all, is on.
        static bool OnOff(string value) => value is not ("off" or "false" or "no" or "0");

        // What the game was started with, and what it has to run on, for the top of a bug report.
        public void Print()
        {
            if (_args.Length > 0) Console.WriteLine("[launch] options: " + string.Join(" ", _args));
            foreach (var word in _unknown) Console.WriteLine("[launch] unknown option, ignored: " + word);
            var memory = GC.GetGCMemoryInfo();
            Console.WriteLine($"[launch] machine: {Environment.ProcessorCount} cpus" +
                              $", {memory.TotalAvailableMemoryBytes >> 20} MB memory" +
                              $", .NET {Environment.Version}" +
                              $", {(System.Runtime.GCSettings.IsServerGC ? "server" : "workstation")} gc");
        }

        // Before Run(): the graphics device does not exist yet, so these are only what it is
        // created with. What is printed is read back from the game, not from the options.
        public void Apply(Game1 game)
        {
            var graphics = game.Services.GetService(typeof(Microsoft.Xna.Framework.IGraphicsDeviceManager))
                as Microsoft.Xna.Framework.GraphicsDeviceManager;
            if (vsync.HasValue && graphics != null) graphics.SynchronizeWithVerticalRetrace = vsync.Value;
            if (fixedTimeStep.HasValue) game.IsFixedTimeStep = fixedTimeStep.Value;
            if (lowLatencyGc) System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

            Console.WriteLine($"[launch] perf log {(Environment.GetEnvironmentVariable("FADE_PERF_LOG") != null ? "on" : "off")}" +
                              $", vsync {(graphics?.SynchronizeWithVerticalRetrace switch { true => "on", false => "off", null => "?" })}" +
                              $", fixed timestep {(game.IsFixedTimeStep ? "on" : "off")}" +
                              $", gc {System.Runtime.GCSettings.LatencyMode}" +
                              (Environment.GetEnvironmentVariable("DOTNET_GCgen0size") is { } gen0 ? $", gen0size {gen0}" : "") +
                              $", max catch-up {(PerfKnobs.maxCatchUp > 0 ? PerfKnobs.maxCatchUp.ToString() : "MonoGame's own (30)")}" +
                              (Environment.GetEnvironmentVariable("FADE_SCREEN_EFFECT_SCALE") is { } effectScale ? $", effect scale {effectScale}" : "") +
                              (PerfKnobs.updateDelayMs > 0 ? $", slow update +{PerfKnobs.updateDelayMs} ms" : "") +
                              (PerfKnobs.drawDelayMs > 0 ? $", slow draw +{PerfKnobs.drawDelayMs} ms" : ""));
        }
    }
}
