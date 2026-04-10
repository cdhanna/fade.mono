using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    [FadeBasicCommand("push asset", FadeBasicCommandUsage.Macro)]
    public static void Push(string path)
    {
        ContentSystem.Push(path);
    }

    [FadeBasicCommand("rename asset", FadeBasicCommandUsage.Macro)]
    public static void RenameCurrent(string name)
    {
        ContentSystem.GetCurrent().name = name;
    }
    
    public static void Set()
    {
        // # push asset Fish/Audio/bubble-pop-2-293341.mp3
        // # rename asset Fish/Audio/bubble-pop-2.mp3
        // # set asset importer "Mp3Importer"
        // # set asset processor "SoundEffectProcessor"
        // # set asset param "Quality" "Best"
        
        
        
        // # rename asset Fish/Audio/bubble-pop-2-293341.mp3 Fish/Audio/bubble-pop-2.mp3 
        // # asset param Fish/Audio/bubble-pop-2-293341.mp3 
        
        // set importer 
        // set processor, 
        // set parameter
        // set output name
    }

}