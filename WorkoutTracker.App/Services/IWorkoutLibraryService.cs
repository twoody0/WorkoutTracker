using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Provides access to a library of predefined weightlifting exercises.
/// </summary>
public interface IWorkoutLibraryService
{
    #region Methods

    /// <summary>
    /// Gets all available weightlifting exercises.
    /// </summary>
    /// <returns>A collection of all exercises.</returns>
    Task<IEnumerable<WeightliftingExercise>> GetExercises();

    /// <summary>
    /// Searches exercises by muscle group and partial name match.
    /// </summary>
    /// <param name="muscleGroup">The muscle group to filter by (e.g., "Chest").</param>
    /// <param name="query">The partial name or keyword to match against exercise names.</param>
    /// <returns>A filtered collection of matching exercises.</returns>
    Task<IEnumerable<WeightliftingExercise>> SearchExercisesByName(string muscleGroup, string query);

    #endregion
}
