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
    /// ` load a font so we can display the peeked ID
    /// font 1, "font"
    ///
    /// ` load one clip so a clip ID is already in use
    /// load sfx clip 1, "coin"
    ///
    /// ` peek at what clip ID would be handed out next (does not reserve it)
    /// nextClipId = free sfx clip id(nextClipId)
    ///
    /// do
    ///   ` draw the peeked ID every frame so we can see it
    ///   text 1, 470, 200, 1, "next free clip id: " + str$(nextClipId)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxClipId">Receives the next free clip ID.</param>
    /// <returns>The next available clip ID (not yet reserved).</returns>
    /// <seealso cref="ReserveSfxClipNextId">reserve sfx clip id</seealso>
    /// <seealso cref="LoadSoundEffect">load sfx clip</seealso>
    [FadeBasicCommand("free sfx clip id")]
    public static int GetFreeSfxClipNextId(ref int sfxClipId)
    {
#if BROWSER
        sfxClipId = BrowserAudioBridge.GetHighestClipId() + 1;
#else
        sfxClipId = AudioSystem.highestClipId + 1;
#endif
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
    /// ` load a font so we can report the reserved ID
    /// font 1, "font"
    ///
    /// ` reserve a clip slot, then load a real sound into it
    /// clipId = reserve sfx clip id(clipId)
    /// load sfx clip clipId, "laser"
    ///
    /// ` create an instance from that clip so we can hear it
    /// sfx 1, clipId
    /// play sfx 1
    ///
    /// do
    ///   ` keep the program running and show the reserved ID
    ///   text 1, 470, 200, 1, "reserved clip id: " + str$(clipId)
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.ReserveClipId(sfxClipId);
#else
        AudioSystem.GetAudioEffectIndex(sfxClipId, out _, out _);
#endif
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
    /// ` load a font so we can display the peeked ID
    /// font 1, "font"
    ///
    /// ` create one instance so an instance ID is already in use
    /// load sfx clip 1, "coin"
    /// sfx 1, 1
    ///
    /// ` peek at what instance ID would be handed out next (does not reserve it)
    /// nextSfxId = free sfx id(nextSfxId)
    ///
    /// do
    ///   ` draw the peeked instance ID every frame
    ///   text 1, 470, 200, 1, "next free sfx id: " + str$(nextSfxId)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">Receives the next free instance ID.</param>
    /// <returns>The next available instance ID (not yet reserved).</returns>
    /// <seealso cref="ReserveSfxNextId">reserve sfx id</seealso>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    [FadeBasicCommand("free sfx id")]
    public static int GetFreeSfxNextId(ref int sfxId)
    {
#if BROWSER
        sfxId = BrowserAudioBridge.GetHighestInstanceId() + 1;
#else
        sfxId = AudioInstanceSystem.highestEffectId + 1;
#endif
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
    /// ` load a font so we can report the reserved ID
    /// font 1, "font"
    ///
    /// ` load a clip we can build an instance from
    /// clipId = 1
    /// load sfx clip clipId, "coin"
    ///
    /// ` reserve the instance slot first, then create it from the clip
    /// mysfxId = reserve sfx id(mysfxId)
    /// sfx mysfxId, clipId
    /// play sfx mysfxId
    ///
    /// do
    ///   ` keep running and show the reserved instance ID
    ///   text 1, 470, 200, 1, "reserved sfx id: " + str$(mysfxId)
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.ReserveInstanceId(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out _, out _);
#endif
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
    /// load sfx clip clipId, "explosion"
    ///
    /// ` create an instance so we can play it
    /// sfxId = 1
    /// sfx sfxId, clipId
    /// play sfx sfxId
    ///
    /// ` load a font so we can label what is happening
    /// font 1, "font"
    ///
    /// do
    ///   ` keep the program running so the sound can play out
    ///   text 1, 470, 200, 1, "boom!"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Load one clip and create multiple instances for overlapping playback:
    /// <code>
    /// ` load the laser clip once
    /// laserClip = 1
    /// load sfx clip laserClip, "laser"
    ///
    /// ` create three instances so up to three can overlap
    /// sfx 1, laserClip
    /// sfx 2, laserClip
    /// sfx 3, laserClip
    ///
    /// ` fire all three so they layer on top of each other
    /// play sfx 1
    /// play sfx 2
    /// play sfx 3
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the overlapping shots can be heard
    ///   text 1, 470, 200, 1, "pew pew pew"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="clipId">The clip ID to assign to the loaded sound.</param>
    /// <param name="path">Content path to the sound effect asset, relative to the Content directory.</param>
    /// <seealso cref="CreateSoundEffect">sfx</seealso>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("load sfx clip")]
    public static void LoadSoundEffect(int clipId, string path)
    {
#if BROWSER
        // Browser path: source bytes were decoded by window.fadeAudio
        // when the playground sent register-audio. LoadClip just maps
        // the clip slot id to the registered name. No XNB involved.
        BrowserAudioBridge.LoadClip(clipId, path);
#else
        AudioSystem.LoadSfxFromContent(clipId, path);
#endif
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
    /// load sfx clip clipId, "laser"
    ///
    /// ` create an instance and configure it
    /// sfxId = 1
    /// sfx sfxId, clipId
    /// set sfx volume sfxId, 0.8
    /// set sfx pitch sfxId, 0.2
    ///
    /// ` fire!
    /// play sfx sfxId
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep the program running so the shot can be heard
    ///   text 1, 470, 200, 1, "fire!"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Create multiple instances from one clip for overlapping sounds:
    /// <code>
    /// ` one clip, three instances
    /// clipId = 1
    /// load sfx clip clipId, "jump"
    ///
    /// sfx 10, clipId
    /// sfx 11, clipId
    /// sfx 12, clipId
    ///
    /// ` randomize pitch slightly on each for variety
    /// set sfx pitch 10, -0.1
    /// set sfx pitch 11, 0.0
    /// set sfx pitch 12, 0.1
    ///
    /// ` play each one so you can hear the variety
    /// play sfx 10
    /// play sfx 11
    /// play sfx 12
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the varied jumps can be heard
    ///   text 1, 470, 200, 1, "hop hop hop"
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.CreateInstance(sfxId, clipId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioSystem.GetAudioEffectIndex(clipId, out _, out var clip);
        if (clip.source == null)
        {
            // Reached if `load sfx clip <clipId>, "name"` previously failed
            // (browser: ContentLoadException, unsupported format, etc.) — the
            // catch in LoadSfxFromContent logged the cause already. Without
            // this guard the next line would NRE; with it the user gets a
            // clear message and the program keeps running.
            System.Console.Error.WriteLine(
                $"[fade] sfx: clip {clipId} has no loaded source — did `load sfx clip {clipId}` succeed?");
            return;
        }
        try
        {
            sfx.instance = clip.source.CreateInstance();
        }
        catch (System.Exception ex)
        {
            System.Console.Error.WriteLine(
                $"[fade] sfx: CreateInstance() threw for clip {clipId}: {ex}");
            return;
        }
        AudioInstanceSystem.audioEffects[index] = sfx;
#endif
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
    /// ` set up a looping ambient sound
    /// clipId = 1
    /// load sfx clip clipId, "powerup"
    /// windSfx = 1
    /// sfx windSfx, clipId
    /// set sfx loop windSfx, 1
    /// play sfx windSfx
    ///
    /// ` load a font so we can show the current state
    /// font 1, "font"
    ///
    /// frame = 0
    /// paused = 0
    /// do
    ///   frame = frame + 1
    ///   ` after about 2 seconds, pause the looping sound once
    ///   IF frame = 120
    ///     pause sfx windSfx
    ///     paused = 1
    ///   ENDIF
    ///   IF paused = 1
    ///     text 1, 470, 200, 1, "paused"
    ///   ELSE
    ///     text 1, 470, 200, 1, "playing"
    ///   ENDIF
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.Pause(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pause();
#endif
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
    /// load sfx clip clipId, "coin"
    /// coinSfx = 1
    /// sfx coinSfx, clipId
    ///
    /// ` play the sound
    /// play sfx coinSfx
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the coin sound can play
    ///   text 1, 470, 200, 1, "coin collected!"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Wait for a sound to finish before playing the next one:
    /// <code>
    /// ` load two clips: an intro and the main theme
    /// load sfx clip 1, "select"
    /// load sfx clip 2, "powerup"
    /// introSfx = 1
    /// mainThemeSfx = 2
    /// sfx introSfx, 1
    /// sfx mainThemeSfx, 2
    ///
    /// ` start the intro
    /// play sfx introSfx
    /// startedMain = 0
    ///
    /// ` load a font so we can show the state
    /// font 1, "font"
    ///
    /// do
    ///   ` once the intro finishes, start the main theme (only once)
    ///   IF is sfx done(introSfx) = 1
    ///     IF startedMain = 0
    ///       play sfx mainThemeSfx
    ///       startedMain = 1
    ///     ENDIF
    ///     text 1, 470, 200, 1, "main theme"
    ///   ELSE
    ///     text 1, 470, 200, 1, "intro..."
    ///   ENDIF
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.Play(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        var instance = AudioInstanceSystem.audioEffects[index].instance;
        if (instance == null)
        {
            // Reached if `sfx <sfxId>, <clipId>` previously failed (null clip
            // source, CreateInstance threw, etc.). The earlier failure was
            // already logged; a clear message here is friendlier than the
            // VM exploding with a raw NRE the user can't trace.
            System.Console.Error.WriteLine(
                $"[fade] play sfx: instance {sfxId} has no playable source — did `sfx {sfxId}, …` succeed?");
            return;
        }
        try
        {
            instance.Stop();
            instance.Play();
        }
        catch (System.Exception ex)
        {
            System.Console.Error.WriteLine(
                $"[fade] play sfx: instance.Play() threw for {sfxId}: {ex}");
        }
#endif
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
    /// ` load one impact clip and make three instances from it
    /// load sfx clip 1, "explosion"
    /// impactSfx1 = 1
    /// impactSfx2 = 2
    /// impactSfx3 = 3
    /// sfx impactSfx1, 1
    /// sfx impactSfx2, 1
    /// sfx impactSfx3, 1
    ///
    /// ` play three impact sounds with slight offsets
    /// delay play sfx impactSfx1, 0
    /// delay play sfx impactSfx2, 50
    /// delay play sfx impactSfx3, 120
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the staggered impacts can be heard
    ///   text 1, 470, 200, 1, "crash!"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Play a warning beep one second from now:
    /// <code>
    /// ` load a warning sound and create an instance
    /// load sfx clip 1, "laser"
    /// warningSfx = 1
    /// sfx warningSfx, 1
    ///
    /// ` schedule the beep for 1000 milliseconds in the future
    /// delay play sfx warningSfx, 1000
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the delayed beep actually fires
    ///   text 1, 470, 200, 1, "warning incoming..."
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect to play.</param>
    /// <param name="delayMs">Delay in milliseconds before playback starts.</param>
    /// <seealso cref="PlaySfx(int)">play sfx</seealso>
    [FadeBasicCommand("delay play sfx")]
    public static void PlaySfx(int sfxId, int delayMs)
    {
#if BROWSER
        BrowserAudioBridge.PlayWithDelay(sfxId, delayMs);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Stop();
        sfx.playingDelayedUntil = AudioInstanceSystem.currentTime + delayMs;
        AudioInstanceSystem.audioEffects[index] = sfx;
#endif
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
    /// ` load a footstep sound and create an instance
    /// load sfx clip 1, "jump"
    /// footstepSfx = 1
    /// sfx footstepSfx, 1
    ///
    /// ` load a font so we can show the chosen pitch
    /// font 1, "font"
    ///
    /// frame = 0
    /// randomPitch = 0
    /// do
    ///   frame = frame + 1
    ///   ` about twice a second, play a footstep at a new random pitch
    ///   IF frame &gt;= 30
    ///     frame = 0
    ///     ` give each footstep a slightly different pitch
    ///     randomPitch = rnd(60) - 30
    ///     randomPitch = randomPitch / 100.0
    ///     set sfx pitch footstepSfx, randomPitch
    ///     play sfx footstepSfx
    ///   ENDIF
    ///   text 1, 470, 200, 1, "footstep pitch: " + str$(randomPitch)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Pitch down an explosion for a heavy feel:
    /// <code>
    /// ` load an explosion and create an instance
    /// load sfx clip 1, "explosion"
    /// explosionSfx = 1
    /// sfx explosionSfx, 1
    ///
    /// ` pitch it down for a heavier feel, then play it
    /// set sfx pitch explosionSfx, -0.5
    /// play sfx explosionSfx
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the deep explosion can be heard
    ///   text 1, 470, 200, 1, "heavy boom"
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.SetPitch(sfxId, pitch);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pitch = pitch;
#endif
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
    /// ` load a looping siren sound and start it
    /// load sfx clip 1, "laser"
    /// sirenSfx = 1
    /// sfx sirenSfx, 1
    /// set sfx loop sirenSfx, 1
    /// play sfx sirenSfx
    ///
    /// ` load a font so we can show the current pitch
    /// font 1, "font"
    ///
    /// do
    ///   ` read current pitch and nudge it upward
    ///   currentPitch = sfx pitch(sirenSfx)
    ///   currentPitch = currentPitch + 0.01
    ///   IF currentPitch &gt; 1.0 THEN currentPitch = -1.0
    ///   set sfx pitch sirenSfx, currentPitch
    ///   text 1, 470, 200, 1, "pitch: " + str$(currentPitch)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current pitch value, from <c>-1</c> (one octave down) to <c>1</c> (one octave up).</returns>
    /// <seealso cref="SetSfxPitch">set sfx pitch</seealso>
    [FadeBasicCommand("sfx pitch")]
    public static float GetSfxPitch(int sfxId)
    {
#if BROWSER
        return BrowserAudioBridge.GetPitch(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pitch;
#endif
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
    /// ` load a sound and loop it so we can hear the panning
    /// load sfx clip 1, "laser"
    /// enemySfx = 1
    /// sfx enemySfx, 1
    /// set sfx loop enemySfx, 1
    /// play sfx enemySfx
    ///
    /// ` load the ghost so we can see the moving "enemy"
    /// texture 1, "ghost"
    ///
    /// enemyX = 0
    /// do
    ///   ` move the enemy across the screen and draw it
    ///   enemyX = enemyX + 4
    ///   IF enemyX &gt; screen width() THEN enemyX = 0
    ///   sprite 1, enemyX, 200, 1
    ///
    ///   ` calculate pan from enemy X relative to screen center
    ///   screenW = screen width()
    ///   panValue = (enemyX - (screenW / 2)) / (screenW / 2)
    ///   set sfx pan enemySfx, panValue
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Hard-pan a sound to the left speaker:
    /// <code>
    /// ` load a sound and create an instance
    /// load sfx clip 1, "coin"
    /// leftChannelSfx = 1
    /// sfx leftChannelSfx, 1
    ///
    /// ` hard-pan it to the left speaker, then play it
    /// set sfx pan leftChannelSfx, -1.0
    /// play sfx leftChannelSfx
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the left-panned sound can be heard
    ///   text 1, 470, 200, 1, "left channel"
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.SetPan(sfxId, pan);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Pan = pan;
#endif
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
    /// ` load a looping engine sound and start it
    /// load sfx clip 1, "powerup"
    /// engineSfx = 1
    /// sfx engineSfx, 1
    /// set sfx loop engineSfx, 1
    /// play sfx engineSfx
    ///
    /// ` we want the engine to settle on the right side
    /// targetPan = 1.0
    ///
    /// ` load a font so we can show the current pan
    /// font 1, "font"
    ///
    /// do
    ///   ` lerp the pan toward the target by 10% each frame
    ///   currentPan = sfx pan(engineSfx)
    ///   currentPan = currentPan + (targetPan - currentPan) * 0.1
    ///   set sfx pan engineSfx, currentPan
    ///   text 1, 470, 200, 1, "pan: " + str$(currentPan)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current pan value, from <c>-1</c> (full left) to <c>1</c> (full right).</returns>
    /// <seealso cref="SetSfxPan">set sfx pan</seealso>
    [FadeBasicCommand("sfx pan")]
    public static float GetSfxPan(int sfxId)
    {
#if BROWSER
        return BrowserAudioBridge.GetPan(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Pan;
#endif
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
    /// ` load a looping sound and start it at full volume
    /// load sfx clip 1, "powerup"
    /// mySfx = 1
    /// sfx mySfx, 1
    /// set sfx loop mySfx, 1
    /// set sfx volume mySfx, 1.0
    /// play sfx mySfx
    ///
    /// ` load a font so we can show the current volume
    /// font 1, "font"
    ///
    /// do
    ///   ` reduce volume by a small amount each frame
    ///   vol = sfx volume(mySfx)
    ///   vol = vol - 0.02
    ///   IF vol &lt; 0.0 THEN vol = 0.0
    ///   set sfx volume mySfx, vol
    ///   text 1, 470, 200, 1, "volume: " + str$(vol)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Set a quiet background ambience at half volume:
    /// <code>
    /// ` load an ambient sound and create an instance
    /// load sfx clip 1, "powerup"
    /// ambientSfx = 1
    /// sfx ambientSfx, 1
    ///
    /// ` play it quietly on a loop as background ambience
    /// set sfx volume ambientSfx, 0.5
    /// set sfx loop ambientSfx, 1
    /// play sfx ambientSfx
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the ambience keeps looping
    ///   text 1, 470, 200, 1, "ambience at half volume"
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.SetVolume(sfxId, volume);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.Volume = volume;
#endif
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
    /// ` load a looping sound and start it silent
    /// load sfx clip 1, "powerup"
    /// mySfx = 1
    /// sfx mySfx, 1
    /// set sfx loop mySfx, 1
    /// set sfx volume mySfx, 0.0
    /// play sfx mySfx
    ///
    /// ` load a font so we can show the current volume
    /// font 1, "font"
    ///
    /// do
    ///   ` increase volume toward 1.0 each frame
    ///   vol = sfx volume(mySfx)
    ///   IF vol &lt; 1.0
    ///     vol = vol + 0.01
    ///     set sfx volume mySfx, vol
    ///   ENDIF
    ///   text 1, 470, 200, 1, "volume: " + str$(vol)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The current volume level, from <c>0</c> (silent) to <c>1</c> (full volume).</returns>
    /// <seealso cref="SetSfxVolume">set sfx volume</seealso>
    [FadeBasicCommand("sfx volume")]
    public static float GetSfxVolume(int sfxId)
    {
#if BROWSER
        return BrowserAudioBridge.GetVolume(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.Volume;
#endif
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
    /// load sfx clip clipId, "powerup"
    /// ambSfx = 1
    /// sfx ambSfx, clipId
    ///
    /// ` enable looping and play at half volume
    /// set sfx loop ambSfx, 1
    /// set sfx volume ambSfx, 0.5
    /// play sfx ambSfx
    ///
    /// ` load a font so we can show a label
    /// font 1, "font"
    ///
    /// do
    ///   ` keep running so the loop keeps repeating
    ///   text 1, 470, 200, 1, "looping ambience"
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Stop a looping sound gracefully by letting it finish its current pass:
    /// <code>
    /// ` set up a looping sound
    /// load sfx clip 1, "powerup"
    /// ambSfx = 1
    /// sfx ambSfx, 1
    /// set sfx loop ambSfx, 1
    /// play sfx ambSfx
    ///
    /// ` load a font so we can show the current state
    /// font 1, "font"
    ///
    /// frame = 0
    /// stopped = 0
    /// do
    ///   frame = frame + 1
    ///   ` after about 3 seconds, stop looping so it finishes and ends
    ///   IF frame = 180
    ///     ` turn off looping so the sound plays to the end and stops
    ///     set sfx loop ambSfx, 0
    ///     stopped = 1
    ///   ENDIF
    ///   IF stopped = 1
    ///     text 1, 470, 200, 1, "loop off - will finish"
    ///   ELSE
    ///     text 1, 470, 200, 1, "looping"
    ///   ENDIF
    ///   sync
    /// loop
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
#if BROWSER
        BrowserAudioBridge.SetLoop(sfxId, isLooped);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        AudioInstanceSystem.audioEffects[index].instance.IsLooped = isLooped;
#endif
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
    /// ` load an intro jingle and the gameplay music
    /// load sfx clip 1, "select"
    /// load sfx clip 2, "powerup"
    /// jingleSfx = 1
    /// musicSfx = 2
    /// sfx jingleSfx, 1
    /// sfx musicSfx, 2
    ///
    /// ` start the jingle
    /// play sfx jingleSfx
    /// startedMusic = 0
    ///
    /// ` load a font so we can show the state
    /// font 1, "font"
    ///
    /// do
    ///   ` when the jingle finishes, start the looping gameplay music once
    ///   IF is sfx done(jingleSfx) = 1
    ///     IF startedMusic = 0
    ///       set sfx loop musicSfx, 1
    ///       play sfx musicSfx
    ///       startedMusic = 1
    ///     ENDIF
    ///     text 1, 470, 200, 1, "music"
    ///   ELSE
    ///     text 1, 470, 200, 1, "jingle..."
    ///   ENDIF
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <example>
    /// Trigger a visual effect when a sound finishes (called each frame):
    /// <code>
    /// ` load a charge-up sound and a laser sound
    /// load sfx clip 1, "powerup"
    /// load sfx clip 2, "laser"
    /// chargeSfx = 1
    /// laserSfx = 2
    /// sfx chargeSfx, 1
    /// sfx laserSfx, 2
    ///
    /// ` start the charge-up
    /// play sfx chargeSfx
    /// fired = 0
    ///
    /// ` load a font so we can show the state
    /// font 1, "font"
    ///
    /// do
    ///   IF is sfx done(chargeSfx) = 1
    ///     IF fired = 0
    ///       ` the charge-up sound finished, fire the laser!
    ///       play sfx laserSfx
    ///       fired = 1
    ///     ENDIF
    ///     text 1, 470, 200, 1, "fired!"
    ///   ELSE
    ///     text 1, 470, 200, 1, "charging..."
    ///   ENDIF
    ///   sync
    /// loop
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
#if BROWSER
        return BrowserAudioBridge.IsDone(sfxId);
#else
        AudioInstanceSystem.GetAudioEffectIndex(sfxId, out var index, out var sfx);
        return AudioInstanceSystem.audioEffects[index].instance.State == SoundState.Stopped;
#endif
    }

}
