using System;
using System.Collections.Generic;

namespace Fade.MonoGame.Core;

/// <summary>
/// What a group contributes to every sound in it. Volume multiplies; pitch and pan add.
/// </summary>
public struct AudioGroup
{
    public float volume;
    public float pitch;
    public float pan;
}

/// <summary>
/// What a program asked for on ONE sound, before its group is applied.
/// </summary>
public struct AudioMixSettings
{
    public int group;
    public float volume;
    public float pitch;
    public float pan;
}

/// <summary>
/// Sound groups: every sound instance belongs to one group, and what is actually heard is the
/// sound's own volume, pitch and pan combined with its group's.
///
/// The sound's own settings are kept HERE rather than read back from the playing instance,
/// because the instance only ever holds the combined value. Reading that back as "the volume
/// the program set" would bake the group in: a fade written as `vol = sfx volume(id)` then
/// `set sfx volume id, vol + step` would compound the group's multiplier into the sound every
/// frame. Keeping the two apart is also what lets a group change re-mix sounds that are
/// already playing.
///
/// Platform independent on purpose. Desktop pushes the combined value into the
/// SoundEffectInstance and the browser pushes it through BrowserAudioBridge, but the
/// bookkeeping is the same, so groups behave identically on both.
/// </summary>
public static class AudioMixSystem
{
    /// <summary>The group a sound is in until it is moved. Group 0 is "everything".</summary>
    public const int DEFAULT_GROUP = 0;

    private static readonly Dictionary<int, AudioGroup> _groups = new Dictionary<int, AudioGroup>();
    private static readonly Dictionary<int, AudioMixSettings> _sounds = new Dictionary<int, AudioMixSettings>();

    public static void Reset()
    {
        _groups.Clear();
        _sounds.Clear();
    }

    /// <summary>A group that was never touched is neutral: it changes nothing.</summary>
    public static AudioGroup GetGroup(int groupId)
    {
        return _groups.TryGetValue(groupId, out var group)
            ? group
            : new AudioGroup { volume = 1, pitch = 0, pan = 0 };
    }

    /// <summary>
    /// Store the group, then re-mix every sound in it so the change is heard immediately,
    /// including on sounds that are already playing.
    /// </summary>
    public static void SetGroup(int groupId, AudioGroup group)
    {
        _groups[groupId] = group;

        // Collected first: Apply does not touch _sounds today, but iterating a dictionary
        // while calling out of it is one innocent edit away from an InvalidOperationException.
        var ids = new List<int>();
        foreach (var pair in _sounds)
        {
            if (pair.Value.group == groupId) ids.Add(pair.Key);
        }

        foreach (var sfxId in ids)
        {
            Apply(sfxId);
        }
    }

    /// <summary>A sound that was never touched is at full volume, unshifted, centred, in group 0.</summary>
    public static AudioMixSettings GetSound(int sfxId)
    {
        return _sounds.TryGetValue(sfxId, out var sound)
            ? sound
            : new AudioMixSettings { group = DEFAULT_GROUP, volume = 1, pitch = 0, pan = 0 };
    }

    public static void SetSound(int sfxId, AudioMixSettings sound)
    {
        _sounds[sfxId] = sound;
        Apply(sfxId);
    }

    /// <summary>
    /// Called when `sfx` builds a fresh instance for an id. The new instance starts at full
    /// volume, unshifted and centred, exactly as it always has -- but it STAYS in its group,
    /// and the group is applied straight away. Without this a program that re-creates a
    /// sound before every play (a common way to pick a random clip) would escape its group
    /// each time.
    /// </summary>
    public static void OnInstanceCreated(int sfxId)
    {
        var sound = GetSound(sfxId);
        sound.volume = 1;
        sound.pitch = 0;
        sound.pan = 0;
        SetSound(sfxId, sound);
    }

    /// <summary>Push the combined sound-and-group values to whatever is actually playing.</summary>
    public static void Apply(int sfxId)
    {
        var sound = GetSound(sfxId);
        var group = GetGroup(sound.group);

        var volume = Math.Clamp(sound.volume * group.volume, 0f, 1f);
        var pitch = Math.Clamp(sound.pitch + group.pitch, -1f, 1f);
        var pan = Math.Clamp(sound.pan + group.pan, -1f, 1f);

#if BROWSER
        BrowserAudioBridge.SetVolume(sfxId, volume);
        BrowserAudioBridge.SetPitch(sfxId, pitch);
        BrowserAudioBridge.SetPan(sfxId, pan);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out _);
        var instance = AudioInstanceSystem.audioEffects[index].instance;

        // Settings can arrive before `sfx` has built the instance (a group assigned at
        // reserve time, say). They are kept above and applied when the instance appears.
        if (instance == null) return;

        instance.Volume = volume;
        instance.Pitch = pitch;
        instance.Pan = pan;
#endif
    }
}
