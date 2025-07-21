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
            var r = fromColor.Red + (toColor.Red - fromColor.Red) * v;
            var g = fromColor.Green + (toColor.Green - fromColor.Green) * v;
            var b = fromColor.Blue + (toColor.Blue - fromColor.Blue) * v;
            var a = fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * v;

            callback(new Color((float)r, (float)g, (float)b, (float)a));
        });

        var tcs = new TaskCompletionSource<bool>();
        animation.Commit(element, "ColorTo", 16, duration, easing, (v, c) => tcs.SetResult(c));
        return tcs.Task;
    }
}
