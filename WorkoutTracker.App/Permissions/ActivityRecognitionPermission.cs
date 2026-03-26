namespace WorkoutTracker.PlatformPermissions;

#if ANDROID
using Microsoft.Maui.ApplicationModel;

#pragma warning disable CA1416
public sealed class ActivityRecognitionPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
    [
        (global::Android.Manifest.Permission.ActivityRecognition, true)
    ];
}
#pragma warning restore CA1416
#else
public sealed class ActivityRecognitionPermission
{
}
#endif
