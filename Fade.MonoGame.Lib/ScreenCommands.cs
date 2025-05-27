using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("set fullscreen")]
    public static void SetFullScreen(bool fullScreen)
    {
        GameSystem.graphicsDeviceManager.IsFullScreen = fullScreen;
        
        GameSystem.graphicsDeviceManager.PreferredBackBufferWidth = GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
        GameSystem.graphicsDeviceManager.PreferredBackBufferHeight = GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
        GameSystem.graphicsDeviceManager.ApplyChanges();
        RenderSystem.ResetRenderPositioning();

    }

    [FadeBasicCommand("screen width")]
    public static int ScreenWidth()
    {
        return GameSystem.graphicsDeviceManager.PreferredBackBufferWidth;
    }
    
    [FadeBasicCommand("screen height")]
    public static int ScreenHeight()
    {
        return GameSystem.graphicsDeviceManager.PreferredBackBufferHeight;
    }

    [FadeBasicCommand("set screen size")]
    public static void SetScreenResolution(int width, int height)
    {
        // SetFullScreen(false);
        GameSystem.graphicsDeviceManager.PreferredBackBufferWidth = width;
        GameSystem.graphicsDeviceManager.PreferredBackBufferHeight = height;
        GameSystem.graphicsDeviceManager.ApplyChanges();
        RenderSystem.ResetRenderPositioning();


    }
}