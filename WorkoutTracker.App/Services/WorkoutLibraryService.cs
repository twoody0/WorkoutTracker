using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Loads and searches a predefined list of weightlifting exercises from a JSON file.
/// </summary>
public class WorkoutLibraryService : IWorkoutLibraryService
{
    #region Fields

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private IReadOnlyList<WeightliftingExercise>? _exercises;
    private Dictionary<string, IReadOnlyList<WeightliftingExercise>>? _muscleGroupIndex;
    private readonly Dictionary<string, IReadOnlyList<WeightliftingExercise>> _searchCache = new(StringComparer.Ordinal);
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
        return await EnsureExercisesLoadedAsync();
    }

    /// <summary>
    /// Filters exercises by muscle group and optionally by name query.
    /// </summary>
    /// <param name="muscleGroup">The target muscle group to filter.</param>
    /// <param name="query">The optional name prefix to filter by.</param>
    /// <returns>A filtered list of matching exercises.</returns>
    public async Task<IEnumerable<WeightliftingExercise>> SearchExercisesByName(string muscleGroup, string query)
    {
        await EnsureExercisesLoadedAsync();

        var normalizedMuscleGroup = muscleGroup?.Trim() ?? string.Empty;
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var cacheKey = $"{normalizedMuscleGroup}|{normalizedQuery}";
        if (_searchCache.TryGetValue(cacheKey, out var cachedResults))
        {
            return cachedResults;
        }

        var filtered = GetExercisesForMuscleGroup(normalizedMuscleGroup);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            filtered = filtered
                .Where(exercise => exercise.Name.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        _searchCache[cacheKey] = filtered;
        return filtered;
    }

    #endregion

    private async Task<IReadOnlyList<WeightliftingExercise>> EnsureExercisesLoadedAsync()
    {
        if (_exercises != null)
        {
            return _exercises;
        }

        await _syncLock.WaitAsync();
        try
        {
            if (_exercises != null)
            {
                return _exercises;
            }

            using Stream stream = await FileSystem.OpenAppPackageFileAsync("exercises.json");

            _exercises = await JsonSerializer.DeserializeAsync<List<WeightliftingExercise>>(stream)
                ?? [];
            _muscleGroupIndex = BuildMuscleGroupIndex(_exercises);
            _searchCache.Clear();
            return _exercises;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private IReadOnlyList<WeightliftingExercise> GetExercisesForMuscleGroup(string muscleGroup)
    {
        if (_muscleGroupIndex == null || string.IsNullOrWhiteSpace(muscleGroup))
        {
            return [];
        }

        var matchingMuscleGroups = GetMatchingMuscleGroups(muscleGroup);
        var results = new List<WeightliftingExercise>();

        foreach (var group in matchingMuscleGroups)
        {
            if (_muscleGroupIndex.TryGetValue(group, out var exercises))
            {
                results.AddRange(exercises);
            }
        }

        return results
            .Distinct()
            .ToArray();
    }

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

    private static Dictionary<string, IReadOnlyList<WeightliftingExercise>> BuildMuscleGroupIndex(IEnumerable<WeightliftingExercise> exercises)
    {
        return exercises
            .GroupBy(exercise => exercise.MuscleGroup?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WeightliftingExercise>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
