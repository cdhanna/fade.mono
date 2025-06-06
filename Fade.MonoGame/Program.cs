using Fade.MonoGame.Game;
using System;
using System.IO;
using System.Threading;
using Fade.MonoGame.Lib;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Lib.Standard;
using Microsoft.Xna.Framework;


var csProjPath = string.Empty;
for (var i = 0; i < args.Length; i++)
{
    if (string.Equals(args[i], "--fade-watch", StringComparison.InvariantCultureIgnoreCase))
    {
        csProjPath = GameReloader.GetCsprojPath();
    }
}

var isWatching = true;
csProjPath = GameReloader.GetCsprojPath(); // TODO: support a non-dev way of running the game


if (!string.IsNullOrEmpty(csProjPath))
{
    var commandCollection = new CommandCollection(
        new StandardCommands(),
        new FadeMonoGameCommands()
    );

    // Game1 game = null;
    ILaunchable fade = new GeneratedFade();
    GameReloader.WatchFiles(csProjPath, commandCollection);
    var game = new Game1(fade);
    game.Run();
}

//     while (true)
//     {
//         if (GameReloader.LatestBuild == null)
//         {
//             Thread.Sleep(25);
//             continue;
//         }
//         
//         
//         if (GameReloader.LatestBuild != fade)
//         {
//             Console.WriteLine("new build available...");
//             fade = GameReloader.LatestBuild;
//
//             if (game != null)
//             {
//                 game.Quit();
//             }
//
//             GameSystem.ResetAll();
//             
//             game = new Game1(fade, () => GameReloader.LatestBuild != fade);
//            
//             game.Run(GameRunBehavior.Synchronous);
//
//             game.Dispose();
//             
//         }
//
//         if (GameReloader.LatestBuild == fade)
//         {
//             // the same build; so quit.
//             break;
//         }
//         
//         // time to reset?
//     }
// }
