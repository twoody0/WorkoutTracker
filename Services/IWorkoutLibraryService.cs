using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public interface IWorkoutLibraryService
{
    Task<IEnumerable<WeightliftingExercise>> GetExercises();
    Task<IEnumerable<WeightliftingExercise>> SearchExercises(string muscleGroup);
}
