using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui;

namespace WorkoutTracker
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Check if ACTIVITY_RECOGNITION permission is granted.
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ActivityRecognition) != (int)Permission.Granted)
            {
                // Request the permission from the user.
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ActivityRecognition }, 0);
            }
        }
    }
}
