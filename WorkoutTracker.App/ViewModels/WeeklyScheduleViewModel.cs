using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WeeklyScheduleViewModel : BaseViewModel
{
    private const double ProgressionEligibilityThreshold = 0.7;

    private readonly IWorkoutScheduleService _scheduleService;
    private readonly IWorkoutService _workoutService;
    private readonly Dictionary<DayOfWeek, WeeklyScheduleDayGroup> _dayGroupsByDay = new();
    private string? _lastCompletedPlanPromptKey;
    private int _lastLoadedScheduleVersion = -1;
    private DateTime _lastLoadedDate = DateTime.MinValue;

    public ICommand ChangeWorkoutDayCommand { get; }
    public ICommand EditDayCommand { get; }
    public ICommand ToggleDayCommand { get; }
    public ICommand ViewPlanPreviewCommand { get; }
    public ICommand ManageExerciseSubstitutionsCommand { get; }
    public ObservableCollection<WeeklyScheduleDayGroup> WeeklySchedule { get; } = new();
    public string ActivePlanName => _scheduleService.ActivePlan?.Name ?? "No active plan";
    public string ActivePlanTimelineSummary => _scheduleService.GetActivePlanTimelineSummary();
    public bool HasActivePlan => _scheduleService.ActivePlan != null;

    public WeeklyScheduleViewModel(IWorkoutScheduleService scheduleService, IWorkoutService workoutService)
    {
        _scheduleService = scheduleService;
        _workoutService = workoutService;
        ChangeWorkoutDayCommand = new Command<Workout>(ChangeWorkoutDay);
        EditDayCommand = new Command<DayOfWeek>(EditDay);
        ToggleDayCommand = new Command<WeeklyScheduleDayGroup>(ToggleDay);
        ViewPlanPreviewCommand = new Command(async () => await ViewPlanPreviewAsync());
        ManageExerciseSubstitutionsCommand = new Command(async () => await ManageExerciseSubstitutionsAsync());
        LoadSchedule();
    }

    public async Task OnAppearingAsync()
    {
        var today = DateTime.Today;
        var currentScheduleVersion = _scheduleService.ScheduleVersion;
        if (_lastLoadedScheduleVersion != currentScheduleVersion || _lastLoadedDate != today)
        {
            LoadSchedule();
        }

        await PromptForCompletedPlanAsync();
    }

    private async void EditDay(DayOfWeek day)
    {
        var editPage = new EditDayPage(day, _scheduleService);
        await Shell.Current.Navigation.PushAsync(editPage);
    }

    private async void ChangeWorkoutDay(Workout workout)
    {
        if (workout == null)
        {
            return;
        }

        var days = Enum.GetNames(typeof(DayOfWeek));
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        string selectedDay = await page.DisplayActionSheet(
            "Move Workout To:",
            "Cancel",
            null,
            days);

        if (!string.IsNullOrWhiteSpace(selectedDay) && Enum.TryParse(selectedDay, out DayOfWeek newDay))
        {
            workout.Day = newDay;
            LoadSchedule();
            await page.DisplayAlert("Workout Moved", $"{workout.Name} is now scheduled for {newDay}.", "OK");
        }
    }

    private void LoadSchedule()
    {
        var schedule = _scheduleService.GetWeeklySchedule();
        var today = DateTime.Today.DayOfWeek;
        var orderedDays = Enum.GetValues<DayOfWeek>()
            .OrderBy(day => ((int)day - (int)today + 7) % 7)
            .ToList();

        foreach (var day in orderedDays)
        {
            var workouts = schedule.ContainsKey(day)
                ? schedule[day]
                : new List<Workout>();

            if (_dayGroupsByDay.TryGetValue(day, out var existingGroup))
            {
                existingGroup.UpdateWorkouts(workouts);
                continue;
            }

            _dayGroupsByDay[day] = new WeeklyScheduleDayGroup(
                day,
                workouts,
                isToday: day == today,
                isExpanded: day == today);
        }

        WeeklySchedule.Clear();

        foreach (var day in orderedDays)
        {
            WeeklySchedule.Add(_dayGroupsByDay[day]);
        }

        OnPropertyChanged(nameof(ActivePlanName));
        OnPropertyChanged(nameof(ActivePlanTimelineSummary));
        OnPropertyChanged(nameof(HasActivePlan));
        _lastLoadedScheduleVersion = _scheduleService.ScheduleVersion;
        _lastLoadedDate = DateTime.Today;
    }

    private void ToggleDay(WeeklyScheduleDayGroup? dayGroup)
    {
        if (dayGroup == null || !dayGroup.CanToggle)
        {
            return;
        }

        dayGroup.IsExpanded = !dayGroup.IsExpanded;
    }

    private async Task ViewPlanPreviewAsync()
    {
        if (_scheduleService.ActivePlan == null)
        {
            return;
        }

        var detailsPage = new WorkoutPlanDetailsPage(
            App.Services.GetRequiredService<WorkoutPlanDetailsViewModel>(),
            _scheduleService.ActivePlan);

        await Shell.Current.Navigation.PushAsync(detailsPage);
    }

    private async Task ManageExerciseSubstitutionsAsync()
    {
        if (!HasActivePlan)
        {
            return;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        var exerciseOptions = _scheduleService.GetActivePlanExerciseOptions();
        if (exerciseOptions.Count == 0)
        {
            await page.DisplayAlert("No Exercises Available", "There are no plan workouts available to replace in the active plan.", "OK");
            return;
        }

        var exerciseLabels = exerciseOptions
            .ToDictionary(GetExerciseSelectionLabel, workout => workout, StringComparer.Ordinal);

        var selectedExerciseLabel = await page.DisplayActionSheet(
            "Replace Which Exercise?",
            "Cancel",
            null,
            exerciseLabels.Keys.OrderBy(label => label).ToArray());

        if (string.IsNullOrWhiteSpace(selectedExerciseLabel) ||
            selectedExerciseLabel == "Cancel" ||
            !exerciseLabels.TryGetValue(selectedExerciseLabel, out var selectedWorkout))
        {
            return;
        }

        var replacementOptions = BuildReplacementOptions(
            selectedWorkout,
            GetUnavailableAlternativeNames(GetOriginalExerciseName(selectedWorkout)));
        if (replacementOptions.Count == 0)
        {
            await page.DisplayAlert("No Alternatives Found", $"No alternatives are available yet for {selectedWorkout.Name}.", "OK");
            return;
        }

        var selectedReplacementLabel = await page.DisplayActionSheet(
            $"Replace {selectedWorkout.Name} With",
            "Cancel",
            null,
            replacementOptions.Keys.ToArray());

        if (string.IsNullOrWhiteSpace(selectedReplacementLabel) ||
            selectedReplacementLabel == "Cancel" ||
            !replacementOptions.TryGetValue(selectedReplacementLabel, out var replacementExerciseName))
        {
            return;
        }

        _scheduleService.ReplaceActivePlanExercise(GetOriginalExerciseName(selectedWorkout), replacementExerciseName);
        LoadSchedule();
    }

    private async Task PromptForCompletedPlanAsync()
    {
        if (!_scheduleService.HasCompletedActivePlan || _scheduleService.ActivePlan == null)
        {
            return;
        }

        var promptKey = $"{_scheduleService.ActivePlan.Name}:{_scheduleService.ActivePlanStartedOn:O}";
        if (string.Equals(_lastCompletedPlanPromptKey, promptKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastCompletedPlanPromptKey = promptKey;

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        var completionStats = await GetCompletionStatsAsync(_scheduleService.ActivePlan);
        var isEligibleForProgression = completionStats.CompletionRate >= ProgressionEligibilityThreshold;
        var suggestedPlan = isEligibleForProgression ? _scheduleService.GetSuggestedNextPlan() : null;
        var suggestionOption = suggestedPlan == null ? null : $"Try Suggested Plan: {suggestedPlan.Name}";
        var completionTitle = isEligibleForProgression
            ? $"Workout Plan Complete ({completionStats.CompletedSessions}/{completionStats.ExpectedSessions} sessions logged)"
            : $"Workout Plan Complete ({completionStats.CompletedSessions}/{completionStats.ExpectedSessions} sessions logged, 70% needed for a harder suggestion)";

        var action = await page.DisplayActionSheet(
            completionTitle,
            "Maybe Later",
            null,
            "Restart Current Plan",
            "Choose a New Plan",
            suggestionOption);

        if (action == "Restart Current Plan")
        {
            _scheduleService.RestartActivePlan();
            LoadSchedule();
            await page.DisplayAlert("Plan Restarted", $"'{_scheduleService.ActivePlan?.Name}' has been restarted.", "OK");
            return;
        }

        if (action == "Choose a New Plan")
        {
            var workoutPlanPage = App.Services.GetRequiredService<WorkoutPlanPage>();
            await Shell.Current.Navigation.PushAsync(workoutPlanPage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(suggestionOption) && action == suggestionOption && suggestedPlan != null)
        {
            _scheduleService.AddPlanToWeeklySchedule(suggestedPlan);
            LoadSchedule();
            await page.DisplayAlert(
                "Suggested Plan Started",
                $"You're now on '{suggestedPlan.Name}', a follow-up to the plan you completed.",
                "OK");
        }
    }

    private async Task<PlanCompletionStats> GetCompletionStatsAsync(WorkoutPlan plan)
    {
        if (!_scheduleService.ActivePlanStartedOn.HasValue || !_scheduleService.ActivePlanEndsOn.HasValue)
        {
            return new PlanCompletionStats(0, 0);
        }

        var startDate = _scheduleService.ActivePlanStartedOn.Value.Date;
        var endDate = _scheduleService.ActivePlanEndsOn.Value.Date;
        var workoutHistory = (await _workoutService.GetWorkouts())
            .Where(workout => workout.StartTime.Date >= startDate && workout.StartTime.Date <= endDate)
            .ToList();

        var expectedSessions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var weekNumber = ((date - startDate).Days / 7) + 1;
            foreach (var plannedWorkout in plan.GetWorkoutsForWeek(weekNumber).Where(workout => workout.Day == date.DayOfWeek))
            {
                var key = GetCompletionKey(plannedWorkout, date);
                expectedSessions[key] = expectedSessions.TryGetValue(key, out var count)
                    ? count + 1
                    : 1;
            }
        }

        if (expectedSessions.Count == 0)
        {
            return new PlanCompletionStats(0, 0);
        }

        var completedSessions = workoutHistory
            .GroupBy(workout => GetCompletionKey(workout, workout.StartTime.Date), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var matchedSessions = expectedSessions.Sum(expectedSession =>
            Math.Min(expectedSession.Value, completedSessions.GetValueOrDefault(expectedSession.Key, 0)));

        return new PlanCompletionStats(matchedSessions, expectedSessions.Values.Sum());
    }

    private static string GetCompletionKey(Workout workout, DateTime date)
    {
        if (workout.Type == WorkoutType.Cardio)
        {
            return $"{date:yyyy-MM-dd}|{workout.Type}";
        }

        return string.Join("|",
            date.ToString("yyyy-MM-dd"),
            workout.Type,
            GetOriginalExerciseName(workout),
            workout.MuscleGroup);
    }

    private static string GetExerciseSelectionLabel(Workout workout)
    {
        var originalExerciseName = GetOriginalExerciseName(workout);
        return string.Equals(originalExerciseName, workout.Name, StringComparison.OrdinalIgnoreCase)
            ? $"{workout.Name} ({workout.MuscleGroup})"
            : $"{originalExerciseName} -> {workout.Name} ({workout.MuscleGroup})";
    }

    private static string GetOriginalExerciseName(Workout workout)
    {
        return string.IsNullOrWhiteSpace(workout.PlannedExerciseName)
            ? workout.Name
            : workout.PlannedExerciseName.Trim();
    }

    private HashSet<string> GetUnavailableAlternativeNames(string selectedOriginalExerciseName)
    {
        return _scheduleService.GetActivePlanExerciseOptions()
            .Where(workout =>
            {
                var originalExerciseName = GetOriginalExerciseName(workout);
                return !string.Equals(originalExerciseName, workout.Name, StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(originalExerciseName, selectedOriginalExerciseName, StringComparison.OrdinalIgnoreCase);
            })
            .Select(workout => workout.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildReplacementOptions(Workout workout, ISet<string> unavailableAlternativeNames)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        var originalExerciseName = GetOriginalExerciseName(workout);

        if (!string.Equals(originalExerciseName, workout.Name, StringComparison.OrdinalIgnoreCase))
        {
            options[$"Reset to Original: {originalExerciseName}"] = originalExerciseName;
        }

        foreach (var alternative in ExerciseAlternativeCatalog.GetAlternatives(originalExerciseName, workout.MuscleGroup, workout.Type))
        {
            if (unavailableAlternativeNames.Contains(alternative))
            {
                continue;
            }

            options[alternative] = alternative;
        }

        return options;
    }

    private readonly record struct PlanCompletionStats(int CompletedSessions, int ExpectedSessions)
    {
        public double CompletionRate => ExpectedSessions == 0 ? 0 : (double)CompletedSessions / ExpectedSessions;
    }
}
