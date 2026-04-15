using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace WorkoutTracker.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class WorkoutReminderReceiver : BroadcastReceiver
{
    internal const string ReminderAction = "com.companyname.megnor.WORKOUT_REMINDER";
    internal const string ReminderChannelId = "workout_reminder_channel";
    internal const int ReminderNotificationId = 4201;
    internal const int ReminderRequestCode = 4201;
    internal const string TitleExtra = "title";
    internal const string BodyExtra = "body";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
        {
            return;
        }

        WorkoutReminderPlatformService.CreateReminderChannel(context);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName);
        PendingIntent? pendingIntent = null;
        if (launchIntent != null)
        {
            var flags = PendingIntentFlags.UpdateCurrent;
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                flags |= PendingIntentFlags.Immutable;
            }

            pendingIntent = PendingIntent.GetActivity(context, ReminderRequestCode, launchIntent, flags);
        }

        var title = intent?.GetStringExtra(TitleExtra) ?? "Workout Reminder";
        var body = intent?.GetStringExtra(BodyExtra) ?? "Today's workout is still waiting for you.";

        var notification = new NotificationCompat.Builder(context, ReminderChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
            .SetAutoCancel(true)
            .SetPriority((int)NotificationPriority.Default)
            .SetContentIntent(pendingIntent)
            .Build();

        NotificationManagerCompat.From(context).Notify(ReminderNotificationId, notification);
    }
}
