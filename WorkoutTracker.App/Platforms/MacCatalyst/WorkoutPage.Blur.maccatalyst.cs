#if MACCATALYST
using UIKit;

namespace WorkoutTracker.Views;

public partial class WorkoutPage
{
    private UIVisualEffectView? _blurEffectView;

    partial void UpdateBackgroundBlur(bool isEnabled)
    {
        if (WorkoutContentHost?.Handler?.PlatformView is not UIView platformView)
        {
            return;
        }

        if (!isEnabled)
        {
            _blurEffectView?.RemoveFromSuperview();
            return;
        }

        _blurEffectView ??= new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial))
        {
            UserInteractionEnabled = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
        };

        _blurEffectView.Frame = platformView.Bounds;

        if (_blurEffectView.Superview != platformView)
        {
            platformView.AddSubview(_blurEffectView);
        }

        platformView.BringSubviewToFront(_blurEffectView);
    }
}
#endif
