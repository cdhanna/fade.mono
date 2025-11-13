using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

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
    [FadeBasicCommand("atan2")]
    public static float Atan2(float y, float x)
    {
        return (float)Math.Atan2(y, x);
    }
    [FadeBasicCommand("atan")]
    public static float Atan(float x)
    {
        return (float)Math.Atan(x);
    }
    [FadeBasicCommand("sqrt")]
    public static float Sqrt(float x)
    {
        return (float)Math.Sqrt(x);
    }
    
    [FadeBasicCommand("deg")]
    public static float Deg(float radians)
    {
        return (float)MathHelper.ToDegrees(radians);
    }
    [FadeBasicCommand("rad")]
    public static float Rad(float degrees)
    {
        return (float)MathHelper.ToRadians(degrees);
    }
}