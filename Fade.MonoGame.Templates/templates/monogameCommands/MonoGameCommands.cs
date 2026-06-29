using FadeBasic.SourceGenerators;
// Raw MonoGame and the engine's systems are available here because this lib
// references the engine directly — nothing is hidden behind an abstraction.
// Uncomment as you need them:
//   using Microsoft.Xna.Framework;
//   using Fade.MonoGame.Core;

// NOTE: the namespace is intentionally distinct from the class name. The class
// is renamed to your project name; the Fade source-generator emits a
// `<namespace>.<class>` reference, so a namespace equal to the class would
// collide (and a class equal to a fixed member name trips CS0542).
namespace MonoGame.Commands
{
    // Must be `partial`: the Fade source-generator adds the command-source
    // interface to this class.
    public partial class MonoGameCommands
    {
        // A Fade command: a static method tagged with [FadeBasicCommand].
        // From fbasic: `print double(21)` → 42. Replace with your own.
        [FadeBasicCommand("double")]
        public static int Double(int a) => a * 2;

        // --- Driving the engine ----------------------------------------------
        // Commands can call the built-in systems directly, with raw MonoGame
        // types. For example (texture id 0 is the built-in 1x1 white pixel):
        //
        //   [FadeBasicCommand("spawn pixel")]
        //   public static void SpawnPixel(int id, float x, float y)
        //       => Fade.MonoGame.Lib.FadeMonoGameCommands.Sprite(id, x, y, 0);
    }

    // --- Adding your OWN system (Phase 2: IFadeGameModule) -------------------
    // Once the module seam ships, register a per-frame system with raw MonoGame
    // access and Game1 will tick it:
    //
    //   [FadeGameModule]
    //   public class ParticleModule : IFadeGameModule
    //   {
    //       public void Initialize(Game1 game, GraphicsDevice gd) { /* … */ }
    //       public void Update(GameTime time) { /* … */ }
    //       public void Draw(SpriteBatch sb) { /* … */ }
    //   }
}
