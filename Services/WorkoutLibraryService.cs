using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutLibraryService : IWorkoutLibraryService
{
    private List<WeightliftingExercise> _exercises;

    public async Task<IEnumerable<WeightliftingExercise>> GetExercises()
    {
        if (_exercises == null)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("exercises.json");
            _exercises = await JsonSerializer.DeserializeAsync<List<WeightliftingExercise>>(stream);
        }
        return _exercises;
    }

    public async Task<IEnumerable<WeightliftingExercise>> SearchExercises(string muscleGroup)
    {
        var exercises = await GetExercises();
        return exercises.Where(e => e.MuscleGroup.Equals(muscleGroup, StringComparison.OrdinalIgnoreCase));
    }
}
