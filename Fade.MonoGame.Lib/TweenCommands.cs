using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Peeks at the next available tween ID without claiming it.</para>
    /// <para>This doesn't reserve the ID, so another call could grab it before you do.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="ReserveTweenNextId">reserve tween id</see>
    /// instead, which actually claims the slot. This one is handy if you just need to know
    /// what the next ID would be. If you already know your ID, skip both of these and call
    /// <see cref="CreateTween">create basic tween</see> directly.
    /// </remarks>
    /// <example>
    /// Peek at the next tween ID before deciding whether to create one.
    /// <code>
    /// ` check what the next tween ID would be
    /// nextId = free tween id()
    /// print nextId
    /// </code>
    /// </example>
    /// <param name="tweenId">Receives the next free tween ID.</param>
    /// <returns>The next available tween ID (not yet reserved).</returns>
    /// <seealso cref="ReserveTweenNextId">reserve tween id</seealso>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    [FadeBasicCommand("free tween id")]
    public static int GetFreeTweenNextId(ref int tweenId)
    {
        tweenId = TweenSystem.highestTweenId + 1;
        return tweenId;
    }

    /// <summary>
    /// <para>Claims the next available tween ID and initializes its slot.</para>
    /// <para>The slot is created but the tween won't start until you call
    /// <see cref="CreateTween">create basic tween</see> to configure it.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to set up a tween ID ahead of time, for example to store
    /// it in an array before configuring the actual tween. If you don't need that setup
    /// step, just call <see cref="CreateTween">create basic tween</see> directly with a
    /// known ID. See also <see cref="GetFreeTweenNextId">free tween id</see> if you only
    /// need to peek without claiming.
    /// </remarks>
    /// <example>
    /// Reserve tween IDs for a staggered animation sequence.
    /// <code>
    /// ` reserve three tween IDs for a multi-part intro
    /// t1 = reserve tween id()
    /// t2 = reserve tween id()
    /// t3 = reserve tween id()
    ///
    /// ` now configure them with staggered delays
    /// create basic tween t1, 0, 255, 500, 0
    /// create basic tween t2, 0, 255, 500, 200
    /// create basic tween t3, 0, 255, 500, 400
    /// </code>
    /// </example>
    /// <param name="tweenId">Receives the reserved tween ID.</param>
    /// <returns>The newly reserved tween ID.</returns>
    /// <seealso cref="GetFreeTweenNextId">free tween id</seealso>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    [FadeBasicCommand("reserve tween id")]
    public static int ReserveTweenNextId(ref int tweenId)
    {
        GetFreeTweenNextId(ref tweenId);
        TweenSystem.GetTweenIndex(tweenId, out _, out _);
        return tweenId;
    }

    /// <summary>
    /// <para>Creates a tween that smoothly interpolates a value from start to end over a duration.</para>
    /// <para>Defaults to cubic ease-in-out. Change the curve with
    /// <see cref="SetTweenEasing">set tween easing</see> after creation.</para>
    /// </summary>
    /// <remarks>
    /// This is the main entry point for Fade's tween system. Tweens run on real time
    /// (milliseconds), not frame counts, so they're smooth regardless of frame rate. The
    /// system updates them automatically each frame.
    ///
    /// The typical pattern is: create a tween, then each frame read its current value with
    /// <see cref="GetTweenValue">tweenVal</see> and use that to drive a position, alpha,
    /// scale, or anything else you want to animate. Check
    /// <see cref="GetTweenPlaying">is tween done</see> to know when it's finished.
    ///
    /// By default a tween plays once and stops. Use
    /// <see cref="SetTweenType">set tween type</see> to make it loop or ping-pong.
    /// </remarks>
    /// <example>
    /// Slide a sprite from left to right over one second.
    /// <code>
    /// ` tween the X position from 0 to 640 in 1000ms
    /// tweenId = 1
    /// spriteId = 1
    /// create basic tween tweenId, 0, 640, 1000, 0
    ///
    /// set sync rate 16
    /// DO
    ///   x = tweenVal(tweenId)
    ///   set transform position spriteId, x, 240
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Fade in a sprite's alpha after a half-second delay.
    /// <code>
    /// ` fade alpha from 0 to 255 over 800ms, starting after 500ms
    /// tweenId = 2
    /// create basic tween tweenId, 0, 255, 800, 500
    ///
    /// set sync rate 16
    /// DO
    ///   a = tweenVal(tweenId)
    ///   set sprite alpha spriteId, a
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID to assign to this tween.</param>
    /// <param name="start">The starting value.</param>
    /// <param name="end">The ending value.</param>
    /// <param name="duration">How long the tween takes, in milliseconds.</param>
    /// <param name="delay">How long to wait before starting, in milliseconds. Pass <c>0</c> to start immediately.</param>
    /// <seealso cref="SetTweenEasing">set tween easing</seealso>
    /// <seealso cref="SetTweenType">set tween type</seealso>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    /// <seealso cref="GetTweenPlaying">is tween done</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    [FadeBasicCommand("create basic tween")]
    public static void CreateTween(int tweenId, float start, float end, float duration, float delay)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        tween.startValue = start;
        tween.endValue = end;
        tween.currValue = start;
        tween.interpolator = 0;
        tween.type = TweenInterpolator.EASE_IN_OUT_CUBIC;
        tween.executionType = TweenExecutionType.ONCE;
        tween.startTime = TweenSystem.currentTime + delay;
        tween.endTime = TweenSystem.currentTime + duration + delay;
        tween.isPlaying = true;
        TweenSystem.tweens[index] = tween;
    }

    /// <summary>
    /// <para>Sets the easing function for a tween.</para>
    /// <para>Call this right after <see cref="CreateTween">create basic tween</see> to
    /// override the default cubic ease-in-out.</para>
    /// </summary>
    /// <remarks>
    /// The easing type controls the shape of the interpolation curve, whether the tween
    /// starts slow and speeds up (ease-in), starts fast and slows down (ease-out), or
    /// something else entirely.
    ///
    /// If you don't call this, the tween uses cubic ease-in-out, which is a safe default
    /// for most UI and game animations.
    /// </remarks>
    /// <example>
    /// Create a tween with a linear easing so it moves at constant speed.
    /// <code>
    /// ` slide a sprite at constant speed
    /// tweenId = 1
    /// create basic tween tweenId, 0, 640, 2000, 0
    /// set tween easing tweenId, 0
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID of the tween.</param>
    /// <param name="easingType">The easing curve. Common values include linear, ease-in, ease-out, and cubic variants.</param>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    /// <seealso cref="SetTweenType">set tween type</seealso>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    [FadeBasicCommand("set tween easing")]
    public static void SetTweenEasing(int tweenId, int easingType)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out _);
        TweenSystem.tweens[index].type = (TweenInterpolator)easingType;
    }

    /// <summary>
    /// <para>Sets the execution behavior of a tween (play once, loop, ping-pong, etc.).</para>
    /// <para>By default tweens play once and stop. Call this right after
    /// <see cref="CreateTween">create basic tween</see> to change that.</para>
    /// </summary>
    /// <remarks>
    /// A looping tween repeats from start to end indefinitely. A ping-pong tween bounces
    /// back and forth between start and end. These are useful for ambient animations like
    /// bobbing, pulsing, or breathing effects.
    ///
    /// Note that <see cref="GetTweenPlaying">is tween done</see> will never return
    /// <c>1</c> for a looping or ping-pong tween, since they never finish.
    /// </remarks>
    /// <example>
    /// Make a sprite bob up and down forever with a ping-pong tween.
    /// <code>
    /// ` bob between y=200 and y=240 over 1 second, repeating forever
    /// tweenId = 1
    /// spriteId = 1
    /// create basic tween tweenId, 200, 240, 1000, 0
    /// set tween type tweenId, 2
    ///
    /// set sync rate 16
    /// DO
    ///   y = tweenVal(tweenId)
    ///   set transform position spriteId, 320, y
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID of the tween.</param>
    /// <param name="type">The execution type. Common values: once, loop, ping-pong.</param>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    /// <seealso cref="SetTweenEasing">set tween easing</seealso>
    /// <seealso cref="GetTweenPlaying">is tween done</seealso>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    [FadeBasicCommand("set tween type")]
    public static void SetTweenType(int tweenId, int type)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out _);
        TweenSystem.tweens[index].executionType = (TweenExecutionType)type;
    }

    // [FadeBasicCommand("play tween")]
    // public static void PlayTween(int tweenId)
    // {
    //     TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
    //     tween.isPlaying = true;
    //     TweenSystem.tweens[index] = tween;
    // }

    /// <summary>
    /// <para>Returns the current interpolated value of a tween.</para>
    /// <para>This is the main output of the tween system, the number that smoothly moves
    /// from start to end according to the easing curve.</para>
    /// </summary>
    /// <remarks>
    /// Read this every frame to drive your animation. If you created a tween from <c>0</c>
    /// to <c>100</c>, this will smoothly return values between 0 and 100 as the tween
    /// progresses. Feed this into <see cref="SetTransformPosition">set transform position</see>,
    /// <see cref="SetSpriteDiffuse(int, byte)">set sprite alpha</see>, or anything else you
    /// want to animate.
    ///
    /// If you need the raw 0-to-1 progress instead of the interpolated value, use
    /// <see cref="GetTweenRatio">tweenRatio</see>.
    /// </remarks>
    /// <example>
    /// Use a tween to animate a transform's X position.
    /// <code>
    /// ` smoothly slide an entity from x=50 to x=500
    /// tweenId = 1
    /// entityId = 1
    /// transform entityId, 50, 300
    /// create basic tween tweenId, 50, 500, 1500, 0
    ///
    /// set sync rate 16
    /// DO
    ///   x = tweenVal(tweenId)
    ///   set transform position entityId, x, 300
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Animate scale using two tweens at once.
    /// <code>
    /// ` grow an entity from half-size to full-size
    /// tweenX = 1
    /// tweenY = 2
    /// create basic tween tweenX, 0.5, 1.0, 600, 0
    /// create basic tween tweenY, 0.5, 1.0, 600, 0
    ///
    /// set sync rate 16
    /// DO
    ///   sx = tweenVal(tweenX)
    ///   sy = tweenVal(tweenY)
    ///   set transform scale entityId, sx, sy
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID of the tween.</param>
    /// <returns>The current tweened value, between start and end.</returns>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    /// <seealso cref="GetTweenRatio">tweenRatio</seealso>
    /// <seealso cref="GetTweenPlaying">is tween done</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    /// <seealso cref="SetTransformScale">set transform scale</seealso>
    [FadeBasicCommand("tweenVal")]
    public static float GetTweenValue(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.currValue;
    }

    /// <summary>
    /// <para>Returns the raw progress ratio of a tween, from <c>0</c> (just started) to <c>1</c> (finished).</para>
    /// <para>Unlike <see cref="GetTweenValue">tweenVal</see>, this gives you the
    /// un-interpolated progress, useful when you want to drive your own math.</para>
    /// </summary>
    /// <remarks>
    /// Most of the time you'll want <see cref="GetTweenValue">tweenVal</see> instead, which
    /// gives you the actual number between start and end. This is for cases where you need
    /// the raw 0-to-1 ratio to feed into your own interpolation logic, for example
    /// blending between two colors or computing a custom curve.
    /// </remarks>
    /// <example>
    /// Use the ratio to blend between two colors manually.
    /// <code>
    /// ` blend from red to blue using the raw ratio
    /// tweenId = 1
    /// create basic tween tweenId, 0, 1, 2000, 0
    ///
    /// set sync rate 16
    /// DO
    ///   r = tweenRatio(tweenId)
    ///   red = 255 * (1.0 - r)
    ///   blue = 255 * r
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID of the tween.</param>
    /// <returns>The progress ratio, from <c>0.0</c> (just started) to <c>1.0</c> (finished).</returns>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    [FadeBasicCommand("tweenRatio")]
    public static float GetTweenRatio(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.interpolator;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if a tween has finished playing.</para>
    /// <para>A tween is "done" when its progress ratio reaches <c>1</c> or beyond. Looping
    /// and ping-pong tweens never finish.</para>
    /// </summary>
    /// <remarks>
    /// Use this to sequence actions after a tween completes, for example destroying an
    /// entity after its fade-out finishes, or starting the next animation in a chain.
    ///
    /// If you need to wait for several tweens at once, use
    /// <see cref="GetAnyTweenPlaying">any tweens running</see> instead of checking each
    /// one individually.
    /// </remarks>
    /// <example>
    /// Wait for a slide-in to finish, then print a message.
    /// <code>
    /// ` slide a title in from the left
    /// tweenId = 1
    /// create basic tween tweenId, -200, 320, 1000, 0
    ///
    /// set sync rate 16
    /// DO
    ///   x = tweenVal(tweenId)
    ///   set transform position titleId, x, 100
    ///
    ///   done = is tween done(tweenId)
    ///   IF done = 1 THEN
    ///     print "title is in place!"
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenId">The ID of the tween.</param>
    /// <returns><c>1</c> if the tween's progress ratio has reached <c>1</c> or beyond.</returns>
    /// <seealso cref="GetAnyTweenPlaying">any tweens running</seealso>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    /// <seealso cref="SetTransformPosition">set transform position</seealso>
    [FadeBasicCommand("is tween done")]
    public static bool GetTweenPlaying(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.interpolator >= 1;
    }

    /// <summary>
    /// <para>Checks if any of the given tweens are still running.</para>
    /// <para>Returns <c>1</c> if at least one tween in the list hasn't finished yet.
    /// Returns <c>0</c> only when every tween is done.</para>
    /// </summary>
    /// <remarks>
    /// This is the batch version of <see cref="GetTweenPlaying">is tween done</see>.
    /// Instead of checking each tween individually, pass them all in and get a single
    /// answer. Common use case: you've kicked off several tweens to animate a UI transition,
    /// and you want to wait until they're all finished before proceeding.
    ///
    /// Since this returns <c>1</c> while tweens are still going, you'd typically use it
    /// in a loop condition: keep calling <see cref="Sync(VirtualMachine)">sync</see> while
    /// <c>any tweens running</c> is true.
    /// </remarks>
    /// <example>
    /// Wait for all UI tweens to finish before showing a menu.
    /// <code>
    /// ` kick off three staggered fade-in tweens
    /// t1 = 1
    /// t2 = 2
    /// t3 = 3
    /// create basic tween t1, 0, 255, 400, 0
    /// create basic tween t2, 0, 255, 400, 150
    /// create basic tween t3, 0, 255, 400, 300
    ///
    /// ` wait until all three are done
    /// set sync rate 16
    /// DO
    ///   running = any tweens running(t1, t2, t3)
    ///   IF running = 0 THEN
    ///     print "all animations finished!"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="tweenIds">One or more tween IDs to check.</param>
    /// <returns><c>1</c> if at least one tween is still running, <c>0</c> if all are done.</returns>
    /// <seealso cref="GetTweenPlaying">is tween done</seealso>
    /// <seealso cref="CreateTween">create basic tween</seealso>
    /// <seealso cref="GetTweenValue">tweenVal</seealso>
    [FadeBasicCommand("any tweens running")]
    public static bool GetAnyTweenPlaying(params int[] tweenIds)
    {
        for (var i = 0; i < tweenIds.Length; i++)
        {
            var tweenId = tweenIds[i];
            TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
            if (tween.interpolator < 1) return true;
        }

        return false;
    }


}
