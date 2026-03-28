using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Loads and searches a predefined list of weightlifting exercises from a JSON file.
/// </summary>
public class WorkoutLibraryService : IWorkoutLibraryService
{
    #region Fields

    private List<WeightliftingExercise>? _exercises;
    private static readonly Dictionary<string, string[]> MuscleGroupAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arms"] = ["Arms", "Biceps", "Triceps"],
        ["Biceps"] = ["Biceps", "Arms"],
        ["Triceps"] = ["Triceps", "Arms"],
        ["Core"] = ["Core", "Abs"],
        ["Abs"] = ["Abs", "Core"],
        ["Back"] = ["Back", "Lats", "Lower Back"],
        ["Legs"] = ["Legs", "Quads", "Hamstrings", "Glutes", "Calves"]
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets all weightlifting exercises from the local JSON file.
    /// </summary>
    /// <returns>A collection of all exercises.</returns>
    public async Task<IEnumerable<WeightliftingExercise>> GetExercises()
    {
        if (_exercises == null)
        {
            using Stream stream = await FileSystem.OpenAppPackageFileAsync("exercises.json");

            _exercises = await JsonSerializer.DeserializeAsync<List<WeightliftingExercise>>(stream)
                         ?? new List<WeightliftingExercise>();
        }

        return _exercises;
    }

    /// <summary>
    /// Filters exercises by muscle group and optionally by name query.
    /// </summary>
    /// <param name="muscleGroup">The target muscle group to filter.</param>
    /// <param name="query">The optional name prefix to filter by.</param>
    /// <returns>A filtered list of matching exercises.</returns>
    public async Task<IEnumerable<WeightliftingExercise>> SearchExercisesByName(string muscleGroup, string query)
    {
        var exercises = await GetExercises();
        var matchingMuscleGroups = GetMatchingMuscleGroups(muscleGroup);

        var filtered = exercises
            .Where(e => matchingMuscleGroups.Contains(e.MuscleGroup, StringComparer.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered
                .Where(e => e.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }

    #endregion

    private static IReadOnlyList<string> GetMatchingMuscleGroups(string muscleGroup)
    {
        if (string.IsNullOrWhiteSpace(muscleGroup))
        {
            return [];
        }

        if (MuscleGroupAliases.TryGetValue(muscleGroup.Trim(), out var aliases))
        {
            return aliases;
        }

        return [muscleGroup.Trim()];
    }
}
