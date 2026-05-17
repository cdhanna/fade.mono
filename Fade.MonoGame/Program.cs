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
    public static async Task<int> Main(string[] args)
    {
        var csProjPath = GameReloader.GetCsprojPath(); // TODO: support a non-dev way of running the game

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
                game.Run();

            }

        }

        return 0;
    }
}
