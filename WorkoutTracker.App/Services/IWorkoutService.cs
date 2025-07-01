using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Provides functionality to manage user-created workout sessions.
/// </summary>
public interface IWorkoutService
{
    #region Methods

    /// <summary>
    /// Adds a new workout to the user's workout history.
    /// </summary>
    /// <param name="workout">The workout to add.</param>
    Task AddWorkout(Workout workout);

    /// <summary>
    /// Retrieves all workouts associated with the current user.
    /// </summary>
    /// <returns>A collection of saved workouts.</returns>
    Task<IEnumerable<Workout>> GetWorkouts();

    #endregion
}
