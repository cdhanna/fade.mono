using System;
using System.Collections.Generic;

namespace Fade.MonoGame.Game;

public enum TweenInterpolator
{
    LINEAR,
    EASE_IN_OUT_CUBIC,
    EASE_OUT_BOUNCE,
}

public enum TweenExecutionType
{
    ONCE,
    ONCE_AND_BACK
}

public struct Tween
{
    public int id;

    public bool isPlaying;
    public float interpolator;
    
    public float currValue;
    public float startValue;
    public float endValue;

    public double startTime;
    public double endTime;

    public TweenExecutionType executionType;
    public TweenInterpolator type;
}

public static class TweenSystem
{
    public static double currentTime;
    public const int MAX_TWEEN_COUNT = 10_000_000;

    public static Tween[] tweens = new Tween[MAX_TWEEN_COUNT];
    public static int tweenCount = 0;
    private static Dictionary<int, int> _tweenMap = new Dictionary<int, int>();
    public static int highestTweenId;
    
    public static void GetTweenIndex(int tweenId, out int index, out Tween tween)
    {
        if (!_tweenMap.TryGetValue(tweenId, out index))
        {
            highestTweenId = tweenId > highestTweenId ? tweenId : highestTweenId;
            index = _tweenMap[tweenId] = tweenCount;
            tween = new Tween()
            {
                id = tweenId,
                isPlaying = false
            };
            tweens[index] = tween;
            tweenCount++;
        }
        else
        {
            tween = tweens[index];
        }
    }

    public static void ProcessTweens()
    {
        for (var i = 0; i < tweenCount; i++)
        {
            var tween = tweens[i];
            if (!tween.isPlaying) continue;

            if (currentTime > tween.endTime)
            {
                tweens[i].interpolator = 1;
                continue;
            }
            if (currentTime < tween.startTime)
            {
                tweens[i].interpolator = 0;
                continue;
            }

            var duration = tween.endTime - tween.startTime;
            var ratio = (tween.endTime - currentTime) / duration;

            // var n = (float)EaseInOutCubic(1 - ratio);
            var n = (float)(1f-ratio);
            tween.interpolator = n;


            switch (tween.executionType)
            {
                case TweenExecutionType.ONCE:
                    break;
                case TweenExecutionType.ONCE_AND_BACK:
                    // stretch n from [0-1] to [0-1-0]
                    n *= 2; // [0-2]
                    if (n > 1)
                    {
                        n = 2 - n; 
                    }
                    break;
            }
            
            switch (tween.type)
            {
                case TweenInterpolator.LINEAR:
                    n = n;
                    break;
                case TweenInterpolator.EASE_IN_OUT_CUBIC:
                    n = (float)EaseInOutCubic(n);
                    break;
                case TweenInterpolator.EASE_OUT_BOUNCE:
                    n = EaseOutBounce(n);
                    break;
            }
            
            var v = tween.startValue + n * (tween.endValue - tween.startValue);
            if (n <= 0)
            {
                tween.currValue = tween.startValue;
                n = 0;
            }

            if (n >= 1)
            {
                tween.currValue = tween.endValue;
                n = 1;
            }
            tween.currValue = v;
            tweens[i] = tween;
            /*
             * e = 10
             * s = 2
             * c = 4
             *
             * d = 10-2 = 8
             * r = 10-4 / d = 6 / 8 = .75
             */

        }
    }

    // for a list of easing functions, 
    // https://easings.net/
    
    
    static double EaseInOutCubic(double x)
    {
        
        // function easeInOutCubic(x: number): number {
        return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
        // }
    }
    
    public static float EaseOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (x < 1f / d1)
        {
            return n1 * x * x;
        }
        else if (x < 2f / d1)
        {
            x -= 1.5f / d1;
            return n1 * x * x + 0.75f;
        }
        else if (x < 2.5f / d1)
        {
            x -= 2.25f / d1;
            return n1 * x * x + 0.9375f;
        }
        else
        {
            x -= 2.625f / d1;
            return n1 * x * x + 0.984375f;
        }
    }
}