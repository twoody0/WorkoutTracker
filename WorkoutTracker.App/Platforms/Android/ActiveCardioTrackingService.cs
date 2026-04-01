using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Services;

namespace WorkoutTracker.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeHealth)]
public sealed class ActiveCardioTrackingService : Service
{
    public const string StartAction = "WorkoutTracker.action.START_CARDIO_TRACKING";
    public const string StopAction = "WorkoutTracker.action.STOP_CARDIO_TRACKING";
    public const string SessionStartedAtExtra = "WorkoutTracker.extra.SESSION_STARTED_AT_UTC";

    private const string NotificationChannelId = "cardio_tracking";
    private const int NotificationId = 4107;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var tracker = App.Services.GetRequiredService<IStepCounterService>() as StepCounterServiceAndroid;
        if (tracker == null)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var action = intent?.Action;
        if (string.Equals(action, StopAction, StringComparison.Ordinal))
        {
            tracker.EndForegroundTracking();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (!string.Equals(action, StartAction, StringComparison.Ordinal))
        {
            return StartCommandResult.Sticky;
        }

        var sessionStartedAtText = intent?.GetStringExtra(SessionStartedAtExtra);
        if (!DateTimeOffset.TryParse(sessionStartedAtText, out var sessionStartedAtUtc))
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        EnsureNotificationChannel();
        StartAsForegroundService();
        tracker.BeginForegroundTracking(sessionStartedAtUtc);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        if (notificationManager?.GetNotificationChannel(NotificationChannelId) != null)
        {
            return;
        }

        var channel = new NotificationChannel(
            NotificationChannelId,
            "Cardio Tracking",
            NotificationImportance.Default)
        {
            Description = "Keeps cardio step tracking active while your session is running."
        };
        channel.LockscreenVisibility = NotificationVisibility.Public;

        notificationManager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var openAppIntent = new Intent(this, typeof(MainActivity));
        openAppIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetActivity(this, 0, openAppIntent, pendingFlags);
        var builder = new NotificationCompat.Builder(this, NotificationChannelId);
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle("Cardio session active");
        builder.SetContentText("Megnor is tracking your cardio steps in the background.");
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetCategory(NotificationCompat.CategoryWorkout);
        builder.SetVisibility((int)NotificationCompat.VisibilityPublic);
        builder.SetPriority((int)NotificationCompat.PriorityDefault);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            builder.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
        }

        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        return builder.Build() ?? throw new InvalidOperationException("Unable to build the cardio tracking notification.");
    }

    private void StartAsForegroundService()
    {
        var notification = BuildNotification();
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeHealth);
            return;
        }

        StartForeground(NotificationId, notification);
    }

    public static Intent CreateStartIntent(Context context, DateTimeOffset sessionStartedAtUtc)
    {
        var intent = new Intent(context, typeof(ActiveCardioTrackingService));
        intent.SetAction(StartAction);
        intent.PutExtra(SessionStartedAtExtra, sessionStartedAtUtc.ToString("O"));
        return intent;
    }

    public static Intent CreateStopIntent(Context context)
    {
        var intent = new Intent(context, typeof(ActiveCardioTrackingService));
        intent.SetAction(StopAction);
        return intent;
    }
}
