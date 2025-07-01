using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Manages the user's workout history in memory. Replace with persistent storage for production.
/// </summary>
public class WorkoutService : IWorkoutService
{
    #region Fields

    private readonly List<Workout> _workouts = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new workout to the user's history.
    /// </summary>
    /// <param name="workout">The workout to add.</param>
    public async Task AddWorkout(Workout workout)
    {
        _workouts.Add(workout);
        await Task.CompletedTask; // Placeholder for async compatibility
    }

    /// <summary>
    /// Retrieves all workouts saved in memory.
    /// </summary>
    /// <returns>A collection of workouts.</returns>
    public Task<IEnumerable<Workout>> GetWorkouts() =>
        Task.FromResult(_workouts.AsEnumerable());

    #endregion
}
