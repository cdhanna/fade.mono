using System.Runtime.InteropServices;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Toggles fullscreen mode on or off.</para>
    /// <para>When going fullscreen, the back buffer resolution is automatically set to match your monitor's native resolution.</para>
    /// </summary>
    /// <remarks>
    /// Call this during setup after you have configured your desired resolution with
    /// <see cref="SetScreenResolution">set screen size</see>. Internally, this applies the
    /// changes and resets render positioning, so you do not need to do that yourself. You can
    /// grab the monitor dimensions ahead of time with <see cref="DisplayWidth">display width</see>
    /// and <see cref="DisplayHeight">display height</see> if you need to do any math before switching.
    /// </remarks>
    /// <example>
    /// Enter fullscreen mode at startup:
    /// <code>
    /// ` configure screen size then go fullscreen
    /// set screen size 1920, 1080
    /// set fullscreen 1
    /// </code>
    /// </example>
    /// <example>
    /// Toggle fullscreen on and off with the space key:
    /// <code>
    /// isFullscreen = 0
    /// set sync rate 16
    /// DO
    ///   IF new spaceKey() = 1
    ///     IF isFullscreen = 0
    ///       set fullscreen 1
    ///       isFullscreen = 1
    ///     ELSE
    ///       set fullscreen 0
    ///       isFullscreen = 0
    ///     ENDIF
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="fullScreen"><c>1</c> to go fullscreen, <c>0</c> for windowed.</param>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    /// <seealso cref="DisplayWidth">display width</seealso>
    /// <seealso cref="DisplayHeight">display height</seealso>
    /// <seealso cref="SetSyncRate">set sync rate</seealso>
    [FadeBasicCommand("set fullscreen")]
    public static void SetFullScreen(bool fullScreen)
    {
        GameSystem.graphicsDeviceManager.IsFullScreen = fullScreen;

        GameSystem.graphicsDeviceManager.PreferredBackBufferWidth = DisplayWidth();
        GameSystem.graphicsDeviceManager.PreferredBackBufferHeight = DisplayHeight();
        GameSystem.graphicsDeviceManager.ApplyChanges();
        RenderSystem.ResetRenderPositioning();

    }

    /// <summary>
    /// <para>Sets the text that appears in your game window's title bar.</para>
    /// </summary>
    /// <remarks>
    /// Usually you just call this once at startup and forget about it. Nothing stops you from
    /// changing it later if you want to show dynamic info in the title bar, though.
    /// </remarks>
    /// <example>
    /// Set the window title at startup:
    /// <code>
    /// ` give the game window a title
    /// set window title "My Awesome Game"
    /// set screen size 1280, 720
    /// </code>
    /// </example>
    /// <param name="title">The title string to display in the window bar.</param>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    [FadeBasicCommand("set window title")]
    public static void SetWindowTitle(string title)
    {
        GameSystem.game.Window.Title = title;
    }

    /// <summary>
    /// <para>Checks if the game is running on Windows.</para>
    /// </summary>
    /// <remarks>
    /// Use this alongside <see cref="IsMac">is os mac</see> when you need to branch on
    /// platform-specific behavior. For example, you might pick different default resolutions
    /// on Windows vs Mac.
    /// </remarks>
    /// <example>
    /// Choose a resolution based on the operating system:
    /// <code>
    /// ` set resolution based on platform
    /// IF is os windows() = 1
    ///   set screen size 1920, 1080
    /// ELSE
    ///   set screen size 1280, 720
    /// ENDIF
    /// </code>
    /// </example>
    /// <returns><c>1</c> if running on Windows, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsMac">is os mac</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    [FadeBasicCommand("is os windows")]
    public static int IsWindows()
    {
        return OperatingSystem.IsWindows() ? 1 : 0;
    }

    /// <summary>
    /// <para>Checks if the game is running on macOS.</para>
    /// </summary>
    /// <remarks>
    /// Use this alongside <see cref="IsWindows">is os windows</see> when you need to branch on
    /// platform-specific behavior. For example, you might pick different default resolutions or
    /// input handling on Mac vs Windows.
    /// </remarks>
    /// <example>
    /// Adjust settings on macOS:
    /// <code>
    /// ` check if running on Mac and adjust accordingly
    /// IF is os mac() = 1
    ///   set screen size 1280, 800
    ///   print "Running on macOS"
    /// ENDIF
    /// </code>
    /// </example>
    /// <returns><c>1</c> if running on macOS, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsWindows">is os windows</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    /// <seealso cref="Print">print</seealso>
    [FadeBasicCommand("is os mac")]
    public static int IsMac()
    {
        return OperatingSystem.IsMacOS() ? 1 : 0;
    }

    /// <summary>
    /// <para>Returns the full width of your physical monitor in pixels.</para>
    /// <para>This is the monitor resolution, not your game window size.</para>
    /// </summary>
    /// <remarks>
    /// Do not confuse this with <see cref="ScreenWidth">screen width</see>, which gives you the
    /// game's back buffer width (that is, what you set with <see cref="SetScreenResolution">set screen size</see>).
    /// This is handy when setting up fullscreen. You can read the display dimensions first to
    /// decide how to configure your game resolution. Pairs with <see cref="DisplayHeight">display height</see>.
    /// </remarks>
    /// <example>
    /// Print the monitor resolution:
    /// <code>
    /// ` check the monitor's native resolution
    /// w = display width()
    /// h = display height()
    /// print w
    /// print h
    /// </code>
    /// </example>
    /// <example>
    /// Set the game window to half the monitor width:
    /// <code>
    /// ` size the window to half the display
    /// dw = display width()
    /// dh = display height()
    /// set screen size dw / 2, dh / 2
    /// </code>
    /// </example>
    /// <returns>The monitor width in pixels.</returns>
    /// <seealso cref="DisplayHeight">display height</seealso>
    /// <seealso cref="ScreenWidth">screen width</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    [FadeBasicCommand("display width")]
    public static int DisplayWidth()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
    }

    /// <summary>
    /// <para>Returns the full height of your physical monitor in pixels.</para>
    /// <para>This is the monitor resolution, not your game window size.</para>
    /// </summary>
    /// <remarks>
    /// Do not confuse this with <see cref="ScreenHeight">screen height</see>, which gives you the
    /// game's back buffer height (that is, what you set with <see cref="SetScreenResolution">set screen size</see>).
    /// Useful when planning your fullscreen setup. Pairs with <see cref="DisplayWidth">display width</see>.
    /// </remarks>
    /// <example>
    /// Use the display height to decide on a resolution:
    /// <code>
    /// ` pick a game height based on the monitor
    /// dh = display height()
    /// IF dh &gt;= 1080
    ///   set screen size 1920, 1080
    /// ELSE
    ///   set screen size 1280, 720
    /// ENDIF
    /// </code>
    /// </example>
    /// <returns>The monitor height in pixels.</returns>
    /// <seealso cref="DisplayWidth">display width</seealso>
    /// <seealso cref="ScreenHeight">screen height</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    [FadeBasicCommand("display height")]
    public static int DisplayHeight()
    {
        return GameSystem.graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
    }

    /// <summary>
    /// <para>Returns your game's current back buffer width in pixels.</para>
    /// <para>This is the game window size, not the physical monitor resolution.</para>
    /// </summary>
    /// <remarks>
    /// This returns whatever you last set with <see cref="SetScreenResolution">set screen size</see>.
    /// If you need the physical monitor width instead, use <see cref="DisplayWidth">display width</see>.
    /// Pairs with <see cref="ScreenHeight">screen height</see>.
    /// </remarks>
    /// <example>
    /// Center a sprite horizontally on screen:
    /// <code>
    /// ` place a sprite in the center of the screen
    /// texture 1, "Images/Logo"
    /// sprite 1, 0, 0, 1
    /// sw = screen width()
    /// w = texture width(1)
    /// xPos = (sw - w) / 2
    /// position sprite 1, xPos, 100
    /// </code>
    /// </example>
    /// <returns>The game's back buffer width in pixels.</returns>
    /// <seealso cref="ScreenHeight">screen height</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    /// <seealso cref="DisplayWidth">display width</seealso>
    [FadeBasicCommand("screen width")]
    public static int ScreenWidth()
    {
        return GameSystem.graphicsDeviceManager.PreferredBackBufferWidth;
    }

    /// <summary>
    /// <para>Returns your game's current back buffer height in pixels.</para>
    /// <para>This is the game window size, not the physical monitor resolution.</para>
    /// </summary>
    /// <remarks>
    /// This returns whatever you last set with <see cref="SetScreenResolution">set screen size</see>.
    /// If you need the physical monitor height instead, use <see cref="DisplayHeight">display height</see>.
    /// Pairs with <see cref="ScreenWidth">screen width</see>.
    /// </remarks>
    /// <example>
    /// Keep a sprite at the bottom of the screen:
    /// <code>
    /// ` position a ground sprite at the bottom edge
    /// texture 1, "Images/Ground"
    /// sprite 1, 0, 0, 1
    /// sh = screen height()
    /// h = texture height(1)
    /// position sprite 1, 0, sh - h
    /// </code>
    /// </example>
    /// <returns>The game's back buffer height in pixels.</returns>
    /// <seealso cref="ScreenWidth">screen width</seealso>
    /// <seealso cref="SetScreenResolution">set screen size</seealso>
    /// <seealso cref="DisplayHeight">display height</seealso>
    [FadeBasicCommand("screen height")]
    public static int ScreenHeight()
    {
        return GameSystem.graphicsDeviceManager.PreferredBackBufferHeight;
    }

    /// <summary>
    /// <para>Sets the game window resolution by updating the back buffer dimensions.</para>
    /// <para>This applies immediately. There is no need to call a separate apply or refresh command.</para>
    /// </summary>
    /// <remarks>
    /// Call this during setup to establish your game's window size. This controls the actual pixel
    /// dimensions of the game window (the back buffer), which is different from the internal render
    /// resolution you can set with <see cref="SetRenderSize">set render size</see>.
    /// Think of screen size as "how big is the window on the desktop" and render size as "how many
    /// pixels does the game actually draw at internally."
    ///
    /// After calling this, you can read the values back with <see cref="ScreenWidth">screen width</see>
    /// and <see cref="ScreenHeight">screen height</see>. If you want to go fullscreen instead, use
    /// <see cref="SetFullScreen">set fullscreen</see>, which will override the back buffer to match
    /// your monitor's native resolution.
    /// </remarks>
    /// <example>
    /// Set up a standard 720p window:
    /// <code>
    /// ` configure a 720p game window
    /// set window title "My Game"
    /// set screen size 1280, 720
    /// set sync rate 16
    /// DO
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Match the screen size to the monitor for borderless windowed:
    /// <code>
    /// ` fill the whole display without going fullscreen
    /// dw = display width()
    /// dh = display height()
    /// set screen size dw, dh
    /// </code>
    /// </example>
    /// <param name="width">Desired window width in pixels. Typical values are <c>640</c>, <c>1280</c>, or <c>1920</c>.</param>
    /// <param name="height">Desired window height in pixels. Typical values are <c>480</c>, <c>720</c>, or <c>1080</c>.</param>
    /// <seealso cref="SetWindowTitle">set window title</seealso>
    /// <seealso cref="SetSyncRate">set sync rate</seealso>
    /// <seealso cref="ScreenWidth">screen width</seealso>
    /// <seealso cref="ScreenHeight">screen height</seealso>
    /// <seealso cref="SetFullScreen">set fullscreen</seealso>
    /// <seealso cref="DisplayWidth">display width</seealso>
    /// <seealso cref="DisplayHeight">display height</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
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
