using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Peeks at the next available sound effect clip ID without claiming it.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveSfxClipNextId">reserve sfx clip id</see>
    /// instead, which actually claims the slot. This is the "peek" half of the peek-vs-claim
    /// pattern. If you already know your ID, skip both and call
    /// <see cref="LoadSoundEffect">load sfx clip</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next clip ID to see what it would be:
    /// <code>
    /// ` check what ID would be assigned next
    /// nextClipId = free sfx clip id(nextClipId)
    /// </code>
    /// </example>
    /// <param name="sfxClipId">Receives the next free clip ID.</param>
    /// <returns>The next available clip ID (not yet reserved).</returns>
    /// <seealso cref="ReserveSfxClipNextId">reserve sfx clip id</seealso>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    [FadeBasicCommand("free sfx clip id")]
    public static int GetFreeSfxClipNextId(ref int sfxClipId)
    {
        sfxClipId = AudioSystem.highestClipId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return sfxClipId;
    }

    /// <summary>
    /// <para>Claims the next available sound effect clip ID and initializes its slot.</para>
    /// <para>Use this when you need to wire up references before loading the actual audio data.</para>
    /// </summary>
    /// <remarks>
    /// The "claim" half of the peek-vs-claim pattern. After reserving, load the audio data
    /// with <see cref="LoadSoundEffect">load sfx clip</see>. See also
    /// <see cref="GetFreeSfxClipNextId">free sfx clip id</see> if you only need to peek.
    /// </remarks>
    /// <example>
    /// Reserve a clip ID, then load audio into it:
    /// <code>
    /// ` reserve a slot and load a sound effect clip
    /// clipId = reserve sfx clip id(clipId)
    /// load sfx clip clipId, "audio/laser"
    /// </code>
    /// </example>
    /// <param name="sfxClipId">Receives the reserved clip ID.</param>
    /// <returns>The newly reserved clip ID.</returns>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    /// <seealso cref="GetFreeSfxClipNextId">free sfx clip id</seealso>
    [FadeBasicCommand("reserve sfx clip id")]
    public static int ReserveSfxClipNextId(ref int sfxClipId)
    {
        GetFreeSfxClipNextId(ref sfxClipId);
        AudioSystem.GetAudioEffectIndex(sfxClipId, out _, out _);
        return sfxClipId;
    }


    /// <summary>
    /// <para>Peeks at the next available sound effect instance ID without claiming it.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveSfxNextId">reserve sfx id</see>
    /// instead, which actually claims the slot. If you already know your ID, skip both and
    /// call <see cref="CreateSoundEffect">sfx</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next instance ID:
    /// <code>
    /// ` check what instance ID would be assigned next
    /// nextSfxId = free sfx id(nextSfxId)
    /// </code>
    /// </example>
    /// <param name="sfxId">Receives the next free instance ID.</param>
    /// <returns>The next available instance ID (not yet reserved).</returns>
    /// <seealso cref="ReserveSfxNextId">reserve sfx id</seealso>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    [FadeBasicCommand("free sfx id")]
    public static int GetFreeSfxNextId(ref int sfxId)
    {
        sfxId = AudioInstanceSystem.highestEffectId + 1;
        // TextureSystem.GetTextureIndex(textureId, out _, out _);
        return sfxId;
    }

    /// <summary>
    /// <para>Claims the next available sound effect instance ID and initializes its slot.</para>
    /// <para>Use this when you need to wire up references before creating the actual instance.</para>
    /// </summary>
    /// <remarks>
    /// The "claim" half of the peek-vs-claim pattern. After reserving, create the instance
    /// with <see cref="CreateSoundEffect">sfx</see>. See also
    /// <see cref="GetFreeSfxNextId">free sfx id</see> if you only need to peek.
    /// </remarks>
    /// <example>
    /// Reserve an instance ID, then create the instance from a loaded clip:
    /// <code>
    /// ` reserve the instance slot first, then create it
    /// mysfxId = reserve sfx id(mysfxId)
    /// sfx mysfxId, clipId
    /// </code>
    /// </example>
    /// <param name="sfxId">Receives the reserved instance ID.</param>
    /// <returns>The newly reserved instance ID.</returns>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="GetFreeSfxNextId">free sfx id</seealso>
    [FadeBasicCommand("reserve sfx id")]
    public static int ReserveSfxNextId(ref int sfxId)
    {
        GetFreeSfxNextId(ref sfxId);
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out _, out _);
        return sfxId;
    }

    /// <summary>
    /// <para>Loads a sound effect clip from the content pipeline.</para>
    /// <para>A clip is the raw audio data. Think of it as the sound file itself. You
    /// need to create an instance from it with <see cref="CreateSoundEffect">sfx</see>
    /// before you can actually play it.</para>
    /// </summary>
    /// <remarks>
    /// Call this during setup. The content path is relative to the Content directory and
    /// doesn't need a file extension. One clip can be used to create many instances, so
    /// create one instance per concurrent playback you need.
    ///
    /// The typical audio setup is: load a clip here, create an instance with
    /// <see cref="CreateSoundEffect">sfx</see>, optionally configure pitch/pan/volume/loop,
    /// then call <see cref="PlaySfx(int)">play sfx</see> when you want to hear it.
    /// </remarks>
    /// <example>
    /// Load a clip and create a playable instance from it:
    /// <code>
    /// ` load the explosion sound clip
    /// clipId = 1
    /// load sfx clip clipId, "audio/explosion"
    ///
    /// ` create an instance so we can play it
    /// sfxId = 1
    /// sfx sfxId, clipId
    /// play sfx sfxId
    /// </code>
    /// </example>
    /// <example>
    /// Load one clip and create multiple instances for overlapping playback:
    /// <code>
    /// ` load the gunshot clip once
    /// gunClip = 1
    /// load sfx clip gunClip, "audio/gunshot"
    ///
    /// ` create three instances so up to three can overlap
    /// sfx 1, gunClip
    /// sfx 2, gunClip
    /// sfx 3, gunClip
    /// </code>
    /// </example>
    /// <param name="clipId">The clip ID to assign to the loaded sound.</param>
    /// <param name="path">Content path to the sound effect asset, relative to the Content directory.</param>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("load sfx clip")]
    public static void LoadSoundEffect(int clipId, string path)
    {
        AudioSystem.LoadSfxFromContent(clipId, path);
    }

    /// <summary>
    /// <para>Creates a playable sound effect instance from a loaded clip.</para>
    /// <para>You need a separate instance for each concurrent playback of the same sound.
    /// If you want to play the same explosion sound three times overlapping, you need three
    /// instances.</para>
    /// </summary>
    /// <remarks>
    /// This is the second step in the audio setup pipeline: first you load a clip with
    /// <see cref="LoadSoundEffect">load sfx clip</see>, then you create one or more
    /// instances here. Each instance has its own pitch, pan, volume, and playback state.
    ///
    /// After creating an instance, configure it with
    /// <see cref="SetSfxPitch">set sfx pitch</see>,
    /// <see cref="SetSfxPan">set sfx pan</see>,
    /// <see cref="SetSfxVolume">set sfx volume</see>, and
    /// <see cref="SetSfxLoop">set sfx loop</see>, then play it with
    /// <see cref="PlaySfx(int)">play sfx</see>.
    /// </remarks>
    /// <example>
    /// Full audio setup from clip to playback:
    /// <code>
    /// ` load the clip
    /// clipId = 1
    /// load sfx clip clipId, "audio/laser"
    ///
    /// ` create an instance and configure it
    /// sfxId = 1
    /// sfx sfxId, clipId
    /// set sfx volume sfxId, 0.8
    /// set sfx pitch sfxId, 0.2
    ///
    /// ` fire!
    /// play sfx sfxId
    /// </code>
    /// </example>
    /// <example>
    /// Create multiple instances from one clip for overlapping sounds:
    /// <code>
    /// ` one clip, three instances
    /// clipId = 1
    /// load sfx clip clipId, "audio/footstep"
    ///
    /// sfx 10, clipId
    /// sfx 11, clipId
    /// sfx 12, clipId
    ///
    /// ` randomize pitch slightly on each for variety
    /// set sfx pitch 10, -0.1
    /// set sfx pitch 11, 0.0
    /// set sfx pitch 12, 0.1
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID to assign to the new sound effect.</param>
    /// <param name="clipId">The clip ID of a previously loaded sound (from <see cref="LoadSoundEffect">load sfx clip</see>).</param>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    /// <seealso cref="SetSfxPitch">set sfx pitch</seealso>
    /// <seealso cref="SetSfxPan">set sfx pan</seealso>
    /// <seealso cref="SetSfxVolume">set sfx volume</seealso>
    /// <seealso cref="SetSfxLoop">set sfx loop</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("sfx")]
    public static void CreateSoundEffect(int sfxId, int clipId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioSystem.GetAudioEffectIndex(clipId, out _, out var clip);
        sfx.instance = clip.source.CreateInstance();
        AudioInstanceSystem.audioEffects[index] = sfx;
    }

    /// <summary>
    /// <para>Pauses a playing sound effect.</para>
    /// <para>The sound stops where it is and can be resumed from that point by calling
    /// <see cref="PlaySfx(int)">play sfx</see> again. Note that <see cref="PlaySfx(int)">play sfx</see> restarts
    /// from the beginning, so pausing is mainly useful for stopping a sound temporarily.</para>
    /// </summary>
    /// <remarks>
    /// A paused sound is different from a stopped one. <see cref="IsSfxDone">is sfx done</see>
    /// returns <c>0</c> for paused sounds (they're not "done", just on hold) but
    /// <c>1</c> for stopped sounds.
    /// </remarks>
    /// <example>
    /// Pause a looping ambient sound when the game pauses:
    /// <code>
    /// ` set up a looping wind sound
    /// clipId = 1
    /// load sfx clip clipId, "audio/wind"
    /// windSfx = 1
    /// sfx windSfx, clipId
    /// set sfx loop windSfx, 1
    /// play sfx windSfx
    ///
    /// ` later, when the game pauses
    /// pause sfx windSfx
    ///
    /// ` to resume, call play sfx again (restarts from beginning)
    /// play sfx windSfx
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect to pause.</param>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    /// <seealso cref="IsSfxDone">is sfx done</seealso>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="SetSfxLoop">set sfx loop</seealso>
    [FadeBasicCommand("pause sfx")]
    public static void PauseSfx(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pause();
    }


    /// <summary>
    /// <para>Plays a sound effect from the beginning.</para>
    /// <para>If the sound is already playing, it stops and restarts from the top. There is no
    /// way to layer the same instance on top of itself. Create multiple instances if you
    /// need overlapping playback of the same sound.</para>
    /// </summary>
    /// <remarks>
    /// This is the command that actually makes noise. You must have created the instance
    /// first with <see cref="CreateSoundEffect">sfx</see>. After calling this, you can
    /// check <see cref="IsSfxDone">is sfx done</see> to know when the sound has finished.
    ///
    /// For delayed playback, use <see cref="PlaySfx(int, int)">delay play sfx</see> instead.
    /// </remarks>
    /// <example>
    /// Basic playback:
    /// <code>
    /// ` load and create
    /// clipId = 1
    /// load sfx clip clipId, "audio/coin"
    /// coinSfx = 1
    /// sfx coinSfx, clipId
    ///
    /// ` play the sound
    /// play sfx coinSfx
    /// </code>
    /// </example>
    /// <example>
    /// Wait for a sound to finish before playing the next one:
    /// <code>
    /// play sfx introSfx
    /// DO
    ///   ` wait each frame until the sound is done
    /// LOOP UNTIL is sfx done(introSfx) = 1
    /// play sfx mainThemeSfx
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect to play.</param>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="IsSfxDone">is sfx done</seealso>
    /// <seealso cref="PlaySfx(int, int)">delay play sfx</seealso>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    [FadeBasicCommand("play sfx")]
    public static void PlaySfx(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Stop();
        AudioInstanceSystem.audioEffects[index].instance.Play();

    }

    /// <summary>
    /// <para>Plays a sound effect after a delay in milliseconds.</para>
    /// <para>The delay is measured from the moment you call this command, using the
    /// internal audio clock.</para>
    /// </summary>
    /// <remarks>
    /// Use this to stagger sound effects for a more natural feel. For example, playing
    /// slightly offset impact sounds when multiple objects collide in the same frame. The
    /// delay runs on the audio system's own timer, not game frames, so it stays accurate
    /// regardless of frame rate.
    ///
    /// Like <see cref="PlaySfx(int)">play sfx</see>, this stops any current playback on
    /// the instance before scheduling the delayed start.
    /// </remarks>
    /// <example>
    /// Stagger three impact sounds for a more natural collision:
    /// <code>
    /// ` play three impact sounds with slight offsets
    /// delay play sfx impactSfx1, 0
    /// delay play sfx impactSfx2, 50
    /// delay play sfx impactSfx3, 120
    /// </code>
    /// </example>
    /// <example>
    /// Play a warning beep one second from now:
    /// <code>
    /// ` schedule the beep for 1000 milliseconds in the future
    /// delay play sfx warningSfx, 1000
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect to play.</param>
    /// <param name="delayMs">Delay in milliseconds before playback starts.</param>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("delay play sfx")]
    public static void PlaySfx(int sfxId, int delayMs)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Stop();
        sfx.playingDelayedUntil = AudioInstanceSystem.currentTime + delayMs;
        AudioInstanceSystem.audioEffects[index] = sfx;
    }

    /// <summary>
    /// <para>Sets the pitch of a sound effect instance.</para>
    /// <para>Values outside the <c>-1</c> to <c>1</c> range are clamped automatically, so
    /// you will not get an error, but the value will not go beyond the limits.</para>
    /// </summary>
    /// <remarks>
    /// Pitch shifts the playback speed and frequency of the sound. A value of <c>0</c> is
    /// normal speed, <c>-1</c> is one octave down (slower, deeper), and <c>1</c> is one
    /// octave up (faster, higher). Fractional values like <c>0.5</c> work fine for
    /// subtle shifts.
    ///
    /// You can call this before or after <see cref="PlaySfx(int)">play sfx</see> and it
    /// takes effect immediately either way. This is handy for randomizing pitch slightly
    /// each time you play a sound so it doesn't feel repetitive (e.g., footsteps, gunshots).
    ///
    /// Read the current value back with <see cref="GetSfxPitch">sfx pitch</see>.
    /// </remarks>
    /// <example>
    /// Randomize pitch each time you play a footstep:
    /// <code>
    /// ` give each footstep a slightly different pitch
    /// randomPitch = rnd(60) - 30
    /// randomPitch = randomPitch / 100.0
    /// set sfx pitch footstepSfx, randomPitch
    /// play sfx footstepSfx
    /// </code>
    /// </example>
    /// <example>
    /// Pitch down an explosion for a heavy feel:
    /// <code>
    /// set sfx pitch explosionSfx, -0.5
    /// play sfx explosionSfx
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <param name="pitch">Pitch shift, from <c>-1</c> (one octave down) to <c>1</c> (one octave up). <c>0</c> is normal.</param>
    /// <seealso cref="GetSfxPitch">sfx pitch</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("set sfx pitch")]
    public static void SetSfxPitch(int sfxId, float pitch)
    {
        if (pitch >= 1) pitch = 1;
        if (pitch <= -1) pitch = -1;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pitch = pitch;
    }


    /// <summary>
    /// <para>Returns the current pitch of a sound effect instance.</para>
    /// </summary>
    /// <remarks>
    /// Use this to read back whatever was set with <see cref="SetSfxPitch">set sfx pitch</see>.
    /// This is useful if you're adjusting pitch incrementally each frame. Grab the current
    /// value, nudge it, and write it back. The returned value will always be in the
    /// <c>-1</c> to <c>1</c> range since <see cref="SetSfxPitch">set sfx pitch</see> clamps
    /// its input.
    /// </remarks>
    /// <example>
    /// Gradually raise the pitch of a rising siren each frame:
    /// <code>
    /// ` read current pitch and nudge it upward
    /// currentPitch = sfx pitch(sirenSfx)
    /// currentPitch = currentPitch + 0.01
    /// IF currentPitch &gt; 1.0 THEN currentPitch = -1.0
    /// set sfx pitch sirenSfx, currentPitch
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current pitch value, from <c>-1</c> (one octave down) to <c>1</c> (one octave up).</returns>
    /// <seealso cref="SetSfxPitch">set sfx pitch</seealso>
    [FadeBasicCommand("sfx pitch")]
    public static float GetSfxPitch(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pitch;
    }


    /// <summary>
    /// <para>Sets the stereo pan of a sound effect instance.</para>
    /// <para>Values outside the <c>-1</c> to <c>1</c> range are clamped automatically.</para>
    /// </summary>
    /// <remarks>
    /// Pan controls where the sound sits in the stereo field. <c>-1</c> is full left,
    /// <c>0</c> is centered, and <c>1</c> is full right. Use fractional values for
    /// subtle positioning. For example, <c>-0.3</c> places the sound slightly left
    /// of center.
    ///
    /// You can call this before or after <see cref="PlaySfx(int)">play sfx</see> and it
    /// takes effect immediately. A common pattern is to update pan each frame based on
    /// where the sound source is relative to the player, giving a simple positional
    /// audio effect without a full 3D audio system.
    ///
    /// Read the current value back with <see cref="GetSfxPan">sfx pan</see>.
    /// </remarks>
    /// <example>
    /// Pan a sound based on an enemy's screen position:
    /// <code>
    /// ` calculate pan from enemy X relative to screen center
    /// screenW = screen width()
    /// panValue = (enemyX - (screenW / 2)) / (screenW / 2)
    /// set sfx pan enemySfx, panValue
    /// </code>
    /// </example>
    /// <example>
    /// Hard-pan a sound to the left speaker:
    /// <code>
    /// set sfx pan leftChannelSfx, -1.0
    /// play sfx leftChannelSfx
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <param name="pan">Stereo position, from <c>-1</c> (full left) to <c>1</c> (full right). <c>0</c> is centered.</param>
    /// <seealso cref="GetSfxPan">sfx pan</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("set sfx pan")]
    public static void SetSfxPan(int sfxId, float pan)
    {

        if (pan >= 1) pan = 1;
        if (pan <= -1) pan = -1;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pan = pan;
    }

    /// <summary>
    /// <para>Returns the current stereo pan of a sound effect instance.</para>
    /// </summary>
    /// <remarks>
    /// Use this to read back whatever was set with <see cref="SetSfxPan">set sfx pan</see>.
    /// Handy if you're blending pan toward a target over time. Grab the current value,
    /// interpolate toward where you want it, and write it back with
    /// <see cref="SetSfxPan">set sfx pan</see>. The returned value will always be in the
    /// <c>-1</c> to <c>1</c> range.
    /// </remarks>
    /// <example>
    /// Smoothly blend pan toward a target position each frame:
    /// <code>
    /// ` lerp the pan toward the target by 10% each frame
    /// currentPan = sfx pan(engineSfx)
    /// currentPan = currentPan + (targetPan - currentPan) * 0.1
    /// set sfx pan engineSfx, currentPan
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current pan value, from <c>-1</c> (full left) to <c>1</c> (full right).</returns>
    /// <seealso cref="SetSfxPan">set sfx pan</seealso>
    [FadeBasicCommand("sfx pan")]
    public static float GetSfxPan(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pan;
    }


    /// <summary>
    /// <para>Sets the volume of a sound effect instance.</para>
    /// <para>Values outside the <c>0</c> to <c>1</c> range are clamped automatically.</para>
    /// </summary>
    /// <remarks>
    /// Volume goes from <c>0</c> (completely silent) to <c>1</c> (full volume). There is no
    /// way to boost above <c>1</c>. If you need a sound to feel louder, you will need to
    /// adjust the source audio asset itself.
    ///
    /// You can call this before or after <see cref="PlaySfx(int)">play sfx</see> and it
    /// takes effect immediately. This makes it easy to fade sounds in and out by adjusting
    /// volume a little each frame.
    ///
    /// Read the current value back with <see cref="GetSfxVolume">sfx volume</see>.
    /// </remarks>
    /// <example>
    /// Fade out a sound over time each frame:
    /// <code>
    /// ` reduce volume by a small amount each frame
    /// vol = sfx volume(mySfx)
    /// vol = vol - 0.02
    /// IF vol &lt; 0.0 THEN vol = 0.0
    /// set sfx volume mySfx, vol
    /// </code>
    /// </example>
    /// <example>
    /// Set a quiet background ambience at half volume:
    /// <code>
    /// set sfx volume ambientSfx, 0.5
    /// set sfx loop ambientSfx, 1
    /// play sfx ambientSfx
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <param name="volume">Volume level, from <c>0</c> (silent) to <c>1</c> (full volume).</param>
    /// <seealso cref="GetSfxVolume">sfx volume</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    /// <seealso cref="SetSfxLoop">set sfx loop</seealso>
    [FadeBasicCommand("set sfx volume")]
    public static void SetSfxVolume(int sfxId, float volume)
    {
        if (volume >= 1) volume = 1;
        if (volume <= 0) volume = 0;
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Volume = volume;
    }

    /// <summary>
    /// <para>Returns the current volume of a sound effect instance.</para>
    /// </summary>
    /// <remarks>
    /// Use this to read back whatever was set with <see cref="SetSfxVolume">set sfx volume</see>.
    /// This is useful for fade-in and fade-out effects. Grab the current volume, adjust it
    /// toward your target, and write it back with <see cref="SetSfxVolume">set sfx volume</see>.
    /// The returned value will always be in the <c>0</c> to <c>1</c> range.
    /// </remarks>
    /// <example>
    /// Fade in a sound from silence to full volume:
    /// <code>
    /// ` increase volume toward 1.0 each frame
    /// vol = sfx volume(mySfx)
    /// IF vol &lt; 1.0
    ///   vol = vol + 0.01
    ///   set sfx volume mySfx, vol
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current volume level, from <c>0</c> (silent) to <c>1</c> (full volume).</returns>
    /// <seealso cref="SetSfxVolume">set sfx volume</seealso>
    [FadeBasicCommand("sfx volume")]
    public static float GetSfxVolume(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Volume;
    }


    /// <summary>
    /// <para>Sets whether a sound effect should loop continuously.</para>
    /// <para>When looping is enabled, the sound restarts from the beginning each time it
    /// reaches the end, and <see cref="IsSfxDone">is sfx done</see> will never
    /// return <c>1</c> while it's playing.</para>
    /// </summary>
    /// <remarks>
    /// Set this before calling <see cref="PlaySfx(int)">play sfx</see> for the cleanest
    /// results. You can also toggle it while a sound is already playing. Turning loop off
    /// mid-playback lets the sound finish its current pass and then stop naturally.
    ///
    /// Looping is great for ambient sounds, music loops, or engine hums, basically anything that
    /// needs to run indefinitely. When you're done with a looping sound, either call
    /// <see cref="PauseSfx">pause sfx</see> to silence it or set loop back to <c>0</c>
    /// and let it finish on its own.
    /// </remarks>
    /// <example>
    /// Set up a looping background ambience:
    /// <code>
    /// ` load and create the ambient loop
    /// clipId = 1
    /// load sfx clip clipId, "audio/forest_ambience"
    /// ambSfx = 1
    /// sfx ambSfx, clipId
    ///
    /// ` enable looping and play at half volume
    /// set sfx loop ambSfx, 1
    /// set sfx volume ambSfx, 0.5
    /// play sfx ambSfx
    /// </code>
    /// </example>
    /// <example>
    /// Stop a looping sound gracefully by letting it finish its current pass:
    /// <code>
    /// ` turn off looping so the sound plays to the end and stops
    /// set sfx loop ambSfx, 0
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <param name="isLooped">Pass <c>1</c> to loop, <c>0</c> to play once.</param>
    /// <seealso cref="IsSfxDone">is sfx done</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    /// <seealso cref="PauseSfx">pause sfx</seealso>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="SetSfxVolume">set sfx volume</seealso>
    [FadeBasicCommand("set sfx loop")]
    public static void SetSfxLoop(int sfxId, bool isLooped)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.IsLooped = isLooped;
    }
    //

    /// <summary>
    /// <para>Checks whether a sound effect has finished playing.</para>
    /// <para>A paused sound is not considered "done". Only a sound that has fully stopped
    /// (either it played to the end or was never started) returns <c>1</c>.</para>
    /// </summary>
    /// <remarks>
    /// This is how you know when a one-shot sound has finished. Poll it each frame if you
    /// need to trigger something when the sound ends. For example, you could play a follow-up
    /// sound or remove a visual effect that was synced to the audio.
    ///
    /// For looping sounds (set via <see cref="SetSfxLoop">set sfx loop</see>), this will
    /// always return <c>0</c> while they're playing, since they never reach a natural end.
    /// A sound that was paused with <see cref="PauseSfx">pause sfx</see> also returns
    /// <c>0</c> because it's on hold, not done.
    /// </remarks>
    /// <example>
    /// Wait for an intro jingle to finish, then start gameplay music:
    /// <code>
    /// play sfx jingleSfx
    /// DO
    ///   ` keep looping until the jingle finishes
    /// LOOP UNTIL is sfx done(jingleSfx) = 1
    ///
    /// ` now start the looping gameplay music
    /// set sfx loop musicSfx, 1
    /// play sfx musicSfx
    /// </code>
    /// </example>
    /// <example>
    /// Trigger a visual effect when a sound finishes (called each frame):
    /// <code>
    /// IF is sfx done(chargeSfx) = 1
    ///   ` the charge-up sound finished, fire the laser!
    ///   play sfx laserSfx
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect to check.</param>
    /// <returns><c>1</c> if the sound effect has stopped, <c>0</c> if it's still playing or paused.</returns>
    /// <seealso cref="SetSfxLoop">set sfx loop</seealso>
    /// <seealso cref="PauseSfx">pause sfx</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("is sfx done")]
    public static bool IsSfxDone(int sfxId)
    {
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.State == SoundState.Stopped;
    }

}
