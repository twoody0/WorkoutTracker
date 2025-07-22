using System.Collections.ObjectModel;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanViewModel : BaseViewModel
{
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly IWorkoutScheduleService _scheduleService;

    public ObservableCollection<WorkoutPlan> WorkoutPlans { get; set; } = new();
    private List<WorkoutPlan> AllPlans { get; set; } = new();

    public WorkoutPlan CurrentPlan => _scheduleService.ActivePlan;
    public bool HasActivePlan => _scheduleService.ActivePlan != null;

    public string NewPlanName { get; set; }
    public string NewPlanDescription { get; set; }
    public Command AddWorkoutPlanCommand { get; }
    public Command SelectWorkoutPlanCommand { get; }

    public WorkoutPlanViewModel(IWorkoutPlanService workoutPlanService, IWorkoutScheduleService scheduleService)
    {
        _workoutPlanService = workoutPlanService;
        _scheduleService = scheduleService;

        AddWorkoutPlanCommand = new Command(AddWorkoutPlan);
        SelectWorkoutPlanCommand = new Command<WorkoutPlan>(SelectWorkoutPlan);
        LoadWorkoutPlans();
    }

    private void LoadWorkoutPlans()
    {
        // Get all plans
        AllPlans = _workoutPlanService.GetWorkoutPlans().ToList();

        RefreshWorkoutPlans();
    }

    private void RefreshWorkoutPlans()
    {
        WorkoutPlans.Clear();

        // Add all plans EXCEPT the current active one
        foreach (var plan in AllPlans)
        {
            if (plan != _scheduleService.ActivePlan)
            {
                WorkoutPlans.Add(plan);
            }
        }

        OnPropertyChanged(nameof(CurrentPlan));
        OnPropertyChanged(nameof(HasActivePlan));
    }
    private async void SelectWorkoutPlan(WorkoutPlan plan)
    {
        if (plan == null)
            return;

        // If selected plan is already active, go straight to WeeklySchedulePage
        if (_scheduleService.ActivePlan != null && _scheduleService.ActivePlan == plan)
        {
            var schedulePage = App.Services.GetRequiredService<WeeklySchedulePage>();
            await Shell.Current.Navigation.PushAsync(schedulePage);
        }
        else
        {
            // Otherwise, show details page first
            var detailsPage = new WorkoutPlanDetailsPage(
                App.Services.GetRequiredService<WorkoutPlanDetailsViewModel>(), plan);
            await Shell.Current.Navigation.PushAsync(detailsPage);
        }
    }

    private void AddWorkoutPlan()
    {
        if (string.IsNullOrWhiteSpace(NewPlanName)) return;

        var newPlan = new WorkoutPlan
        {
            Name = NewPlanName,
            Description = NewPlanDescription,
            IsCustom = true
        };

        _workoutPlanService.AddWorkoutPlan(newPlan);
        AllPlans.Add(newPlan);

        RefreshWorkoutPlans();

        NewPlanName = string.Empty;
        NewPlanDescription = string.Empty;
        OnPropertyChanged(nameof(NewPlanName));
        OnPropertyChanged(nameof(NewPlanDescription));
    }

    public void RefreshActivePlan()
    {
        RefreshWorkoutPlans();
    }
}
