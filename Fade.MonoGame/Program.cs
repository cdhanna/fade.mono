using System;
using System.Linq;
using System.Threading.Tasks;
using Fade.MonoGame;
using Fade.MonoGame.Core;
using Fade.MonoGame.Lib;
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
#if FADE_CONTENT_HOTRELOAD
        // Debug desktop: compile the .fbasic from source at runtime and watch it, so the
        // game hot-reloads. Compiled out of Release, which runs GeneratedFade (below).
        var csProjPath = GameReloader.GetCsprojPath();

        Console.WriteLine("STARTING:");
        if (!string.IsNullOrEmpty(csProjPath))
        {
            var commandCollection = new CommandCollection(
                new StandardCommands(),
                new FadeMonoGameCommands()
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
#endif
        using var shipped = new Game1(new GeneratedFade());
        shipped.Run();
        return 0;
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
}
