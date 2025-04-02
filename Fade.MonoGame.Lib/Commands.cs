using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("print")]
    public static void Print(params object[] values)
    {
        foreach (var value in values)
        {
            Console.WriteLine(value);
        }
    }

    
    
    [FadeBasicCommand("test")]
    public static void Test(int x)
    {
        Console.WriteLine(x);
    }
}