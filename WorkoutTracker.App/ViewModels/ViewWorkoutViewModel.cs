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
    #region Private Fields

    private readonly IWorkoutService _workoutService;
    private ObservableCollection<Workout> _workouts;

    #endregion

    #region Constructor

    public ViewWorkoutViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        Workouts = new ObservableCollection<Workout>();
        _ = LoadWorkouts();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Collection of workouts loaded from the service.
    /// </summary>
    public ObservableCollection<Workout> Workouts
    {
        get => _workouts;
        set => SetProperty(ref _workouts, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command that copies the selected workout and navigates to the WorkoutPage to edit/add it.
    /// </summary>
    public ICommand CopyWorkoutCommand => new Command<Workout>(async (workout) =>
    {
        if (workout != null)
        {
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

    #endregion

    #region Private Methods

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

    #endregion
}
