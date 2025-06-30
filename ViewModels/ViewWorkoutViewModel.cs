using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
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
        IEnumerable<Workout> workouts = await _workoutService.GetWorkouts();
        Workouts.Clear();
        foreach (var workout in workouts)
        {
            Workouts.Add(workout);
        }
    }
    public ICommand CopyWorkoutCommand => new Command<Workout>(async (workout) =>
    {
        if (workout != null)
        {
            // Pass values to the WorkoutPage via query or static helper
            WorkoutTemplateCache.Template = new Workout
            {
                Name = workout.Name,
                Weight = workout.Weight,
                Reps = workout.Reps,
                Sets = workout.Sets,
                Type = workout.Type
            };

            await Shell.Current.GoToAsync("///WorkoutPage");
        }
    });
}
