using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public interface IWorkoutLibraryService
{
    Task<IEnumerable<WeightliftingExercise>> GetExercises();
    Task<IEnumerable<WeightliftingExercise>> SearchExercisesByName(string muscleGroup, string query);
}