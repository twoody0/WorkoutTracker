using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanDetailsViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly Dictionary<string, string> _previewExerciseSubstitutions = new(StringComparer.OrdinalIgnoreCase);
    private int _selectedPreviewWeek = 1;
    private bool _showWeekTemplateHelp;
    private bool _showRpeHelp;

    public WorkoutPlan? SelectedPlan { get; private set; }
    public ObservableCollection<WorkoutPlanDayGroup> WorkoutGroups { get; } = new();
    public ObservableCollection<int> PreviewWeeks { get; } = new();

    public int SelectedPreviewWeek
    {
        get => _selectedPreviewWeek;
        set
        {
            if (SetProperty(ref _selectedPreviewWeek, value))
            {
                RefreshWorkoutGroups();
                OnPropertyChanged(nameof(PreviewWeekSummary));
            }
        }
    }

    public bool ShowWeekSelector => SelectedPlan != null && (SelectedPlan.DurationInWeeks > 1 || SelectedPlan.HasWeeklyVariation);
    public bool ShowStartPlanButton => SelectedPlan != null && !IsSelectedPlanActive;
    public bool ShowManageExerciseSubstitutionsButton => SelectedPlan != null;
    public bool IsSelectedPlanActive => SelectedPlan != null &&
        _scheduleService.ActivePlan != null &&
        string.Equals(_scheduleService.ActivePlan.Name, SelectedPlan.Name, StringComparison.OrdinalIgnoreCase);
    public bool ShowWeekTemplateHelp
    {
        get => _showWeekTemplateHelp;
        set => SetProperty(ref _showWeekTemplateHelp, value);
    }
    public bool ShowRpeHelp
    {
        get => _showRpeHelp;
        set => SetProperty(ref _showRpeHelp, value);
    }
    public bool ShowRpeInfo => GetPreviewWorkouts().Any(workout => workout.HasTargetRpe);
    public string RpeHelpText => "RPE means rate of perceived exertion on a 1-10 scale. Around 6 feels fairly easy, 8 is hard with a couple reps left, and 9-10 is near-max effort.";

    public string WeekTemplateHelpText => SelectedPlan == null
        ? string.Empty
        : SelectedPlan.HasWeeklyVariation
            ? "Each week can change exercise selection, volume, or cardio goals. Some plans use a full week-by-week progression, while others rotate a smaller set of templates across the full plan."
            : "This plan keeps the same weekly layout across the full plan length.";
    public string PreviewWeekSummary
    {
        get
        {
            if (SelectedPlan == null)
            {
                return string.Empty;
            }

            if (!SelectedPlan.HasWeeklyVariation)
            {
                return "This plan uses the same weekly layout throughout the full duration.";
            }

            if (SelectedPlan.WeeklyVariationCount >= SelectedPlan.DurationInWeeks)
            {
                return $"Week {SelectedPreviewWeek} has its own progression.";
            }

            return $"Week {SelectedPreviewWeek} uses template {SelectedPlan.NormalizeWeekNumber(SelectedPreviewWeek)} of {SelectedPlan.WeeklyVariationCount}.";
        }
    }

    public ICommand ToggleExpandCommand { get; }
    public ICommand StartPlanCommand { get; }
    public ICommand ChangeWorkoutDayCommand { get; }
    public ICommand EditDayCommand { get; }
    public ICommand ToggleWeekTemplateHelpCommand { get; }
    public ICommand ToggleRpeHelpCommand { get; }
    public ICommand ManageExerciseSubstitutionsCommand { get; }

    public WorkoutPlanDetailsViewModel(IWorkoutScheduleService scheduleService, IWorkoutPlanService workoutPlanService)
    {
        _scheduleService = scheduleService;
        _workoutPlanService = workoutPlanService;
        ToggleExpandCommand = new Command<WorkoutPlanDayGroup>(ToggleExpand);
        StartPlanCommand = new Command(StartPlan);
        ChangeWorkoutDayCommand = new Command<WorkoutDisplay>(ChangeWorkoutDay);
        EditDayCommand = new Command<WorkoutPlanDayGroup>(EditDay);
        ToggleWeekTemplateHelpCommand = new Command(() => ShowWeekTemplateHelp = !ShowWeekTemplateHelp);
        ToggleRpeHelpCommand = new Command(() => ShowRpeHelp = !ShowRpeHelp);
        ManageExerciseSubstitutionsCommand = new Command(async () => await ManageExerciseSubstitutionsAsync());
    }

    private async void EditDay(WorkoutPlanDayGroup? workoutGroup)
    {
        if (workoutGroup == null || SelectedPlan == null)
        {
            return;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        await page.Navigation.PushAsync(new EditDayPage(workoutGroup.Day, SelectedPlan, _scheduleService));
    }

    private async void ChangeWorkoutDay(WorkoutDisplay workoutDisplay)
    {
        if (workoutDisplay == null)
        {
            return;
        }

        var days = Enum.GetNames(typeof(DayOfWeek));
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null || SelectedPlan == null)
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
            workoutDisplay.Workout.Day = newDay;
            if (SelectedPlan.IsCustom)
            {
                _workoutPlanService.SavePlans();
            }

            LoadPlan(SelectedPlan);

            await page.DisplayAlert(
                "Workout Moved",
                $"{workoutDisplay.Workout.Name} is now scheduled for {newDay}.",
                "OK");
        }
    }

    public void LoadPlan(WorkoutPlan plan)
    {
        var currentPlanName = SelectedPlan?.Name;
        SelectedPlan = plan;

        if (!string.Equals(currentPlanName, plan.Name, StringComparison.OrdinalIgnoreCase))
        {
            _previewExerciseSubstitutions.Clear();
        }

        PreviewWeeks.Clear();
        for (var weekNumber = 1; weekNumber <= plan.DurationInWeeks; weekNumber++)
        {
            PreviewWeeks.Add(weekNumber);
        }

        _selectedPreviewWeek = 1;
        RefreshWorkoutGroups();
        OnPropertyChanged(nameof(SelectedPlan));
        OnPropertyChanged(nameof(WorkoutGroups));
        OnPropertyChanged(nameof(PreviewWeeks));
        OnPropertyChanged(nameof(SelectedPreviewWeek));
        OnPropertyChanged(nameof(ShowWeekSelector));
        OnPropertyChanged(nameof(ShowStartPlanButton));
        OnPropertyChanged(nameof(ShowManageExerciseSubstitutionsButton));
        OnPropertyChanged(nameof(IsSelectedPlanActive));
        OnPropertyChanged(nameof(PreviewWeekSummary));
        OnPropertyChanged(nameof(WeekTemplateHelpText));
        OnPropertyChanged(nameof(ShowRpeInfo));
        OnPropertyChanged(nameof(RpeHelpText));
    }

    private void RefreshWorkoutGroups()
    {
        WorkoutGroups.Clear();

        if (SelectedPlan == null)
        {
            return;
        }

        var workoutsByDay = GetPreviewWorkouts()
            .GroupBy(workout => workout.Day)
            .ToDictionary(group => group.Key, group => group.AsEnumerable());

        var orderedDays = Enum.GetValues<DayOfWeek>()
            .OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)
            .ToList();

        foreach (var day in orderedDays)
        {
            var workoutsForDay = workoutsByDay.TryGetValue(day, out var workouts)
                ? workouts.OrderBy(workout => workout.Name)
                : Enumerable.Empty<Workout>();

            WorkoutGroups.Add(new WorkoutPlanDayGroup(
                day,
                workoutsForDay.Select(workout => new WorkoutDisplay(workout)),
                isExpanded: false,
                canEditDay: SelectedPlan.IsCustom));
        }

        OnPropertyChanged(nameof(WorkoutGroups));
        OnPropertyChanged(nameof(ShowRpeInfo));
    }

    private async Task ManageExerciseSubstitutionsAsync()
    {
        if (SelectedPlan == null)
        {
            return;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        var exerciseOptions = GetExerciseOptions();
        if (exerciseOptions.Count == 0)
        {
            await page.DisplayAlert("No Exercises Available", "There are no plan workouts available to replace in this plan.", "OK");
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

        var originalExerciseName = GetOriginalExerciseName(selectedWorkout);
        if (IsSelectedPlanActive)
        {
            _scheduleService.ReplaceActivePlanExercise(originalExerciseName, replacementExerciseName);
        }
        else
        {
            ReplacePreviewExercise(originalExerciseName, replacementExerciseName);
        }

        RefreshWorkoutGroups();
    }

    private void ToggleExpand(WorkoutPlanDayGroup? workoutGroup)
    {
        if (workoutGroup == null || !workoutGroup.HasWorkouts)
        {
            return;
        }

        workoutGroup.IsExpanded = !workoutGroup.IsExpanded;
    }

    private async void StartPlan()
    {
        if (SelectedPlan == null)
            return;

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
            return;

        if (_scheduleService.ActivePlan != null)
        {
            bool confirm = await page.DisplayAlert(
                "Replace Active Plan?",
                $"You already have '{_scheduleService.ActivePlan.Name}' as your active plan.\n\nDo you want to replace it with '{SelectedPlan.Name}'?",
                "Yes, Replace",
                "Cancel");

            if (!confirm)
                return;
        }

        var alignFirstWorkoutDayToToday = false;
        if (TryGetFirstWorkoutDay(SelectedPlan, out var firstWorkoutDay) &&
            firstWorkoutDay != DateTime.Today.DayOfWeek)
        {
            alignFirstWorkoutDayToToday = await page.DisplayAlert(
                "Start Plan Timing",
                $"'{SelectedPlan.Name}' normally opens on {firstWorkoutDay}, and today is {DateTime.Today:dddd, MMMM d}.\n\nDo you want day 1 of the plan to start today, or keep the plan on its normal weekly schedule?",
                "Start Day 1 Today",
                "Keep Schedule");
        }

        _scheduleService.AddPlanToWeeklySchedule(SelectedPlan, alignFirstWorkoutDayToToday);

        foreach (var substitution in _previewExerciseSubstitutions)
        {
            _scheduleService.ReplaceActivePlanExercise(substitution.Key, substitution.Value);
        }

        _previewExerciseSubstitutions.Clear();

        var parentViewModel = App.Services.GetRequiredService<WorkoutPlanViewModel>();
        parentViewModel.RefreshActivePlan();

        await page.DisplayAlert(
            "Plan Started",
            $"'{SelectedPlan.Name}' is now your active workout plan!",
            "OK");

        await Shell.Current.GoToAsync("//add-workout");
    }

    private static bool TryGetFirstWorkoutDay(WorkoutPlan plan, out DayOfWeek firstWorkoutDay)
    {
        var firstDay = plan.GetWorkoutsForWeek(1)
            .Select(workout => workout.Day)
            .Distinct()
            .OrderBy(GetMondayFirstDayIndex)
            .Cast<DayOfWeek?>()
            .FirstOrDefault();

        if (!firstDay.HasValue)
        {
            firstWorkoutDay = default;
            return false;
        }

        firstWorkoutDay = firstDay.Value;
        return true;
    }

    private static int GetMondayFirstDayIndex(DayOfWeek day)
        => day == DayOfWeek.Sunday ? 6 : ((int)day - 1);

    private IReadOnlyList<Workout> GetPreviewWorkouts()
    {
        if (SelectedPlan == null)
        {
            return [];
        }

        if (IsSelectedPlanActive)
        {
            return _scheduleService.GetPlanWorkoutsForPreview(SelectedPlan, SelectedPreviewWeek);
        }

        return SelectedPlan.GetWorkoutsForWeek(SelectedPreviewWeek)
            .Select(CloneWorkout)
            .Select(ApplyPreviewExerciseSubstitution)
            .ToList();
    }

    private IReadOnlyList<Workout> GetExerciseOptions()
    {
        if (SelectedPlan == null)
        {
            return [];
        }

        if (IsSelectedPlanActive)
        {
            return _scheduleService.GetActivePlanExerciseOptions();
        }

        var uniqueExercises = new Dictionary<string, Workout>(StringComparer.OrdinalIgnoreCase);
        foreach (var workout in SelectedPlan.Workouts)
        {
            var originalExerciseName = GetOriginalExerciseName(workout);
            if (uniqueExercises.ContainsKey(originalExerciseName))
            {
                continue;
            }

            uniqueExercises[originalExerciseName] = ApplyPreviewExerciseSubstitution(CloneWorkout(workout));
        }

        return uniqueExercises.Values
            .OrderBy(workout => workout.Name)
            .ToList();
    }

    private HashSet<string> GetUnavailableAlternativeNames(string selectedOriginalExerciseName)
    {
        if (IsSelectedPlanActive)
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

        return _previewExerciseSubstitutions
            .Where(pair => !string.Equals(pair.Key, selectedOriginalExerciseName, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void ReplacePreviewExercise(string originalExerciseName, string replacementExerciseName)
    {
        if (string.IsNullOrWhiteSpace(originalExerciseName) || string.IsNullOrWhiteSpace(replacementExerciseName))
        {
            return;
        }

        var normalizedOriginal = originalExerciseName.Trim();
        var normalizedReplacement = replacementExerciseName.Trim();

        if (string.Equals(normalizedOriginal, normalizedReplacement, StringComparison.OrdinalIgnoreCase))
        {
            _previewExerciseSubstitutions.Remove(normalizedOriginal);
            return;
        }

        if (_previewExerciseSubstitutions.Any(pair =>
                !string.Equals(pair.Key, normalizedOriginal, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pair.Value, normalizedReplacement, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _previewExerciseSubstitutions[normalizedOriginal] = normalizedReplacement;
    }

    private Workout ApplyPreviewExerciseSubstitution(Workout workout)
    {
        var originalExerciseName = GetOriginalExerciseName(workout);
        if (_previewExerciseSubstitutions.TryGetValue(originalExerciseName, out var replacementExerciseName) &&
            !string.IsNullOrWhiteSpace(replacementExerciseName) &&
            !string.Equals(workout.Name, replacementExerciseName, StringComparison.OrdinalIgnoreCase))
        {
            workout.PlannedExerciseName = originalExerciseName;
            workout.Name = replacementExerciseName.Trim();
        }

        return workout;
    }

    private static Workout CloneWorkout(Workout workout)
    {
        return new Workout(
            name: workout.Name,
            weight: workout.Weight,
            reps: workout.Reps,
            sets: workout.Sets,
            muscleGroup: workout.MuscleGroup,
            day: workout.Day,
            startTime: workout.StartTime,
            type: workout.Type,
            gymLocation: workout.GymLocation)
        {
            PlannedExerciseName = workout.PlannedExerciseName,
            MinReps = workout.MinReps,
            MaxReps = workout.MaxReps,
            TargetRpe = workout.TargetRpe,
            TargetRestRange = workout.TargetRestRange,
            EndTime = workout.EndTime,
            Steps = workout.Steps,
            DurationMinutes = workout.DurationMinutes,
            DistanceMiles = workout.DistanceMiles,
            DurationSeconds = workout.DurationSeconds,
            PlanWeekNumber = workout.PlanWeekNumber,
            IsWarmup = workout.IsWarmup
        };
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
}
