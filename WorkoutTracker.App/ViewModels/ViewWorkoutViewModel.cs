using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for displaying and copying existing workouts.
/// </summary>
public class ViewWorkoutViewModel : BaseViewModel
{
    // ─────────────────────────────────────────────────────────────
    // Private Fields
    // ─────────────────────────────────────────────────────────────

    private readonly IWorkoutService _workoutService;

    // ─────────────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Collection of workouts loaded from the service.
    /// </summary>
    public ObservableCollection<Workout> Workouts { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Command that copies the selected workout and navigates to the WorkoutPage to edit/add it.
    /// </summary>
    public ICommand CopyWorkoutCommand => new Command<Workout>(async (workout) =>
    {
        if (workout != null)
        {
            // Use parameterized constructor to store the copied workout
            WorkoutTemplateCache.Template = new Workout(
                name: workout.Name,
                weight: workout.Weight,
                reps: workout.Reps,
                sets: workout.Sets,
                muscleGroup: workout.MuscleGroup,
                startTime: workout.StartTime,
                type: workout.Type,
                gymLocation: workout.GymLocation
            );

            await Shell.Current.GoToAsync("///WorkoutPage");
        }
    });

    // ─────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────

    public ViewWorkoutViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        Workouts = new ObservableCollection<Workout>();
        _ = LoadWorkouts();
    }

    // ─────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads all workouts from the service into the observable collection.
    /// </summary>
    private async Task LoadWorkouts()
    {
        IEnumerable<Workout> workouts = await _workoutService.GetWorkouts();
        Workouts.Clear();
        foreach (var workout in workouts)
        {
            Workouts.Add(workout);
        }
    }
}
