using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

/// <summary>
/// Cameras. A camera is a position that a render output views the world through, so that
/// world-space sprite coordinates stop having to be screen-space coordinates.
///
/// The association is per OUTPUT, not per sprite, which means UI immunity is structural: put
/// the HUD on its own output, leave that output on camera 0, and no amount of panning can
/// drag it off screen. It also means one camera can be shared by several outputs -- which a
/// deferred pipeline requires, since a g-buffer pass and a light pass that disagree about the
/// view produce lighting offset from the geometry it is supposed to be lighting.
/// </summary>
public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Peeks at the next unused camera ID without claiming it.</para>
    /// <para>Usually you want <see cref="ReserveCameraNextId">reserve camera id</see>, which
    /// claims the ID and creates its slot.</para>
    /// </summary>
    /// <param name="cameraId">Receives the next free camera ID.</param>
    /// <returns>The next free camera ID.</returns>
    /// <seealso cref="ReserveCameraNextId">reserve camera id</seealso>
    [FadeBasicCommand("free camera id")]
    public static int GetFreeCameraNextId(ref int cameraId)
    {
        cameraId = CameraSystem.highestCameraId + 1;
        return cameraId;
    }

    /// <summary>
    /// <para>Claims the next available camera ID and initializes its slot.</para>
    /// <para>The new camera sits at world <c>(0, 0)</c>, which means it looks at the world
    /// origin -- NOT that it has no effect. See <see cref="PositionCamera">position
    /// camera</see> for why that distinction matters the first time you attach one.</para>
    /// </summary>
    /// <example>
    /// Claim a camera, point it somewhere, and let the main output look through it.
    /// <code>
    /// cam = reserve camera id(cam)
    /// position camera cam, 640, 360
    /// set render target camera 1, cam
    /// </code>
    /// </example>
    /// <param name="cameraId">Receives the reserved camera ID.</param>
    /// <returns>The newly reserved camera ID.</returns>
    /// <seealso cref="CreateCamera">camera</seealso>
    /// <seealso cref="SetRenderTargetCamera">set render target camera</seealso>
    [FadeBasicCommand("reserve camera id")]
    public static int ReserveCameraNextId(ref int cameraId)
    {
        GetFreeCameraNextId(ref cameraId);
        CameraSystem.GetCameraIndex(cameraId, out _, out _);
        return cameraId;
    }

    /// <summary>
    /// <para>Creates a camera, or moves an existing one.</para>
    /// <para>Identical to <see cref="PositionCamera">position camera</see>; this spelling
    /// exists so a camera can be declared in one line the way <c>sprite</c> and <c>text</c>
    /// can.</para>
    /// </summary>
    /// <param name="cameraId">The camera's ID. Reusing an existing ID moves it.</param>
    /// <param name="x">The world X coordinate at the centre of the view.</param>
    /// <param name="y">The world Y coordinate at the centre of the view.</param>
    /// <seealso cref="PositionCamera">position camera</seealso>
    // NOT named Camera: FadeMonoGameCommands is partial across every command file, so a member
    // called Camera would shadow the Camera type in all of them.
    [FadeBasicCommand("camera")]
    public static void CreateCamera(int cameraId, float x, float y)
        => PositionCamera(cameraId, x, y);

    /// <summary>
    /// <para>Moves a camera. The coordinates are the world point that lands at the CENTRE of
    /// the view, not its top-left corner.</para>
    /// <para>Centre-based means a camera sitting at the middle of the render buffer produces
    /// no visible change at all -- which is the useful sanity check when adopting one. It also
    /// means a camera at <c>(0, 0)</c> is NOT the same as no camera: it centres the view on the
    /// world origin, shifting everything by half a buffer. If attaching a camera appears to
    /// throw your scene off screen, this is why.</para>
    /// </summary>
    /// <remarks>
    /// Fractional positions are fine. The translation is snapped to whole pixels when it is
    /// applied, because sprites are sampled with point filtering and a sub-pixel scroll would
    /// only make the image crawl.
    ///
    /// Moving a camera does not re-sort anything -- it cannot change the relative order of
    /// sprites -- so it is cheap enough to drive every frame.
    /// </remarks>
    /// <param name="cameraId">The camera to move.</param>
    /// <param name="x">The world X coordinate at the centre of the view.</param>
    /// <param name="y">The world Y coordinate at the centre of the view.</param>
    /// <seealso cref="GetCameraX">camera x</seealso>
    /// <seealso cref="GetCameraY">camera y</seealso>
    [FadeBasicCommand("position camera")]
    public static void PositionCamera(int cameraId, float x, float y)
    {
        CameraSystem.GetCameraIndex(cameraId, out var index, out var camera);
        camera.position.X = x;
        camera.position.Y = y;
        CameraSystem.cameras[index] = camera;
    }

    /// <summary>
    /// <para>The world X coordinate at the centre of a camera's view.</para>
    /// </summary>
    /// <param name="cameraId">The camera to read.</param>
    /// <returns>The camera's world X position.</returns>
    /// <seealso cref="PositionCamera">position camera</seealso>
    [FadeBasicCommand("camera x")]
    public static float GetCameraX(int cameraId)
    {
        CameraSystem.GetCameraIndex(cameraId, out _, out var camera);
        return camera.position.X;
    }

    /// <summary>
    /// <para>The world Y coordinate at the centre of a camera's view.</para>
    /// </summary>
    /// <param name="cameraId">The camera to read.</param>
    /// <returns>The camera's world Y position.</returns>
    /// <seealso cref="PositionCamera">position camera</seealso>
    [FadeBasicCommand("camera y")]
    public static float GetCameraY(int cameraId)
    {
        CameraSystem.GetCameraIndex(cameraId, out _, out var camera);
        return camera.position.Y;
    }

    /// <summary>
    /// <para>Makes a render output view the world through a camera. Pass <c>0</c> for the
    /// camera to go back to an unmoved, identity view.</para>
    /// <para>Output <c>1</c> is the default output that every sprite and text lands on unless
    /// told otherwise, so <c>set render target camera 1, cam</c> is how you move the whole
    /// scene.</para>
    /// </summary>
    /// <remarks>
    /// Text shares outputs with sprites, so it moves with the camera too. That is usually what
    /// you want for world-space labels and never what you want for a HUD: give the HUD its own
    /// output with <see cref="SetSpriteTarget">set sprite render target</see> and leave
    /// that output on camera 0.
    ///
    /// Several outputs may share one camera. In a multi-pass setup they generally should, since
    /// passes that disagree about the view produce results that do not line up.
    /// </remarks>
    /// <param name="outputId">The render output to attach the camera to. 1 is the default output.</param>
    /// <param name="cameraId">The camera to view through, or 0 for none.</param>
    /// <seealso cref="PositionCamera">position camera</seealso>
    /// <seealso cref="SetSpriteTarget">set sprite render target</seealso>
    [FadeBasicCommand("set render target camera")]
    public static void SetRenderTargetCamera(int outputId, int cameraId)
    {
        RenderSystem.GetOutputIndex(outputId, out _, out var output);
        output.cameraId = cameraId;
    }

    /// <summary>
    /// <para>Converts a screen X coordinate into a world X coordinate, through a camera.</para>
    /// <para>This is what makes mouse picking survive a camera. <c>mouse x()</c> deliberately
    /// keeps reporting render-buffer space -- with per-output cameras there is no single world
    /// answer for a screen pixel, only an answer per camera -- so the unprojection is explicit
    /// about which camera it means.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// worldX = screen to world x(cam, mouse x())
    /// worldY = screen to world y(cam, mouse y())
    /// </code>
    /// </example>
    /// <param name="cameraId">The camera to unproject through. 0 passes the value straight back.</param>
    /// <param name="screenX">A render-buffer X coordinate, such as <c>mouse x()</c>.</param>
    /// <returns>The corresponding world X coordinate.</returns>
    /// <seealso cref="ScreenToWorldY">screen to world y</seealso>
    /// <seealso cref="WorldToScreenX">world to screen x</seealso>
    [FadeBasicCommand("screen to world x")]
    public static float ScreenToWorldX(int cameraId, float screenX)
        => screenX - CameraSystem.GetTranslation(cameraId,
            RenderSystem.mainBuffer.Width, RenderSystem.mainBuffer.Height).X;

    /// <summary>
    /// <para>Converts a screen Y coordinate into a world Y coordinate, through a camera.</para>
    /// <para>See <see cref="ScreenToWorldX">screen to world x</see>.</para>
    /// </summary>
    /// <param name="cameraId">The camera to unproject through. 0 passes the value straight back.</param>
    /// <param name="screenY">A render-buffer Y coordinate, such as <c>mouse y()</c>.</param>
    /// <returns>The corresponding world Y coordinate.</returns>
    /// <seealso cref="ScreenToWorldX">screen to world x</seealso>
    [FadeBasicCommand("screen to world y")]
    public static float ScreenToWorldY(int cameraId, float screenY)
        => screenY - CameraSystem.GetTranslation(cameraId,
            RenderSystem.mainBuffer.Width, RenderSystem.mainBuffer.Height).Y;

    /// <summary>
    /// <para>Converts a world X coordinate into a screen X coordinate, through a camera.</para>
    /// <para>The inverse of <see cref="ScreenToWorldX">screen to world x</see>. Use it to park
    /// something that must NOT scale or scroll -- a name plate, a damage number, an off-screen
    /// marker -- over a thing that lives in the world.</para>
    /// </summary>
    /// <param name="cameraId">The camera to project through. 0 passes the value straight back.</param>
    /// <param name="worldX">A world X coordinate.</param>
    /// <returns>The corresponding render-buffer X coordinate.</returns>
    /// <seealso cref="WorldToScreenY">world to screen y</seealso>
    /// <seealso cref="ScreenToWorldX">screen to world x</seealso>
    [FadeBasicCommand("world to screen x")]
    public static float WorldToScreenX(int cameraId, float worldX)
        => worldX + CameraSystem.GetTranslation(cameraId,
            RenderSystem.mainBuffer.Width, RenderSystem.mainBuffer.Height).X;

    /// <summary>
    /// <para>Converts a world Y coordinate into a screen Y coordinate, through a camera.</para>
    /// <para>See <see cref="WorldToScreenX">world to screen x</see>.</para>
    /// </summary>
    /// <param name="cameraId">The camera to project through. 0 passes the value straight back.</param>
    /// <param name="worldY">A world Y coordinate.</param>
    /// <returns>The corresponding render-buffer Y coordinate.</returns>
    /// <seealso cref="WorldToScreenX">world to screen x</seealso>
    [FadeBasicCommand("world to screen y")]
    public static float WorldToScreenY(int cameraId, float worldY)
        => worldY + CameraSystem.GetTranslation(cameraId,
            RenderSystem.mainBuffer.Width, RenderSystem.mainBuffer.Height).Y;
}
