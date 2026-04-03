#if ANDROID
using Android.Graphics;
using PlatformView = Android.Views.View;

namespace WorkoutTracker.Views;

public partial class BodyWeightPromptPage
{
    partial void UpdatePageBackgroundBlur(bool isEnabled)
    {
        if (_parentPage.Handler?.PlatformView is not PlatformView platformView)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            platformView.SetRenderEffect(isEnabled
                ? RenderEffect.CreateBlurEffect(20f, 20f, Shader.TileMode.Clamp)
                : null);
        }

        platformView.Alpha = isEnabled ? 0.92f : 1f;
    }
}
#endif
