using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace WorkoutTracker.Helpers;

public static class ColorAnimationExtensions
{
    public static Task<bool> ColorTo(this VisualElement element, Color fromColor, Color toColor, Action<Color> callback, uint duration = 250, Easing easing = null)
    {
        easing ??= Easing.Linear;

        var animation = new Animation(v =>
        {
            callback(Lerp(fromColor, toColor, (float)v));
        });

        var tcs = new TaskCompletionSource<bool>();
        animation.Commit(element, "ColorTo", 16, duration, easing, (v, c) => tcs.SetResult(c));
        return tcs.Task;
    }

    public static Color Lerp(Color from, Color to, float t)
    {
        return new Color(
            from.Red + (to.Red - from.Red) * t,
            from.Green + (to.Green - from.Green) * t,
            from.Blue + (to.Blue - from.Blue) * t,
            from.Alpha + (to.Alpha - from.Alpha) * t
        );
    }
}
