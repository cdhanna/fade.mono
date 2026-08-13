using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;

/// <summary>
/// A view onto the world, owned by a <see cref="RenderOutput"/> rather than by a sprite.
///
/// Only a position is stored. The matrix is derived at render time from the camera plus the
/// dimensions of the target being drawn into, and deliberately NOT cached here: two outputs
/// may share one camera at different resolutions (a half-res light buffer alongside a
/// full-res g-buffer), and they need the same WORLD view expressed as different pixel
/// translations. A matrix baked into the camera would be silently wrong for one of them.
///
/// Rotation and scale are absent on purpose. Zoom belongs to `set render size`, where it
/// stays pixel-exact because the scale happens once at the composite blit; and rotation
/// cannot work for pre-rendered isometric art, whose implied 3D geometry does not turn with
/// the pixels.
/// </summary>
public struct Camera
{
    public int id;

    /// <summary>
    /// The world point that appears at the CENTRE of the view, not its top-left corner.
    /// Centre-based is what gameplay code wants ("look at the player") and is the convention
    /// that stays sane if a zoom is ever added above this layer.
    /// </summary>
    public Vector2 position;
}

public static class CameraSystem
{
    /// <summary>
    /// Cameras are a handful per game, not thousands: one for the world, maybe one for a
    /// minimap or a second player. Outputs cap at 63 for bitmask reasons and nothing needs
    /// more cameras than outputs.
    /// </summary>
    public const int MAX_CAMERA_COUNT = 32;

    /// <summary>Camera id 0 is the identity view, and is not a real camera.</summary>
    public const int CAMERA_ID_NONE = 0;

    public static Camera[] cameras = new Camera[MAX_CAMERA_COUNT];
    public static int cameraCount = 0;
    public static int highestCameraId = 0;
    private static Dictionary<int, int> _cameraMap = new Dictionary<int, int>();

    public static void Reset()
    {
        cameraCount = 0;
        highestCameraId = 0;
        _cameraMap.Clear();
    }

    /// <summary>
    /// Resolves a camera id to its slot, creating the slot on first use -- the same
    /// lazy-create contract as <see cref="SpriteSystem.GetSpriteIndex"/>.
    /// </summary>
    public static void GetCameraIndex(int cameraId, out int index, out Camera camera)
    {
        if (cameraId < 1)
            throw new System.ArgumentException(
                "cameraId must be one or greater. 0 means 'no camera', an identity view.");

        if (!_cameraMap.TryGetValue(cameraId, out index))
        {
            if (cameraCount >= MAX_CAMERA_COUNT)
                throw new System.InvalidOperationException(
                    $"too many cameras; the limit is {MAX_CAMERA_COUNT}.");

            highestCameraId = cameraId > highestCameraId ? cameraId : highestCameraId;

            index = _cameraMap[cameraId] = cameraCount;
            camera = new Camera
            {
                id = cameraId,
                position = Vector2.Zero
            };
            cameras[index] = camera;
            cameraCount++;
        }
        else
        {
            camera = cameras[index];
        }
    }

    /// <summary>
    /// The pixel translation a camera applies when drawing into a target of the given size.
    ///
    /// A camera sitting at the centre of its target produces the identity, so adopting a
    /// camera does not move anything until you actually pan it.
    ///
    /// The translation is snapped to whole pixels here rather than at the call sites, because
    /// the batch samples with PointClamp: a half-pixel scroll gains nothing and makes the
    /// whole map crawl. Callers are free to pass fractional positions.
    /// </summary>
    public static Vector2 GetTranslation(int cameraId, int targetWidth, int targetHeight)
    {
        if (cameraId == CAMERA_ID_NONE) return Vector2.Zero;

        GetCameraIndex(cameraId, out _, out var camera);

        var tx = targetWidth * 0.5f - camera.position.X;
        var ty = targetHeight * 0.5f - camera.position.Y;

        return new Vector2((int)System.MathF.Round(tx), (int)System.MathF.Round(ty));
    }

    /// <summary>
    /// The matrix handed to <c>SpriteBatch.Begin</c> for an output. Identity for camera 0, so
    /// every output that never opts in behaves exactly as it did before cameras existed.
    /// </summary>
    public static Matrix GetMatrix(int cameraId, int targetWidth, int targetHeight)
    {
        if (cameraId == CAMERA_ID_NONE) return Matrix.Identity;

        var t = GetTranslation(cameraId, targetWidth, targetHeight);
        return Matrix.CreateTranslation(t.X, t.Y, 0f);
    }
}
