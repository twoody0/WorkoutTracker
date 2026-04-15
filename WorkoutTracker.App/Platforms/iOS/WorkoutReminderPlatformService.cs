using Foundation;
using UserNotifications;
using WorkoutTracker.Services;

namespace WorkoutTracker.Platforms.iOS;

public sealed class WorkoutReminderPlatformService : WorkoutReminderServiceBase
{
    private const string ReminderIdentifier = "workout.reminder.today";

    public WorkoutReminderPlatformService(IWorkoutService workoutService, IWorkoutScheduleService workoutScheduleService)
        : base(workoutService, workoutScheduleService)
    {
    }

    protected override async Task<bool> IsAuthorizedAsync()
    {
        var center = UNUserNotificationCenter.Current;
        var settings = await center.GetNotificationSettingsAsync();
        if (settings.AuthorizationStatus == UNAuthorizationStatus.Authorized ||
            settings.AuthorizationStatus == UNAuthorizationStatus.Provisional)
        {
            return true;
        }

        return false;
    }

    protected override async Task<bool> RequestAuthorizationAsync()
    {
        var center = UNUserNotificationCenter.Current;
        var settings = await center.GetNotificationSettingsAsync();
        if (settings.AuthorizationStatus == UNAuthorizationStatus.Authorized ||
            settings.AuthorizationStatus == UNAuthorizationStatus.Provisional)
        {
            return true;
        }

        var (granted, _) = await center.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        return granted;
    }

    protected override Task ScheduleReminderAsync(DateTime reminderAtLocal, string title, string body)
    {
        var center = UNUserNotificationCenter.Current;
        center.RemovePendingNotificationRequests([ReminderIdentifier]);

        var content = new UNMutableNotificationContent
        {
            Title = title,
            Body = body,
            Sound = UNNotificationSound.Default
        };

        var localDate = reminderAtLocal.Kind == DateTimeKind.Local
            ? reminderAtLocal
            : reminderAtLocal.ToLocalTime();

        var dateComponents = new NSDateComponents
        {
            Year = localDate.Year,
            Month = localDate.Month,
            Day = localDate.Day,
            Hour = localDate.Hour,
            Minute = localDate.Minute
        };

        var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, false);
        var request = UNNotificationRequest.FromIdentifier(ReminderIdentifier, content, trigger);
        center.AddNotificationRequest(request, null);
        return Task.CompletedTask;
    }

    protected override Task CancelReminderAsync()
    {
        var center = UNUserNotificationCenter.Current;
        center.RemovePendingNotificationRequests([ReminderIdentifier]);
        center.RemoveDeliveredNotifications([ReminderIdentifier]);
        return Task.CompletedTask;
    }
}
