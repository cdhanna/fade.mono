using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Sets the target frame time in milliseconds.</para>
    /// <para>This controls how long the engine waits between frames: <c>16</c> ms gives you roughly 60 fps, <c>33</c> ms gives you roughly 30 fps.</para>
    /// </summary>
    /// <remarks>
    /// Call this once during setup, before your main <c>DO...LOOP</c>. You generally
    /// don't need to change it at runtime, though nothing stops you from doing so
    /// (for example, dropping to 30 fps during a heavy scene).
    ///
    /// This works hand-in-hand with <see cref="Sync(VirtualMachine)">sync</see>.
    /// The sync call is what actually yields to let the frame happen, and the rate you
    /// set here determines how long that frame takes. If you never call
    /// <see cref="Sync(VirtualMachine)">sync</see>, this setting has no visible effect.
    /// </remarks>
    /// <example>
    /// Standard 60 fps game loop setup:
    /// <code>
    /// ` set up a 60 fps game loop
    /// set sync rate 16
    /// DO
    ///   ` game logic goes here
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Switch to a slower frame rate for a cutscene:
    /// <code>
    /// ` run at 30 fps during a cutscene, then switch back
    /// set sync rate 33
    /// ` ... play cutscene ...
    /// set sync rate 16
    /// </code>
    /// </example>
    /// <param name="rate">Target elapsed time per frame, in milliseconds. Common values: <c>16</c> (~60 fps), <c>33</c> (~30 fps).</param>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("set sync rate")]
    public static void SetSyncRate(int rate)
    {
        GameSystem.game.TargetElapsedTime = TimeSpan.FromMilliseconds(rate);
    }
    
    /// <summary>
    /// <para>Suspends script execution and lets a render frame happen.</para>
    /// <para>Without this call, nothing you draw, move, or change will ever appear on screen.</para>
    /// </summary>
    /// <remarks>
    /// This is THE core game loop command. You'll typically call it once per iteration
    /// inside a <c>DO...LOOP</c>. Every sprite move, text change, or effect you set up
    /// between syncs becomes visible only after this call fires.
    ///
    /// Pair it with <see cref="SetSyncRate">set sync rate</see> to control how fast
    /// frames tick. You can read <see cref="GameTime">game ms</see> right after a sync
    /// to get the current time for animations, or check
    /// <see cref="Sync()">frame number</see> if you prefer frame-based timing.
    ///
    /// Calling sync twice in a row is harmless; you just get an extra frame with no
    /// changes. Forgetting to call it at all means your script runs to completion and
    /// the window closes (or hangs) without ever rendering.
    /// </remarks>
    /// <example>
    /// Minimal game loop that moves a sprite each frame:
    /// <code>
    /// ` move a sprite to the right, one pixel per frame
    /// set sync rate 16
    /// texture 1, "Images/Ball"
    /// sprite 1, 0, 100, 1
    /// x = 0
    /// DO
    ///   x = x + 1
    ///   sprite 1, x, 100, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <seealso cref="SetSyncRate">set sync rate</seealso>
    /// <seealso cref="GameTime">game ms</seealso>
    /// <seealso cref="Sync()">frame number</seealso>
    [FadeBasicCommand("sync")]
    public static void Sync([FromVm] VirtualMachine vm)
    {
        vm.Suspend();
    }
    
    /// <summary>
    /// <para>Returns the current frame number.</para>
    /// <para>The counter increments by one each time <see cref="Sync(VirtualMachine)">sync</see> is called, starting from zero.</para>
    /// </summary>
    /// <remarks>
    /// Useful for frame-based timing and animations. For example, you can cycle a sprite
    /// sheet every N frames, or trigger an event after a fixed number of updates.
    ///
    /// If you need real wall-clock time instead of frame counts, use
    /// <see cref="GameTime">game ms</see>.
    /// </remarks>
    /// <example>
    /// Cycle a sprite image every 10 frames:
    /// <code>
    /// ` swap between two images every 10 frames
    /// set sync rate 16
    /// texture 1, "Images/Frame1"
    /// texture 2, "Images/Frame2"
    /// sprite 1, 100, 100, 1
    /// DO
    ///   f = frame number()
    ///   ` switch image every 10 frames
    ///   img = (f / 10) mod 2 + 1
    ///   sprite 1, 100, 100, img
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Trigger an event after 120 frames:
    /// <code>
    /// set sync rate 16
    /// DO
    ///   f = frame number()
    ///   IF f = 120 THEN print "two seconds have passed!"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The current frame number. Starts at <c>0</c> and increments by one per sync.</returns>
    /// <seealso cref="Sync">sync</seealso>
    /// <seealso cref="SetSyncRate">set sync rate</seealso>
    /// <seealso cref="GameTime">game ms</seealso>
    /// <seealso cref="Print">print</seealso>
    [FadeBasicCommand("frame number")]
    public static long Sync()
    {
        return GameSystem.currentFrameNumber;
    }
}