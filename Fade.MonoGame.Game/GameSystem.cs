using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Game;

public class GameSystem
{
    public static GraphicsDeviceManager graphicsDeviceManager;

    public static Game1 game;

    public static GameTime latestTime;

    public static long currentFrameNumber;

    public static void ResetAll()
    {
        game = null;
        graphicsDeviceManager = null;
        latestTime = null;
        currentFrameNumber = 0;
        
        AudioSystem.Reset();
        AudioInstanceSystem.Reset();
        CollisionSystem.Reset();
        SpriteSystem.Reset();
        InputSystem.Reset();
        RenderSystem.Reset();
        SpriteSystem.Reset();
        TextSystem.Reset();
        TextureSystem.Reset();
        TransformSystem.Reset();
        TweenSystem.Reset();

    }
}