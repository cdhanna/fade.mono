using System;
using System.Diagnostics;
using System.Text;

namespace Fade.MonoGame.Core;

/// <summary>
/// Where the time of a frame of the Fade program goes, for the frame timing log (FADE_PERF_LOG).
///
/// The program calls `perf lap n` between the parts of its frame. Each call gives the time since
/// the call before it to section n, so a frame is cut into sections with one command between
/// each. The log prints, every few seconds, what each section cost per frame on average and in
/// its worst frame. With the log off, a lap does nothing.
/// </summary>
public static class PerfSections
{
    public const int MAX = 64;

    public static bool enabled;

    private static readonly string[] _names = new string[MAX];
    private static readonly long[] _frameTicks = new long[MAX];
    private static readonly long[] _sumTicks = new long[MAX];
    private static readonly long[] _maxTicks = new long[MAX];
    private static long _last;
    private static int _frames;

    public static void Name(int section, string name)
    {
        if (section >= 0 && section < MAX) _names[section] = name;
    }

    public static void Lap(int section)
    {
        if (!enabled) return;
        var now = Stopwatch.GetTimestamp();
        if (_last != 0 && section >= 0 && section < MAX) _frameTicks[section] += now - _last;
        _last = now;
    }

    /// <summary>The program's frame is about to start: nothing before this belongs to a section.</summary>
    public static void BeginFrame()
    {
        if (enabled) _last = Stopwatch.GetTimestamp();
    }

    public static void EndFrame()
    {
        if (!enabled) return;
        _frames++;
        for (var i = 0; i < MAX; i++)
        {
            var ticks = _frameTicks[i];
            if (ticks == 0) continue;
            _sumTicks[i] += ticks;
            if (ticks > _maxTicks[i]) _maxTicks[i] = ticks;
            _frameTicks[i] = 0;
        }
    }

    /// <summary>One line per section that was used, most expensive first, and start counting again.</summary>
    public static string Report()
    {
        if (_frames == 0) return null;
        var toMs = 1000.0 / Stopwatch.Frequency;
        var order = new int[MAX];
        for (var i = 0; i < MAX; i++) order[i] = i;
        Array.Sort(order, (a, b) => _sumTicks[b].CompareTo(_sumTicks[a]));

        var text = new StringBuilder();
        foreach (var i in order)
        {
            if (_sumTicks[i] == 0) continue;
            var name = _names[i] ?? ("section " + i);
            text.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"[perf]   {name,-28} avg {_sumTicks[i] * toMs / _frames,6:F3} ms  worst {_maxTicks[i] * toMs,6:F2} ms\n"));
        }

        Array.Clear(_sumTicks);
        Array.Clear(_maxTicks);
        var frames = _frames;
        _frames = 0;
        // A frame is a drawn picture, with every update that MonoGame ran before it.
        return text.Length == 0 ? null : $"[perf] sections over {frames} frames, per frame (all of its updates):\n" + text;
    }
}
