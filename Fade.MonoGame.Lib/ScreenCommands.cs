using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("set fullscreen")]
    public static void SetFullScreen(bool fullScreen)
    {
        GameSystem.graphicsDeviceManager.IsFullScreen = fullScreen;
        GameSystem.graphicsDeviceManager.ApplyChanges();
    }

    [FadeBasicCommand("screen width")]
    public static int ScreenWidth()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
    }
    
    [FadeBasicCommand("screen height")]
    public static int ScreenHeight()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
    }

    [FadeBasicCommand("set screen size")]
    public static void SetScreenResolution(int width, int height)
    {
        // SetFullScreen(false);
        GameSystem.graphicsDeviceManager.PreferredBackBufferWidth = width;
        GameSystem.graphicsDeviceManager.PreferredBackBufferHeight = height;
        GameSystem.graphicsDeviceManager.ApplyChanges();

    }
}