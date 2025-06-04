using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    [FadeBasicCommand("free sfx clip id")]
    public static int GetFreeSfxClipNextId(ref int sfxClipId)
    {
        sfxClipId = AudioSystem.highestClipId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return sfxClipId;
    }
    
    [FadeBasicCommand("reserve sfx clip id")]
    public static int ReserveSfxClipNextId(ref int sfxClipId)
    {
        GetFreeSfxClipNextId(ref sfxClipId);
        AudioSystem.GetAudioEffectIndex(sfxClipId, out _, out _);
        return sfxClipId;
    }

    
    [FadeBasicCommand("free sfx id")]
    public static int GetFreeSfxNextId(ref int sfxId)
    {
        sfxId = AudioInstanceSystem.highestEffectId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return sfxId;
    }
    
    [FadeBasicCommand("reserve sfx id")]
    public static int ReserveSfxNextId(ref int sfxId)
    {
        GetFreeSfxNextId(ref sfxId);
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out _, out _);
        return sfxId;
    }
    
    [FadeBasicCommand("load sfx clip")]
    public static void LoadSoundEffect(int clipId, string path)
    {
        AudioSystem.LoadSfxFromContent(clipId, path);
    }

    [FadeBasicCommand("sfx")]
    public static void CreateSoundEffect(int sfxId, int clipId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioSystem.GetAudioEffectIndex(clipId, out _, out var clip);
        sfx.instance = clip.source.CreateInstance();
        AudioInstanceSystem.audioEffects[index] = sfx;
    }
    
    [FadeBasicCommand("pause sfx")]
    public static void PauseSfx(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pause();
    }
    
    
    [FadeBasicCommand("play sfx")]
    public static void PlaySfx(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Stop();
        AudioInstanceSystem.audioEffects[index].instance.Play();
        
    }
    
    [FadeBasicCommand("delay play sfx")]
    public static void PlaySfx(int sfxId, int delayMs)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Stop();
        sfx.playingDelayedUntil = AudioInstanceSystem.currentTime + delayMs;
        AudioInstanceSystem.audioEffects[index] = sfx;
    }
    
    [FadeBasicCommand("set sfx pitch")]
    public static void SetSfxPitch(int sfxId, float pitch)
    {
        if (pitch >= 1) pitch = 1;
        if (pitch <= -1) pitch = -1;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pitch = pitch;
    }
    
     
    [FadeBasicCommand("sfx pitch")]
    public static float GetSfxPitch(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pitch;
    }
    
    
    [FadeBasicCommand("set sfx pan")]
    public static void SetSfxPan(int sfxId, float pan)
    {
        
        if (pan >= 1) pan = 1;
        if (pan <= -1) pan = -1;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pan = pan;
    }
    
    [FadeBasicCommand("sfx pan")]
    public static float GetSfxPan(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pan;
    }
    
    
    [FadeBasicCommand("set sfx volume")]
    public static void SetSfxVolume(int sfxId, float volume)
    {
        if (volume >= 1) volume = 1;
        if (volume <= 0) volume = 0;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Volume = volume;
    }
    
    [FadeBasicCommand("sfx volume")]
    public static float GetSfxVolume(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Volume;
    }
    
    
    [FadeBasicCommand("set sfx loop")]
    public static void SetSfxLoop(int sfxId, bool isLooped)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.IsLooped = isLooped;
    }
    //
    
    [FadeBasicCommand("is sfx done")]
    public static bool IsSfxDone(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.State == SoundState.Stopped;
    }
    
}