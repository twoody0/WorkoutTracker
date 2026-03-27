using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanDetailsViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;

    public WorkoutPlan? SelectedPlan { get; private set; }
    public ObservableCollection<WorkoutPlanDayGroup> WorkoutGroups { get; } = new();

    public ICommand ToggleExpandCommand { get; }
    public ICommand StartPlanCommand { get; }
    public ICommand ChangeWorkoutDayCommand { get; }
    public ICommand EditDayCommand { get; }

    public WorkoutPlanDetailsViewModel(IWorkoutScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        ToggleExpandCommand = new Command<WorkoutPlanDayGroup>(ToggleExpand);
        StartPlanCommand = new Command(StartPlan);
        ChangeWorkoutDayCommand = new Command<WorkoutDisplay>(ChangeWorkoutDay);
        EditDayCommand = new Command<WorkoutPlanDayGroup>(EditDay);
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
        WorkoutGroups.Clear();

        var workoutsByDay = plan.Workouts
            .GroupBy(workout => workout.Day)
            .ToDictionary(group => group.Key, group => group.AsEnumerable());

        var orderedDays = Enum.GetValues<DayOfWeek>()
            .OrderBy(day => (int)day)
            .ToList();
        var firstWorkoutDay = orderedDays.FirstOrDefault(day => workoutsByDay.ContainsKey(day));

        for (var i = 0; i < orderedDays.Count; i++)
        {
            var day = orderedDays[i];
            var workoutsForDay = workoutsByDay.TryGetValue(day, out var workouts)
                ? workouts.OrderBy(workout => workout.Name)
                : Enumerable.Empty<Workout>();

            WorkoutGroups.Add(new WorkoutPlanDayGroup(
                day,
                workoutsForDay.Select(workout => new WorkoutDisplay(workout)),
                isExpanded: false));
        }

        OnPropertyChanged(nameof(SelectedPlan));
        OnPropertyChanged(nameof(WorkoutGroups));
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

        _scheduleService.AddPlanToWeeklySchedule(SelectedPlan);

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

}
