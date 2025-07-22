using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

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

    private void StartPlan()
    {
        _scheduleService.AddPlanToWeeklySchedule(SelectedPlan);
    }
}
