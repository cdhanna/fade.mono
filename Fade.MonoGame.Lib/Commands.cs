using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
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
    /// ` print a greeting and the player's score
    /// score = 42
    /// print "hello world"
    /// print score
    /// </code>
    /// </example>
    /// <example>
    /// Timestamp debug output with <see cref="GameTime">game ms</see>:
    /// <code>
    /// set sync rate 16
    /// DO
    ///   t = game ms()
    ///   print t
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
    /// texture 1, "Images/Ship"
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
    /// startTime = game ms()
    /// DO
    ///   elapsed = game ms() - startTime
    ///   remaining = 5000 - elapsed
    ///   IF remaining &lt; 0 THEN remaining = 0
    ///   print remaining
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
    /// <para>Prints a single integer to the console.</para>
    /// <para>This is a bare-bones debug helper. Use <see cref="Print">print</see> if you need to output arbitrary types.</para>
    /// </summary>
    /// <remarks>
    /// Exists mainly for quick sanity checks during development. If you find yourself reaching
    /// for this a lot, <see cref="Print">print</see> is more flexible since it accepts any type
    /// and any number of values.
    /// </remarks>
    /// <example>
    /// Quick check that a calculation is correct:
    /// <code>
    /// ` verify a value during development
    /// health = 100 - 25
    /// test health
    /// </code>
    /// </example>
    /// <param name="x">The integer value to print.</param>
    /// <seealso cref="Print">print</seealso>
    [FadeBasicCommand("test")]
    public static void Test(int x)
    {
        Console.WriteLine(x);
    }
}