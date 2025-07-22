using System.Collections.ObjectModel;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanViewModel : BaseViewModel
{
    private readonly IWorkoutPlanService _workoutPlanService;

    public ObservableCollection<WorkoutPlan> WorkoutPlans { get; set; } = new();
    public WorkoutPlan SelectedPlan { get; set; }

    public string NewPlanName { get; set; }
    public string NewPlanDescription { get; set; }

    public Command AddWorkoutPlanCommand { get; }
    public Command SelectWorkoutPlanCommand { get; }

    public WorkoutPlanViewModel(IWorkoutPlanService workoutPlanService)
    {
        _workoutPlanService = workoutPlanService;
        AddWorkoutPlanCommand = new Command(AddWorkoutPlan);
        SelectWorkoutPlanCommand = new Command<WorkoutPlan>(SelectWorkoutPlan);
        LoadWorkoutPlans();
    }

    private void LoadWorkoutPlans()
    {
        var plans = _workoutPlanService.GetWorkoutPlans();
        WorkoutPlans.Clear();
        foreach (var plan in plans)
        {
            WorkoutPlans.Add(plan);
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
        WorkoutPlans.Add(newPlan);

        NewPlanName = string.Empty;
        NewPlanDescription = string.Empty;
        OnPropertyChanged(nameof(NewPlanName));
        OnPropertyChanged(nameof(NewPlanDescription));
    }

    private async void SelectWorkoutPlan(WorkoutPlan plan)
    {
        if (plan != null)
        {
            // Resolve the details page from DI and pass in the selected plan
            var detailsPage = new WorkoutPlanDetailsPage(
                App.Services.GetRequiredService<WorkoutPlanDetailsViewModel>(), plan);

            // Navigate using PushAsync
            await Shell.Current.Navigation.PushAsync(detailsPage);
        }
    }
}
