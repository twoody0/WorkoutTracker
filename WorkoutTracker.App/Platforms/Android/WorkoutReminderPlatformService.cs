using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
using WorkoutTracker.PlatformPermissions;
using WorkoutTracker.Services;

namespace WorkoutTracker.Platforms.Android;

public sealed class WorkoutReminderPlatformService : WorkoutReminderServiceBase
{
    public WorkoutReminderPlatformService(IWorkoutService workoutService, IWorkoutScheduleService workoutScheduleService)
        : base(workoutService, workoutScheduleService)
    {
    }

    protected override Task<bool> IsAuthorizedAsync()
    {
        var context = MainActivity.Current ?? global::Android.App.Application.Context;
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return Task.FromResult(true);
        }

        var hasPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) == Permission.Granted;
        return Task.FromResult(hasPermission);
    }

    protected override async Task<bool> RequestAuthorizationAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return true;
        }

        var status = await MauiPermissions.CheckStatusAsync<NotificationPermission>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        status = await MainThread.InvokeOnMainThreadAsync(() => MauiPermissions.RequestAsync<NotificationPermission>());
        return status == PermissionStatus.Granted;
    }

    protected override Task ScheduleReminderAsync(DateTime reminderAtLocal, string title, string body)
    {
        var context = MainActivity.Current ?? global::Android.App.Application.Context;
        CreateReminderChannel(context);

        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager == null)
        {
            return Task.CompletedTask;
        }

        var pendingIntent = BuildPendingIntent(context, title, body);
        alarmManager.Cancel(pendingIntent);

        var triggerAtMillis = new DateTimeOffset(reminderAtLocal).ToUnixTimeMilliseconds();
        if (CanScheduleExactAlarms(alarmManager))
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }

        return Task.CompletedTask;
    }

    protected override Task CancelReminderAsync()
    {
        var context = MainActivity.Current ?? global::Android.App.Application.Context;
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager == null)
        {
            return Task.CompletedTask;
        }

        var pendingIntent = BuildPendingIntent(context, string.Empty, string.Empty);
        alarmManager.Cancel(pendingIntent);

        var notificationManager = NotificationManager.FromContext(context);
        notificationManager?.Cancel(WorkoutReminderReceiver.ReminderNotificationId);
        return Task.CompletedTask;
    }

    internal static void CreateReminderChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (notificationManager?.GetNotificationChannel(WorkoutReminderReceiver.ReminderChannelId) != null)
        {
            return;
        }

        var channel = new NotificationChannel(
            WorkoutReminderReceiver.ReminderChannelId,
            "Workout reminders",
            NotificationImportance.Default)
        {
            Description = "Friendly reminders when it's later than your usual workout time and today's session is still waiting."
        };

        notificationManager?.CreateNotificationChannel(channel);
    }

    private static PendingIntent BuildPendingIntent(Context context, string title, string body)
    {
        var intent = new Intent(context, typeof(WorkoutReminderReceiver));
        intent.SetAction(WorkoutReminderReceiver.ReminderAction);
        intent.PutExtra(WorkoutReminderReceiver.TitleExtra, title);
        intent.PutExtra(WorkoutReminderReceiver.BodyExtra, body);

        var flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            flags |= PendingIntentFlags.Immutable;
        }

        return PendingIntent.GetBroadcast(context, WorkoutReminderReceiver.ReminderRequestCode, intent, flags);
    }

    private static bool CanScheduleExactAlarms(AlarmManager alarmManager)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.S)
        {
            return Build.VERSION.SdkInt >= BuildVersionCodes.M;
        }

        return alarmManager.CanScheduleExactAlarms();
    }
}
