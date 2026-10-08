using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Puts a sound effect instance into a numbered group.</para>
    /// <para>Every sound starts in group <c>0</c>. A group's volume, pitch and pan are applied
    /// to every sound in it, on top of the sound's own.</para>
    /// </summary>
    /// <remarks>
    /// Groups are how you turn a whole category of sounds up or down at once: leave ordinary
    /// sound effects in group <c>0</c>, put music in group <c>1</c>, and an options menu only
    /// has to call <see cref="SetSfxGroupVolume">set sfx group volume</see> on each.
    ///
    /// A group does not need creating. Any number is a group, and a group nothing has touched
    /// changes nothing: volume <c>1</c>, pitch <c>0</c>, pan <c>0</c>.
    ///
    /// The group belongs to the instance ID, so it survives calling
    /// <see cref="CreateSoundEffect">sfx</see> on the same ID again. It can be set before the
    /// instance exists, and it takes effect straight away on a sound that is already playing.
    /// </remarks>
    /// <example>
    /// Keep music and sound effects on separate volume controls:
    /// <code>
    /// #constant GROUP_SFX 0
    /// #constant GROUP_MUSIC 1
    ///
    /// load sfx clip 1, "laser"
    /// load sfx clip 2, "powerup"
    ///
    /// ` the laser stays in group 0, where every sound starts
    /// sfx 1, 1
    ///
    /// ` the music goes in its own group
    /// sfx 2, 2
    /// set sfx group 2, GROUP_MUSIC
    /// set sfx loop 2, 1
    /// play sfx 2
    ///
    /// ` quiet music, full volume effects
    /// set sfx group volume GROUP_MUSIC, 0.3
    ///
    /// do
    ///   IF new spaceKey() THEN play sfx 1
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <param name="groupId">The group to put it in. Any number; <c>0</c> is the default group.</param>
    /// <seealso cref="GetSfxGroup">sfx group</seealso>
    /// <seealso cref="SetSfxGroupVolume">set sfx group volume</seealso>
    /// <seealso cref="SetSfxGroupPitch">set sfx group pitch</seealso>
    /// <seealso cref="SetSfxGroupPan">set sfx group pan</seealso>
    [FadeBasicCommand("set sfx group")]
    public static void SetSfxGroup(int sfxId, int groupId)
    {
        var sound = AudioMixSystem.GetSound(sfxId);
        sound.group = groupId;
        AudioMixSystem.SetSound(sfxId, sound);
    }

    /// <summary>
    /// <para>Returns the group a sound effect instance is in.</para>
    /// </summary>
    /// <remarks>
    /// This is <c>0</c> for any sound that was never given to
    /// <see cref="SetSfxGroup">set sfx group</see>.
    /// </remarks>
    /// <example>
    /// Show which group a sound is in:
    /// <code>
    /// load sfx clip 1, "laser"
    /// sfx 1, 1
    /// set sfx group 1, 2
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "group: " + str$(sfx group(1))
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="sfxId">The instance ID of the sound effect.</param>
    /// <returns>The group the sound is in.</returns>
    /// <seealso cref="SetSfxGroup">set sfx group</seealso>
    [FadeBasicCommand("sfx group")]
    public static int GetSfxGroup(int sfxId)
    {
        return AudioMixSystem.GetSound(sfxId).group;
    }

    /// <summary>
    /// <para>Sets the volume of a whole group of sound effects.</para>
    /// <para>It is a multiplier: <c>1</c> leaves the sounds as they are, <c>0.5</c> halves them,
    /// and <c>0</c> silences the group.</para>
    /// </summary>
    /// <remarks>
    /// What is heard for each sound is its own volume, from
    /// <see cref="SetSfxVolume">set sfx volume</see>, multiplied by this. The sound's own
    /// volume is not changed, and <see cref="GetSfxVolume">sfx volume</see> keeps returning
    /// it, so code that fades a sound in and out goes on working inside a quieter group.
    ///
    /// The result is capped at full volume, so a group above <c>1</c> can bring quiet sounds
    /// up but cannot push a sound past <c>1</c>. Sounds that are already playing change
    /// straight away.
    /// </remarks>
    /// <example>
    /// A volume setting that the up and down keys change:
    /// <code>
    /// load sfx clip 1, "powerup"
    /// sfx 1, 1
    /// set sfx loop 1, 1
    /// play sfx 1
    ///
    /// font 1, "font"
    /// level# = 1.0
    ///
    /// do
    ///   IF new upKey() THEN level# = level# + 0.1
    ///   IF new downKey() THEN level# = level# - 0.1
    ///   IF level# &gt; 1 THEN level# = 1
    ///   IF level# &lt; 0 THEN level# = 0
    ///
    ///   ` every sound in group 0 follows the setting
    ///   set sfx group volume 0, level#
    ///   text 1, 470, 200, 1, "volume: " + str$(level#)
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to change. <c>0</c> is the group every sound starts in.</param>
    /// <param name="volume">The multiplier. <c>0</c> is silent and <c>1</c> is unchanged. Negative values are treated as <c>0</c>.</param>
    /// <seealso cref="GetSfxGroupVolume">sfx group volume</seealso>
    /// <seealso cref="SetSfxGroup">set sfx group</seealso>
    /// <seealso cref="SetSfxVolume">set sfx volume</seealso>
    [FadeBasicCommand("set sfx group volume")]
    public static void SetSfxGroupVolume(int groupId, float volume)
    {
        if (volume <= 0) volume = 0;

        var group = AudioMixSystem.GetGroup(groupId);
        group.volume = volume;
        AudioMixSystem.SetGroup(groupId, group);
    }

    /// <summary>
    /// <para>Returns the volume multiplier of a group of sound effects.</para>
    /// </summary>
    /// <remarks>
    /// This is <c>1</c> for a group that was never given to
    /// <see cref="SetSfxGroupVolume">set sfx group volume</see>.
    /// </remarks>
    /// <example>
    /// Fade a whole group out:
    /// <code>
    /// load sfx clip 1, "powerup"
    /// sfx 1, 1
    /// set sfx loop 1, 1
    /// play sfx 1
    ///
    /// do
    ///   level# = sfx group volume(0)
    ///   IF level# &gt; 0 THEN set sfx group volume 0, level# - 0.01
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to read.</param>
    /// <returns>The group's volume multiplier.</returns>
    /// <seealso cref="SetSfxGroupVolume">set sfx group volume</seealso>
    [FadeBasicCommand("sfx group volume")]
    public static float GetSfxGroupVolume(int groupId)
    {
        return AudioMixSystem.GetGroup(groupId).volume;
    }

    /// <summary>
    /// <para>Shifts the pitch of a whole group of sound effects.</para>
    /// <para>It is added to each sound's own pitch: <c>0</c> leaves the sounds as they are.</para>
    /// </summary>
    /// <remarks>
    /// What is heard for each sound is its own pitch, from
    /// <see cref="SetSfxPitch">set sfx pitch</see>, plus this, kept inside the <c>-1</c> to
    /// <c>1</c> range the audio device allows. Useful for a slow-motion effect that drops
    /// every sound in the game at once.
    /// </remarks>
    /// <example>
    /// Drop every sound while a key is held:
    /// <code>
    /// load sfx clip 1, "powerup"
    /// sfx 1, 1
    /// set sfx loop 1, 1
    /// play sfx 1
    ///
    /// do
    ///   IF spaceKey()
    ///     set sfx group pitch 0, -0.5
    ///   ELSE
    ///     set sfx group pitch 0, 0
    ///   ENDIF
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to change. <c>0</c> is the group every sound starts in.</param>
    /// <param name="pitch">The shift, from <c>-1</c> (one octave down) to <c>1</c> (one octave up). <c>0</c> is unchanged.</param>
    /// <seealso cref="GetSfxGroupPitch">sfx group pitch</seealso>
    /// <seealso cref="SetSfxGroup">set sfx group</seealso>
    /// <seealso cref="SetSfxPitch">set sfx pitch</seealso>
    [FadeBasicCommand("set sfx group pitch")]
    public static void SetSfxGroupPitch(int groupId, float pitch)
    {
        if (pitch >= 1) pitch = 1;
        if (pitch <= -1) pitch = -1;

        var group = AudioMixSystem.GetGroup(groupId);
        group.pitch = pitch;
        AudioMixSystem.SetGroup(groupId, group);
    }

    /// <summary>
    /// <para>Returns the pitch shift of a group of sound effects.</para>
    /// </summary>
    /// <remarks>
    /// This is <c>0</c> for a group that was never given to
    /// <see cref="SetSfxGroupPitch">set sfx group pitch</see>.
    /// </remarks>
    /// <example>
    /// Show the pitch shift of the default group:
    /// <code>
    /// set sfx group pitch 0, 0.25
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "pitch: " + str$(sfx group pitch(0))
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to read.</param>
    /// <returns>The group's pitch shift.</returns>
    /// <seealso cref="SetSfxGroupPitch">set sfx group pitch</seealso>
    [FadeBasicCommand("sfx group pitch")]
    public static float GetSfxGroupPitch(int groupId)
    {
        return AudioMixSystem.GetGroup(groupId).pitch;
    }

    /// <summary>
    /// <para>Shifts the stereo position of a whole group of sound effects.</para>
    /// <para>It is added to each sound's own pan: <c>0</c> leaves the sounds where they are.</para>
    /// </summary>
    /// <remarks>
    /// What is heard for each sound is its own pan, from
    /// <see cref="SetSfxPan">set sfx pan</see>, plus this, kept inside the <c>-1</c> to
    /// <c>1</c> range.
    /// </remarks>
    /// <example>
    /// Push every sound to the left speaker:
    /// <code>
    /// load sfx clip 1, "powerup"
    /// sfx 1, 1
    /// set sfx loop 1, 1
    /// play sfx 1
    ///
    /// set sfx group pan 0, -1
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to change. <c>0</c> is the group every sound starts in.</param>
    /// <param name="pan">The shift, from <c>-1</c> (left) to <c>1</c> (right). <c>0</c> is unchanged.</param>
    /// <seealso cref="GetSfxGroupPan">sfx group pan</seealso>
    /// <seealso cref="SetSfxGroup">set sfx group</seealso>
    /// <seealso cref="SetSfxPan">set sfx pan</seealso>
    [FadeBasicCommand("set sfx group pan")]
    public static void SetSfxGroupPan(int groupId, float pan)
    {
        if (pan >= 1) pan = 1;
        if (pan <= -1) pan = -1;

        var group = AudioMixSystem.GetGroup(groupId);
        group.pan = pan;
        AudioMixSystem.SetGroup(groupId, group);
    }

    /// <summary>
    /// <para>Returns the stereo shift of a group of sound effects.</para>
    /// </summary>
    /// <remarks>
    /// This is <c>0</c> for a group that was never given to
    /// <see cref="SetSfxGroupPan">set sfx group pan</see>.
    /// </remarks>
    /// <example>
    /// Show the stereo shift of the default group:
    /// <code>
    /// set sfx group pan 0, 0.5
    ///
    /// font 1, "font"
    /// text 1, 470, 200, 1, "pan: " + str$(sfx group pan(0))
    ///
    /// do
    ///   sync
    /// loop
    /// </code>
    /// </example>
    /// <param name="groupId">The group to read.</param>
    /// <returns>The group's stereo shift.</returns>
    /// <seealso cref="SetSfxGroupPan">set sfx group pan</seealso>
    [FadeBasicCommand("sfx group pan")]
    public static float GetSfxGroupPan(int groupId)
    {
        return AudioMixSystem.GetGroup(groupId).pan;
    }
}
