using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanDetailsViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;

    public WorkoutPlan SelectedPlan { get; private set; }
    public ObservableCollection<WorkoutDisplay> Workouts { get; } = new();

    public ICommand ToggleExpandCommand { get; }
    public ICommand StartPlanCommand { get; }

    public WorkoutPlanDetailsViewModel(IWorkoutScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        ToggleExpandCommand = new Command<WorkoutDisplay>(ToggleExpand);
        StartPlanCommand = new Command(StartPlan);
    }

    public void LoadPlan(WorkoutPlan plan)
    {
        SelectedPlan = plan;
        Workouts.Clear();
        foreach (var workout in plan.Workouts)
        {
            Workouts.Add(new WorkoutDisplay(workout));
        }
    }

    private void ToggleExpand(WorkoutDisplay workoutDisplay)
    {
        if (workoutDisplay != null)
        {
            workoutDisplay.IsExpanded = !workoutDisplay.IsExpanded;
            OnPropertyChanged(nameof(Workouts));
        }
    }

    private async void StartPlan()
    {
        if (_scheduleService.ActivePlan != null)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
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
        await Application.Current.MainPage.DisplayAlert(
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
