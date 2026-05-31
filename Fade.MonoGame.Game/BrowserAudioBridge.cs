using System;

namespace Fade.MonoGame.Core;

// Browser-only audio backend. Replaces MonoGame's SoundEffect /
// SoundEffectInstance on the BlazorGL platform with a direct Web Audio
// API path: the iframe's window.fadeAudio handles all decode and
// playback, and AudioCommands.cs's BROWSER branches route through the
// delegates below.
//
// Wired up at Index.razor.cs:WireCooperativePump — each delegate becomes
// an IJSInProcessRuntime.InvokeVoid (or InvokeUnmarshalled for returns).
// Library commands call into these the same way HostBridge.PostMessage
// works today, so the FadeBasicCommand surface stays unchanged.
//
// Hooks default to no-op stubs so a library command on this path
// gracefully degrades when the bridge isn't wired (e.g. running unit
// tests outside Blazor) — same convention as HostBridge.
public static class BrowserAudioBridge
{
    // ── Clip / instance lifecycle ────────────────────────────────────
    // Mirrors AudioCommands' bookkeeping (highestClipId, highestEffectId,
    // reserve, get-free) without keeping a parallel store on the C# side.

    /// <summary>Associates a clip slot with a previously-registered audio
    /// asset name. Returns true on success.</summary>
    public static Func<int, string, bool> LoadClip = (_, _) => false;

    /// <summary>Reserves a clip slot id so subsequent `get free sfx
    /// clip next id` sees it as taken.</summary>
    public static Action<int> ReserveClipId = _ => { };

    /// <summary>Highest clip id seen so far; drives `get free sfx clip
    /// next id` / `reserve sfx clip next id`.</summary>
    public static Func<int> GetHighestClipId = () => 0;

    /// <summary>Creates a per-instance audio graph (gain + panner +
    /// settings) tied to the given clip. Returns true on success.</summary>
    public static Func<int, int, bool> CreateInstance = (_, _) => false;

    /// <summary>Highest instance id seen so far.</summary>
    public static Func<int> GetHighestInstanceId = () => 0;

    public static Action<int> ReserveInstanceId = _ => { };

    // ── Playback control ─────────────────────────────────────────────
    public static Action<int> Play = _ => { };
    public static Action<int, int> PlayWithDelay = (_, _) => { };
    public static Action<int> Pause = _ => { };
    public static Action<int> Stop = _ => { };

    // ── Settings ─────────────────────────────────────────────────────
    public static Action<int, float> SetVolume = (_, _) => { };
    public static Func<int, float> GetVolume = _ => 0f;
    public static Action<int, float> SetPitch = (_, _) => { };
    public static Func<int, float> GetPitch = _ => 0f;
    public static Action<int, float> SetPan = (_, _) => { };
    public static Func<int, float> GetPan = _ => 0f;
    public static Action<int, bool> SetLoop = (_, _) => { };

    public static Func<int, bool> IsDone = _ => true;

    // ── Session lifecycle ────────────────────────────────────────────
    /// <summary>Halts every active source without disposing the per-
    /// instance graph — Stop button. Matches AudioInstanceSystem.StopAll().</summary>
    public static Action StopAll = () => { };

    /// <summary>Wipes everything (buffers + clip map + instance map).
    /// Called between Runs.</summary>
    public static Action Reset = () => { };
}
