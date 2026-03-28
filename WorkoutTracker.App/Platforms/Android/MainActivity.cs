using Android.App;
using Android.Content.PM;
using Android.OS;
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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Ensure the activity switches off the splash theme before any Material-backed
        // dialogs or controls are created on physical Android devices.
        SetTheme(Resource.Style.Maui_MainTheme);
        base.OnCreate(savedInstanceState);
        ApplySystemBarColors();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplySystemBarColors();
    }

    protected override void OnDestroy()
    {
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
}
