using System;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>The graphics backend this build is actually running on: "OpenGL" or "Vulkan".</para>
    /// </summary>
    /// <remarks>
    /// Read at runtime from the MonoGame assembly that is loaded, not from a build
    /// flag, so it cannot disagree with reality — which is the point. The backend is
    /// chosen by which MonoGame package the game references, and the two produce an
    /// identically named assembly, so "which one am I running" is otherwise very hard
    /// to answer by eye. Show it in a debug panel when you want to be sure.
    /// </remarks>
    /// <example>
    /// Show it in the debug window:
    /// <code>
    /// begin debug window "Render"
    ///     debug label "backend", graphics backend$()
    /// end debug window
    /// </code>
    /// </example>
    [FadeBasicCommand("graphics backend$")]
    public static string GetGraphicsBackend()
    {
        return GraphicsBackend.Name;
    }

    /// <summary>
    /// <para>Prints one or more values to the console output.</para>
    /// <para>Each value is printed on its own line, so passing three values gives you three lines of output.</para>
    /// </summary>
    /// <remarks>
    /// This is your go-to debug command. You can call it from macros or at runtime
    /// (it works in both contexts), which makes it handy for inspecting values during
    /// compilation as well as while the game is running.
    ///
    /// Since it writes to the console, you won't see anything if your game doesn't have
    /// a console window attached. It pairs naturally with
    /// <see cref="GameTime">game ms</see> if you want to timestamp your debug output,
    /// and with <see cref="Test">test</see> when you just need to dump a single int quickly.
    /// </remarks>
    /// <example>
    /// Print a simple message and a variable:
    /// <code>
    /// ` print writes each value to the console on its own line
    /// score = 42
    /// print "hello world"
    /// print score
    /// ` load a font so we can also show the score on the game canvas
    /// font 1, "font"
    /// set sync rate 16
    /// DO
    ///   ` keep printing the score to the console every frame
    ///   print score
    ///   ` and draw a label on the canvas so something is visible
    ///   text 1, 100, 100, 1, "SCORE 42"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Timestamp debug output with <see cref="GameTime">game ms</see>:
    /// <code>
    /// set sync rate 16
    /// ` load a font so the timestamp is visible on the canvas too
    /// font 1, "font"
    /// DO
    ///   ` game ms() gives a fresh timestamp every frame
    ///   t = game ms()
    ///   print t
    ///   text 1, 100, 100, 1, "RUNNING"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="values">One or more values of any type to print. Each value becomes its own line.</param>
    /// <seealso cref="GameTime">game ms</seealso>
    /// <seealso cref="Test">test</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("print", FadeBasicCommandUsage.Both)]
    public static void Print(params object[] values)
    {
        foreach (var value in values)
        {
            Console.WriteLine(value);
        }
    }

     
    /// <summary>
    /// <para>Returns the total elapsed game time in milliseconds.</para>
    /// <para>This keeps ticking regardless of what your script is doing. It reflects wall-clock time since the game started, not script time.</para>
    /// </summary>
    /// <remarks>
    /// Call this every frame (after <see cref="Sync(VirtualMachine)">sync</see>) when you
    /// need to drive animations, timers, or custom tweens by real elapsed time instead of
    /// frame counts. Because it is millisecond-resolution, you can do smooth interpolation
    /// without worrying about frame-rate jitter.
    ///
    /// If you only need to know how many frames have passed, use
    /// <see cref="Sync()">frame number</see> instead. And if you are building a tween that
    /// uses angles, the trig helpers like <see cref="Sin">sin</see> and
    /// <see cref="Cos">cos</see> pair well with a time value converted to radians.
    /// </remarks>
    /// <example>
    /// Use game time to move a sprite smoothly across the screen:
    /// <code>
    /// ` move a sprite based on elapsed time
    /// set sync rate 16
    /// texture 1, "ghost"
    /// sprite 1, 0, 100, 1
    /// DO
    ///   t = game ms()
    ///   x = t / 10
    ///   sprite 1, x, 100, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Build a simple countdown timer:
    /// <code>
    /// ` count down from 5 seconds
    /// set sync rate 16
    /// ` load a font so the countdown is visible on the canvas
    /// font 1, "font"
    /// startTime = game ms()
    /// DO
    ///   elapsed = game ms() - startTime
    ///   remaining = 5000 - elapsed
    ///   IF remaining &lt; 0
    ///     remaining = 0
    ///   ENDIF
    ///   print remaining
    ///   text 1, 100, 100, 1, "COUNTDOWN"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>Total game time in milliseconds.</returns>
    /// <seealso cref="Sync">sync</seealso>
    /// <seealso cref="SetSyncRate">set sync rate</seealso>
    /// <seealso cref="Print">print</seealso>
    /// <seealso cref="Sync()">frame number</seealso>
    /// <seealso cref="Sin">sin</seealso>
    /// <seealso cref="Cos">cos</seealso>
    [FadeBasicCommand("game ms")]
    public static double GameTime()
    {
        return GameSystem.latestTime.TotalGameTime.TotalMilliseconds;
    }

    /// <summary>
    /// <para>Wall-clock milliseconds from a high-resolution monotonic clock. Use it to measure
    /// how long a section of your own code takes.</para>
    /// </summary>
    /// <remarks>
    /// <see cref="GameTime">game ms</see> cannot do this, and the reason is worth knowing: it
    /// reads the GameTime SNAPSHOT taken when the frame began, so it returns the SAME value
    /// everywhere inside one frame and a difference across a section is always exactly zero.
    /// Under a fixed timestep it is worse than useless for timing -- it advances by the target
    /// frame interval whatever the frame actually cost.
    ///
    /// This is Stopwatch, so it is monotonic and sub-microsecond, and the origin is arbitrary:
    /// only DIFFERENCES between two readings mean anything.
    ///
    /// Store it in a FLOAT (`ms# = wall ms()`). An unsuffixed variable is a 32-bit int, so it
    /// would truncate away exactly the fractional milliseconds worth measuring.
    /// </remarks>
    /// <returns>Milliseconds since an arbitrary origin, as a fractional value.</returns>
    /// <seealso cref="GameTime">game ms</seealso>
    [FadeBasicCommand("wall ms")]
    public static double WallMs()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp()
             * (1000.0 / System.Diagnostics.Stopwatch.Frequency);
    }

    /// <summary>
    /// <para>Marks the end of one part of the frame, for the frame timing log. The time since the
    /// lap before it is given to the section with this number.</para>
    /// </summary>
    /// <remarks>
    /// Put one between the parts of your main loop. Run the game with the FADE_PERF_LOG environment
    /// variable set, and every few seconds the log lists what each section cost per frame, on
    /// average and in its worst frame. Without the variable a lap does nothing, so they can stay in.
    /// </remarks>
    /// <param name="section">Which section the time goes to, from 0 to 63.</param>
    /// <seealso cref="PerfName">perf name</seealso>
    /// <seealso cref="WallMs">wall ms</seealso>
    [FadeBasicCommand("perf lap")]
    public static void PerfLap(int section)
    {
        Fade.MonoGame.Core.PerfSections.Lap(section);
    }

    /// <summary>
    /// <para>Gives a section of the frame timing log a name to be listed under.</para>
    /// </summary>
    /// <param name="section">The number of the section, from 0 to 63.</param>
    /// <param name="name">What the log calls it.</param>
    /// <seealso cref="PerfLap">perf lap</seealso>
    [FadeBasicCommand("perf name")]
    public static void PerfName(int section, string name)
    {
        Fade.MonoGame.Core.PerfSections.Name(section, name);
    }

    /// <summary>
    /// <para>Makes every update take this many milliseconds longer, to see the game the way a slow
    /// machine does. 0 turns it off.</para>
    /// </summary>
    /// <remarks>
    /// It is a busy wait, so the thread is as occupied as a slow CPU would keep it. With a fixed
    /// timestep, once an update and a draw take longer than a step, MonoGame runs extra updates to
    /// keep up, and past a point it never draws: see <see cref="SetPerfMaxCatchUp">set perf max catch up</see>.
    /// </remarks>
    /// <param name="ms">Milliseconds added to every update.</param>
    [FadeBasicCommand("set perf slow update")]
    public static void SetPerfSlowUpdate(float ms) => PerfKnobs.updateDelayMs = Math.Max(0, ms);

    /// <summary><para>The milliseconds that <see cref="SetPerfSlowUpdate">set perf slow update</see> adds to every update.</para></summary>
    [FadeBasicCommand("perf slow update")]
    public static float PerfSlowUpdate() => (float)PerfKnobs.updateDelayMs;

    /// <summary>
    /// <para>Makes every draw take this many milliseconds longer, to see the game the way a machine
    /// with a slow graphics card does. 0 turns it off.</para>
    /// </summary>
    /// <param name="ms">Milliseconds added to every draw.</param>
    [FadeBasicCommand("set perf slow draw")]
    public static void SetPerfSlowDraw(float ms) => PerfKnobs.drawDelayMs = Math.Max(0, ms);

    /// <summary><para>The milliseconds that <see cref="SetPerfSlowDraw">set perf slow draw</see> adds to every draw.</para></summary>
    [FadeBasicCommand("perf slow draw")]
    public static float PerfSlowDraw() => (float)PerfKnobs.drawDelayMs;

    /// <summary>
    /// <para>How many updates MonoGame may run to catch up before it has to draw. 0 is MonoGame's
    /// own limit, which is as many as half a second of falling behind needs (30). It is 3 unless something sets it.</para>
    /// </summary>
    /// <remarks>
    /// On a machine that cannot keep up, MonoGame's own limit keeps the game at full speed and
    /// draws about twice a second. A small limit draws far more often, and the game runs slower
    /// than real time instead. A machine that keeps up never gets near either.
    /// </remarks>
    /// <param name="updates">The most updates in a row, from 1. 0 for MonoGame's own.</param>
    [FadeBasicCommand("set perf max catch up")]
    public static void SetPerfMaxCatchUp(int updates) => PerfKnobs.maxCatchUp = Math.Max(0, updates);

    /// <summary><para>The limit that <see cref="SetPerfMaxCatchUp">set perf max catch up</see> set, or 0.</para></summary>
    [FadeBasicCommand("perf max catch up")]
    public static int PerfMaxCatchUp() => PerfKnobs.maxCatchUp;

    /// <summary>
    /// <para>How many pictures were really drawn in the last second.</para>
    /// </summary>
    /// <remarks>
    /// Not the same as one over the frame time of <see cref="GameTime">game ms</see>: with a fixed
    /// timestep that is always one step, 60 a second, however few pictures are drawn.
    /// </remarks>
    [FadeBasicCommand("perf drawn per second")]
    public static float PerfDrawnPerSecond() => (float)PerfKnobs.drawnPerSecond;

    /// <summary>
    /// <para>How many updates ran for every picture drawn, over the last second. 1 on a machine that
    /// keeps up; more when MonoGame is catching up.</para>
    /// </summary>
    [FadeBasicCommand("perf updates per draw")]
    public static float PerfUpdatesPerDraw() => (float)PerfKnobs.updatesPerDraw;

    [FadeBasicCommand("go kaboom")]
    public static void Throw()
    {
        throw new InvalidOperationException("Kaboom!");
    }
    
}