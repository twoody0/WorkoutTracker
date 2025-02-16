using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

    public async Task<IEnumerable<WeightliftingExercise>> SearchExercisesByName(string muscleGroup, string query)
    {
        var exercises = await GetExercises();
        var filtered = exercises.Where(e => e.MuscleGroup.Equals(muscleGroup, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(e => e.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        }
        return filtered;
    }
}
