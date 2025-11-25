using Fade.MonoGame.Game;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    
    [FadeBasicCommand("free tween id")]
    public static int GetFreeTweenNextId(ref int tweenId)
    {
        tweenId = TweenSystem.highestTweenId + 1;
        return tweenId;
    }
    
    [FadeBasicCommand("reserve tween id")]
    public static int ReserveTweenNextId(ref int tweenId)
    {
        GetFreeTweenNextId(ref tweenId);
        TweenSystem.GetTweenIndex(tweenId, out _, out _);
        return tweenId;
    }
    
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

    [FadeBasicCommand("set tween easing")]
    public static void SetTweenEasing(int tweenId, int easingType)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out _);
        TweenSystem.tweens[index].type = (TweenInterpolator)easingType;
    }
    
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

    [FadeBasicCommand("tweenVal")]
    public static float GetTweenValue(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.currValue;
    }
    
    [FadeBasicCommand("tweenRatio")]
    public static float GetTweenRatio(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.interpolator;
    }
    
    [FadeBasicCommand("is tween done")]
    public static bool GetTweenPlaying(int tweenId)
    {
        TweenSystem.GetTweenIndex(tweenId, out var index, out var tween);
        return tween.interpolator >= 1;
    }
    
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