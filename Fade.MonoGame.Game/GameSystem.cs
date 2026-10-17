using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Core;

public class GameSystem
{
    public static GraphicsDeviceManager graphicsDeviceManager;

    public static Game1 game;

    public static GameTime latestTime;

    public static long currentFrameNumber;

    public static void ResetMacros()
    {
        ContentSystem.Reset();
    }
    
    public static void ResetAll()
    {
        // a recording that the program left running is finished, while the game it paces is still known
        RecordSystem.ProgramReset();
        game = null;
        graphicsDeviceManager = null;
        latestTime = null;
        currentFrameNumber = 0;
        
        AudioSystem.Reset();
        AudioInstanceSystem.Reset();
        AudioMixSystem.Reset();
        PrefsSystem.Reset();
        WindowMemorySystem.Reset();
        CameraSystem.Reset();
        CollisionSystem.Reset();
        SpriteSystem.Reset();
        InputSystem.Reset();
        ActionSystem.Reset();
        RenderSystem.Reset();
        DepthStencilSystem.Reset();
        RasterizerSystem.Reset();
        SpriteSystem.Reset();
        TextSystem.Reset();
        LocalizationSystem.Reset();
        TextureSystem.Reset();
        TransformSystem.Reset();
        TweenSystem.Reset();
        GizmoSystem.Reset();
#if !BROWSER
        DebugUISystem.CaptureMemoryBaseline();
#endif
    }
}