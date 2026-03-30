using Android.App;
using Android.Content.PM;
using Android.OS;
using Java.Util;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Health.Connect.Client;
using AndroidX.Core.View;
using MauiApplication = Microsoft.Maui.Controls.Application;
using AndroidColor = Android.Graphics.Color;

namespace WorkoutTracker;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private static MainActivity? _current;
    private ActivityResultLauncher? _healthPermissionLauncher;
    private TaskCompletionSource<IReadOnlyCollection<string>>? _healthPermissionRequest;

    public static MainActivity? Current => _current;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Ensure the activity switches off the splash theme before any Material-backed
        // dialogs or controls are created on physical Android devices.
        SetTheme(Resource.Style.Maui_MainTheme);
        base.OnCreate(savedInstanceState);
        _current = this;
        _healthPermissionLauncher = RegisterForActivityResult(
            PermissionController.CreateRequestPermissionResultContract(),
            new HealthPermissionResultCallback(this));
        ApplySystemBarColors();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplySystemBarColors();
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(_current, this))
        {
            _current = null;
        }

        if (MauiApplication.Current is not null)
        {
            MauiApplication.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        base.OnDestroy();
    }

    private void ApplySystemBarColors()
    {
        var isDarkTheme = MauiApplication.Current?.RequestedTheme == AppTheme.Dark;
        var backgroundColor = isDarkTheme
            ? AndroidColor.ParseColor("#0F1720")
            : AndroidColor.ParseColor("#FFFFFF");

        Window?.SetNavigationBarColor(backgroundColor);
        Window?.SetStatusBarColor(backgroundColor);

        if (Window?.DecorView is null)
        {
            return;
        }

        var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (insetsController is null)
        {
            return;
        }

        insetsController.AppearanceLightNavigationBars = !isDarkTheme;
        insetsController.AppearanceLightStatusBars = !isDarkTheme;

        if (MauiApplication.Current is not null)
        {
            MauiApplication.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            MauiApplication.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ApplySystemBarColors();
    }

    public Task<IReadOnlyCollection<string>> RequestHealthPermissionsAsync(IEnumerable<string> permissions)
    {
        if (_healthPermissionLauncher == null)
        {
            throw new InvalidOperationException("Health permission launcher is not ready.");
        }

        _healthPermissionRequest?.TrySetCanceled();
        _healthPermissionRequest = new TaskCompletionSource<IReadOnlyCollection<string>>();

        var requestedPermissions = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        _healthPermissionLauncher.Launch(new HashSet(requestedPermissions));
        return _healthPermissionRequest.Task;
    }

    private void CompleteHealthPermissionRequest(Java.Lang.Object? result)
    {
        var grantedPermissions = result as System.Collections.IEnumerable;
        var granted = grantedPermissions?
            .Cast<object>()
            .Select(permission => permission?.ToString())
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission!)
            .ToArray()
            ?? Array.Empty<string>();

        _healthPermissionRequest?.TrySetResult(granted);
        _healthPermissionRequest = null;
    }

    private sealed class HealthPermissionResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        private readonly MainActivity _activity;

        public HealthPermissionResultCallback(MainActivity activity)
        {
            _activity = activity;
        }

        public void OnActivityResult(Java.Lang.Object? result)
        {
            _activity.CompleteHealthPermissionRequest(result);
        }
    }
}
