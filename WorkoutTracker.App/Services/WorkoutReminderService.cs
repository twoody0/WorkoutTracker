using Microsoft.Maui.Storage;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public interface IWorkoutReminderService
{
    bool IsReminderEnabled { get; }
    Task RefreshWorkoutReminderAsync();
    Task SetReminderEnabledAsync(bool isEnabled);
    Task<bool> RequestPermissionIfNeededAsync();
}

public abstract class WorkoutReminderServiceBase : IWorkoutReminderService
{
    private static readonly TimeSpan ReminderDelayAfterUsualWorkoutTime = TimeSpan.FromMinutes(75);
    private static readonly TimeSpan FallbackWorkoutTime = new(18, 0, 0);
    private const string ReminderEnabledPreferenceKey = "workout_reminder.enabled";

    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutScheduleService _workoutScheduleService;

    protected WorkoutReminderServiceBase(IWorkoutService workoutService, IWorkoutScheduleService workoutScheduleService)
    {
        _workoutService = workoutService;
        _workoutScheduleService = workoutScheduleService;
    }

    public bool IsReminderEnabled => Preferences.Get(ReminderEnabledPreferenceKey, false);

    public async Task RefreshWorkoutReminderAsync()
    {
        if (!IsReminderEnabled)
        {
            await CancelReminderAsync();
            return;
        }

        if (!await IsAuthorizedAsync())
        {
            await CancelReminderAsync();
            return;
        }

        var today = DateTime.Today;
        var todaysPlannedWorkouts = _workoutScheduleService.GetActivePlanWorkoutsForDate(today);
        if (todaysPlannedWorkouts.Count == 0)
        {
            await CancelReminderAsync();
            return;
        }

        var todaysLoggedWorkouts = (await _workoutService.GetWorkouts())
            .Where(workout => workout.StartTime.Date == today && !workout.IsWarmup)
            .ToList();
        if (todaysLoggedWorkouts.Count > 0)
        {
            await CancelReminderAsync();
            return;
        }

        var allWorkouts = await _workoutService.GetWorkouts();
        var reminderAt = GetReminderTime(today, allWorkouts);
        if (reminderAt <= DateTime.Now)
        {
            reminderAt = DateTime.Now.AddMinutes(1);
        }

        var plannedSessionCount = todaysPlannedWorkouts
            .Select(workout => $"{workout.Type}|{workout.Name}|{workout.MuscleGroup}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var title = "Friendly Workout Reminder";
        var body = plannedSessionCount <= 1
            ? "Your usual workout time has passed, but today's workout is still here whenever you're ready."
            : $"Your usual workout time has passed, and you still have {plannedSessionCount} planned workouts waiting for today.";

        await ScheduleReminderAsync(reminderAt, title, body);
    }

    public async Task SetReminderEnabledAsync(bool isEnabled)
    {
        Preferences.Set(ReminderEnabledPreferenceKey, isEnabled);

        if (!isEnabled)
        {
            await CancelReminderAsync();
            return;
        }

        await RefreshWorkoutReminderAsync();
    }

    public async Task<bool> RequestPermissionIfNeededAsync()
    {
        if (await IsAuthorizedAsync())
        {
            return true;
        }

        return await RequestAuthorizationAsync();
    }

    private static DateTime GetReminderTime(DateTime today, IEnumerable<Workout> allWorkouts)
    {
        var recentWorkoutMinutes = allWorkouts
            .Where(workout => !workout.IsWarmup)
            .Where(workout => workout.StartTime.Date >= today.AddDays(-30) && workout.StartTime.Date < today)
            .OrderByDescending(workout => workout.StartTime)
            .Take(14)
            .Select(workout => workout.StartTime.TimeOfDay.TotalMinutes)
            .OrderBy(minutes => minutes)
            .ToList();

        var usualWorkoutTime = recentWorkoutMinutes.Count == 0
            ? FallbackWorkoutTime
            : TimeSpan.FromMinutes(recentWorkoutMinutes[recentWorkoutMinutes.Count / 2]);

        return today.Add(usualWorkoutTime).Add(ReminderDelayAfterUsualWorkoutTime);
    }

    protected abstract Task<bool> IsAuthorizedAsync();
    protected abstract Task<bool> RequestAuthorizationAsync();
    protected abstract Task ScheduleReminderAsync(DateTime reminderAtLocal, string title, string body);
    protected abstract Task CancelReminderAsync();
}

public sealed class WorkoutReminderServiceNull : IWorkoutReminderService
{
    public bool IsReminderEnabled => false;

    public Task RefreshWorkoutReminderAsync() => Task.CompletedTask;
    public Task SetReminderEnabledAsync(bool isEnabled) => Task.CompletedTask;
    public Task<bool> RequestPermissionIfNeededAsync() => Task.FromResult(false);
}
