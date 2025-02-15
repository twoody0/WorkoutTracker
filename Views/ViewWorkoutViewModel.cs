using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class ViewWorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;

    public ViewWorkoutViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        Workouts = new ObservableCollection<Workout>();
        LoadWorkouts();
    }

    public ObservableCollection<Workout> Workouts { get; set; }

    private async Task LoadWorkouts()
    {
        var workouts = await _workoutService.GetWorkouts();
        Workouts.Clear();
        foreach (var workout in workouts)
        {
            Workouts.Add(workout);
        }
    }
}
