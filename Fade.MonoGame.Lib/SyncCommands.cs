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
    /// ` load a texture and place a sprite to draw each frame
    /// texture 1, "ghost"
    /// sprite 1, 320, 240, 1
    /// DO
    ///   ` game logic goes here
    ///   sprite 1, 320, 240, 1
    ///   ` present this frame
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Switch to a slower frame rate for a cutscene, then back to normal:
    /// <code>
    /// ` load a texture so we have something on screen
    /// texture 1, "ghost"
    /// x = 0
    /// ` start the cutscene at 30 fps
    /// set sync rate 33
    /// DO
    ///   x = x + 2
    ///   ` after the ghost drifts past the middle, speed the loop back up
    ///   IF x &gt; 320 THEN set sync rate 16
    ///   sprite 1, x, 240, 1
    ///   ` present this frame
    ///   sync
    /// LOOP
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
    /// texture 1, "ghost"
    /// sprite 1, 0, 100, 1
    /// x = 0
    /// DO
    ///   x = x + 1
    ///   sprite 1, x, 100, 1
    ///   ` present this frame so the movement is visible
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
        if (DebugUISystem.autoInspectorEnabled)
        {
            DebugUISystem.Push(new DebugUICommand
            {
                vmInstructionIndex = vm.instructionIndex,
                label = "Debug",
                type = DebugControlType.WINDOW_START
            });
            DebugUISystem.Push(new DebugUICommand
            {
                vmInstructionIndex = vm.instructionIndex,
                label = "auto_inspector",
                type = DebugControlType.INSPECTOR
            });
            DebugUISystem.Push(new DebugUICommand
            {
                vmInstructionIndex = vm.instructionIndex,
                type = DebugControlType.WINDOW_END
            });
        }
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
    /// Hop a sprite between two spots every 10 frames using the frame counter:
    /// <code>
    /// ` load a texture to animate
    /// set sync rate 16
    /// texture 1, "ghost"
    /// sprite 1, 100, 100, 1
    /// DO
    ///   f = frame number()
    ///   ` switch position every 10 frames (alternates 0 then 1)
    ///   s = (f / 10) mod 2
    ///   x = 100 + s * 80
    ///   sprite 1, x, 100, 1
    ///   ` present this frame
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Trigger an event after 120 frames, showing the result on screen:
    /// <code>
    /// set sync rate 16
    /// ` load a font so we can draw a message
    /// font 1, "font"
    /// msg$ = "waiting..."
    /// DO
    ///   f = frame number()
    ///   ` after 120 frames (~2 seconds at 60 fps) change the message
    ///   IF f = 120 THEN msg$ = "two seconds have passed!"
    ///   text 1, 100, 100, 1, msg$
    ///   ` present this frame
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