using Fade.MonoGame.Core;
using Fade.MonoGame.Lib;
using FadeBasic;
using FadeBasic.Launch;
using FadeBasic.Lib.Standard;


var csProjPath = GameReloader.GetCsprojPath(); // TODO: support a non-dev way of running the game

if (!string.IsNullOrEmpty(csProjPath))
{
    var commandCollection = new CommandCollection(
        new StandardCommands(),
        new FadeMonoGameCommands()
    );

    // ILaunchable fade = new GeneratedFade();
    GameReloader.WatchFiles(csProjPath, commandCollection);
    var fade = GameReloader.LatestBuild;
    var game = new Game1(fade);
    game.Run();
}
