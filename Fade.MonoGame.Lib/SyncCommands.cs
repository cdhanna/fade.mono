using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    [FadeBasicCommand("set sync rate")]
    public static void SetSyncRate(int rate)
    {
        GameSystem.game.TargetElapsedTime = TimeSpan.FromMilliseconds(rate);
    }
    
    /// <summary>
    /// allows a render frame to happen
    /// </summary>
    [FadeBasicCommand("sync")]
    public static void Sync([FromVm] VirtualMachine vm)
    {
        vm.Suspend();
    }
    
    [FadeBasicCommand("frame number")]
    public static long Sync()
    {
        return GameSystem.currentFrameNumber;
    }
}