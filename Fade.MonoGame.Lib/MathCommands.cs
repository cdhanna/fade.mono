using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("sin")]
    public static float Sin(float x)
    {
        return (float)Math.Sin(x);
    }
    [FadeBasicCommand("cos")]
    public static float Cos(float x)
    {
        return (float)Math.Cos(x);
    }
}