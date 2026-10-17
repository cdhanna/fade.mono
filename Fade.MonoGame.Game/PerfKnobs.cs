using System;
using System.Diagnostics;
using System.Threading;

namespace Fade.MonoGame.Core;

/// <summary>
/// Knobs for seeing the game the way a slow machine does, and for how the engine copes with one.
///
/// A slow machine is emulated by burning time: every update, and every draw, busy-waits this many
/// milliseconds on top of its real work. It is a busy wait and not a sleep, so the thread is as
/// occupied as a slow CPU would keep it.
///
/// maxCatchUp is how many updates MonoGame may run before it has to draw again. With a fixed
/// timestep MonoGame keeps the game's clock with the real one: when a frame takes longer than one
/// step it runs extra updates to make the difference up. On a machine where one update costs more
/// than one step, that can never be made up, and it runs updates without drawing until the 500 ms
/// that MonoGame allows at most (MaxElapsedTime) is used up: two frames a second, at full speed.
/// A limit of n draws after at most n updates instead, and the game runs slower than real time.
/// 0 leaves MonoGame's 500 ms.
///
/// The fish game's debug window has sliders for all three, and the launch options
/// --slow-update, --slow-draw and --max-catchup set them from the start (Program.cs).
/// </summary>
public static class PerfKnobs
{
    public static double updateDelayMs;
    public static double drawDelayMs;
    public static int maxCatchUp;

    static readonly TimeSpan _monoGameDefault = TimeSpan.FromMilliseconds(500);

    /// <summary>What MonoGame's MaxElapsedTime should be, for a step of this length.</summary>
    public static TimeSpan MaxElapsedTime(TimeSpan step)
    {
        return maxCatchUp > 0 ? TimeSpan.FromTicks(step.Ticks * maxCatchUp) : _monoGameDefault;
    }

    // How the machine is really doing, over the last second, whether or not the frame timing log
    // is on. GameTime cannot say: with a fixed timestep its elapsed time is always one step.
    public static double drawnPerSecond;
    public static double updatesPerDraw;
    static int _draws, _updates;
    static long _windowStart;

    public static void CountUpdate() => _updates++;

    public static void CountDraw()
    {
        _draws++;
        var now = Stopwatch.GetTimestamp();
        if (_windowStart == 0) _windowStart = now;
        var seconds = (now - _windowStart) / (double)Stopwatch.Frequency;
        if (seconds < 1) return;
        drawnPerSecond = _draws / seconds;
        updatesPerDraw = _updates / (double)_draws;
        _draws = _updates = 0;
        _windowStart = now;
    }

    public static void BusyWait(double ms)
    {
        if (ms <= 0) return;
        var end = Stopwatch.GetTimestamp() + (long)(ms * Stopwatch.Frequency / 1000.0);
        while (Stopwatch.GetTimestamp() < end) Thread.SpinWait(20);
    }
}
