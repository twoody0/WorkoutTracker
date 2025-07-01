using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for tracking and saving cardio workout sessions based on step count.
/// </summary>
public class CardioWorkoutViewModel : BaseViewModel
{
    // ─────────────────────────────────────────────────────────────
    // Private Fields
    // ─────────────────────────────────────────────────────────────

    private readonly IWorkoutService _workoutService;
    private readonly IStepCounterService _stepCounterService;
    private int _sessionSteps;
    private bool _isTracking;

    // ─────────────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The number of steps taken during the current cardio session.
    /// </summary>
    public int SessionSteps
    {
        get => _sessionSteps;
        set { _sessionSteps = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Indicates whether the step tracker is currently active.
    /// </summary>
    public bool IsTracking
    {
        get => _isTracking;
        set { _isTracking = value; OnPropertyChanged(); }
    }

    // ─────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a new cardio workout session and begins tracking steps.
    /// </summary>
    public ICommand StartSessionCommand => new Command(() =>
    {
        SessionSteps = 0;
        _stepCounterService.StartTracking();
        IsTracking = true;
    });

    /// <summary>
    /// Stops the current cardio session, saves the workout, and resets steps.
    /// </summary>
    public ICommand StopSessionCommand => new Command(async () =>
    {
        _stepCounterService.StopTracking();
        IsTracking = false;

        // Create cardio workout using the constructor
        var workout = new Workout(
            name: "Cardio Session",
            weight: 0,
            reps: 0,
            sets: 0,
            muscleGroup: string.Empty,
            startTime: DateTime.Now,
            type: WorkoutType.Cardio,
            gymLocation: "Outdoor"
        )
        {
            Steps = SessionSteps,
            EndTime = DateTime.Now
        };

        await _workoutService.AddWorkout(workout);

        // Optionally reset session steps
        SessionSteps = 0;
    });

    // ─────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────

    public CardioWorkoutViewModel(IWorkoutService workoutService, IStepCounterService stepCounterService)
    {
        _workoutService = workoutService;
        _stepCounterService = stepCounterService;

        _stepCounterService.StepsUpdated += OnStepsUpdated;

        SessionSteps = 0;
        IsTracking = false;
    }

    // ─────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles step count updates from the step counter service.
    /// </summary>
    private void OnStepsUpdated(object sender, int steps)
    {
        SessionSteps = steps;
    }
}
