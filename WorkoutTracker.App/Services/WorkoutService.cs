using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Manages workout history in local device storage so the app can run without a backend.
/// </summary>
public class WorkoutService : IWorkoutService
{
    #region Fields

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _workoutsFilePath = Path.Combine(FileSystem.AppDataDirectory, "workouts.json");
    private List<Workout>? _cachedWorkouts;

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new workout to the user's history.
    /// </summary>
    /// <param name="workout">The workout to add.</param>
    public async Task AddWorkout(Workout workout)
    {
        await _syncLock.WaitAsync();
        try
        {
            var workouts = await LoadWorkoutsAsync();
            workouts.Add(workout);
            await SaveWorkoutsAsync(workouts);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Retrieves all workouts saved on the device.
    /// </summary>
    /// <returns>A collection of workouts.</returns>
    public async Task<IEnumerable<Workout>> GetWorkouts()
    {
        await _syncLock.WaitAsync();
        try
        {
            var workouts = await LoadWorkoutsAsync();
            return workouts.ToList();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    #endregion

    #region Private Methods

    private async Task<List<Workout>> LoadWorkoutsAsync()
    {
        if (_cachedWorkouts != null)
        {
            return _cachedWorkouts;
        }

        if (!File.Exists(_workoutsFilePath))
        {
            _cachedWorkouts = new List<Workout>();
            return _cachedWorkouts;
        }

        await using var stream = File.OpenRead(_workoutsFilePath);
        _cachedWorkouts = await JsonSerializer.DeserializeAsync<List<Workout>>(stream, _jsonOptions) ?? new List<Workout>();
        return _cachedWorkouts;
    }

    private async Task SaveWorkoutsAsync(List<Workout> workouts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_workoutsFilePath)!);

        await using var stream = File.Create(_workoutsFilePath);
        await JsonSerializer.SerializeAsync(stream, workouts, _jsonOptions);
        _cachedWorkouts = workouts;
    }

    #endregion
}
