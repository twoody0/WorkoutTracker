using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanDetailsViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;
    private readonly IWorkoutPlanService _workoutPlanService;
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
    public bool ShowRpeInfo => SelectedPlan?.GetWorkoutsForWeek(SelectedPreviewWeek).Any(workout => workout.HasTargetRpe) == true;
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
            return;

        var days = Enum.GetNames(typeof(DayOfWeek));
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null || SelectedPlan == null)
            return;

        string selectedDay = await page.DisplayActionSheet(
            "Move Workout To:",
            "Cancel",
            null,
            days);

        if (!string.IsNullOrWhiteSpace(selectedDay) && Enum.TryParse(selectedDay, out DayOfWeek newDay))
        {
            // Update the workout's DayOfWeek
            workoutDisplay.Workout.Day = newDay;
            if (SelectedPlan.IsCustom)
            {
                _workoutPlanService.SavePlans();
            }

            // Refresh the grouped workouts by day
            LoadPlan(SelectedPlan);

            // Optionally notify user
            await page.DisplayAlert(
                "Workout Moved",
                $"{workoutDisplay.Workout.Name} is now scheduled for {newDay}.",
                "OK");
        }
    }

    public void LoadPlan(WorkoutPlan plan)
    {
        SelectedPlan = plan;
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

        var workoutsByDay = SelectedPlan.GetWorkoutsForWeek(SelectedPreviewWeek)
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
                return; // User cancelled
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

        // Refresh WorkoutPlansPage
        var parentViewModel = App.Services.GetRequiredService<WorkoutPlanViewModel>();
        parentViewModel.RefreshActivePlan();

        // Show success
        await page.DisplayAlert(
            "Plan Started",
            $"'{SelectedPlan.Name}' is now your active workout plan!",
            "OK");

        var schedulePage = App.Services.GetRequiredService<WeeklySchedulePage>();

        // Replace WorkoutPlanDetailsPage with WeeklySchedulePage
        Shell.Current.Navigation.InsertPageBefore(schedulePage, Shell.Current.Navigation.NavigationStack[^1]);

        // Go forward to WeeklySchedulePage and remove WorkoutPlanDetailsPage
        await Shell.Current.Navigation.PopAsync();
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

}
