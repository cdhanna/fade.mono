using System.Runtime.InteropServices;
using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("set fullscreen")]
    public static void SetFullScreen(bool fullScreen)
    {
        GameSystem.graphicsDeviceManager.IsFullScreen = fullScreen;
        
        GameSystem.graphicsDeviceManager.PreferredBackBufferWidth = DisplayWidth();
        GameSystem.graphicsDeviceManager.PreferredBackBufferHeight = DisplayHeight();
        GameSystem.graphicsDeviceManager.ApplyChanges();
        RenderSystem.ResetRenderPositioning();

    }

    [FadeBasicCommand("set window title")]
    public static void SetWindowTitle(string title)
    {
        GameSystem.game.Window.Title = title;
    }
    
    [FadeBasicCommand("is os windows")]
    public static int IsWindows()
    {
        return OperatingSystem.IsWindows() ? 1 : 0;
    }
    
    [FadeBasicCommand("is os mac")]
    public static int IsMac()
    {
        return OperatingSystem.IsMacOS() ? 1 : 0;
    }

    [FadeBasicCommand("display width")]
    public static int DisplayWidth()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
    }

    [FadeBasicCommand("display height")]
    public static int DisplayHeight()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
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