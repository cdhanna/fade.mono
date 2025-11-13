using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Fade.MonoGame.Game;


public struct RuntimeSfxClip
{
    public int id;
    public SoundEffect source;
}

public struct AudioInstance
{
    public int id;
    public SoundEffectInstance instance;

    public double playingDelayedUntil;
}


public static class AudioSystem
{
    public static List<RuntimeSfxClip> sfxClips = new List<RuntimeSfxClip>();
    private static Dictionary<int, int> _clipMap = new Dictionary<int, int>();
    public static int highestClipId;

    public static void Reset()
    {
        sfxClips.Clear();
        _clipMap.Clear();
        highestClipId = 0;
    }
    
    public static void GetAudioEffectIndex(int sfxClipId, out int index, out RuntimeSfxClip sfxClipEffect)
    {
        if (!_clipMap.TryGetValue(sfxClipId, out index))
        {
            highestClipId = sfxClipId > highestClipId ? sfxClipId : highestClipId;
            index = _clipMap[sfxClipId] = sfxClips.Count;
            sfxClipEffect = new RuntimeSfxClip()
            {
                id = sfxClipId,
            };
            sfxClips.Add(sfxClipEffect);
        }
        else
        {
            sfxClipEffect = sfxClips[index];
        }
    }
    
    public static void LoadSfxFromContent(int audioId, string path)
    {
        var clip = GameSystem.game.Content.Load<SoundEffect>(path);
        GetAudioEffectIndex(audioId, out var index, out var runtimeAudio);

        runtimeAudio.source = clip;
        sfxClips[index] = runtimeAudio;
    }
}


public static class AudioInstanceSystem
{
    public static List<AudioInstance> audioEffects = new List<AudioInstance>();
    private static Dictionary<int, int> _effectMap = new Dictionary<int, int>();
    public static int highestEffectId;
    
    public static double currentTime;
    
    public static void Reset()
    {

        foreach (var audio in audioEffects)
        {
            audio.instance?.Dispose();
        }
        
        audioEffects.Clear();
        _effectMap.Clear();
        highestEffectId = 0;
        currentTime = 0;
    }
    
    public static void GetAudioEffectIndex(int audioEffectId, out int index, out AudioInstance audioEffect)
    {
        if (!_effectMap.TryGetValue(audioEffectId, out index))
        {
            highestEffectId = audioEffectId > highestEffectId ? audioEffectId : highestEffectId;
            index = _effectMap[audioEffectId] = audioEffects.Count;
            audioEffect = new AudioInstance()
            {
                id = audioEffectId,
            };
            audioEffects.Add(audioEffect);
        }
        else
        {
            audioEffect = audioEffects[index];
        }
    }

    public static void HandleAudio()
    {
        // foreach (var sfx in audioEffects)
        for (var i = 0 ; i < audioEffects.Count; i ++)
        {
            var sfx = audioEffects[i];
            if (sfx.instance?.State == SoundState.Stopped && sfx.playingDelayedUntil != 0)
            {
                if (currentTime > sfx.playingDelayedUntil)
                {
                    sfx.instance.Play();
                    sfx.playingDelayedUntil = 0;
                    audioEffects[i] = sfx;
                }
            }
        }
    }
}