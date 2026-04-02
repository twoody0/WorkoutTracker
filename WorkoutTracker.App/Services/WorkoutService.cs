using System.Text.Json;
using Microsoft.Data.Sqlite;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Manages workout history in local device storage so the app can run without a backend.
/// </summary>
public class WorkoutService : IWorkoutService
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly WorkoutTrackerDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _legacyWorkoutsFilePath;
    private List<Workout>? _cachedWorkouts;
    private long _changeVersion;

    public WorkoutService(string? databasePath = null)
    {
        _database = new WorkoutTrackerDatabase(databasePath);
        _legacyWorkoutsFilePath = Path.Combine(
            Path.GetDirectoryName(_database.DatabasePath) ?? string.Empty,
            "workouts.json");
    }

    public long ChangeVersion => Interlocked.Read(ref _changeVersion);

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

    private async Task<List<Workout>> LoadWorkoutsAsync()
    {
        if (_cachedWorkouts != null)
        {
            return _cachedWorkouts;
        }

        await MigrateLegacyJsonIfNeededAsync();

        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Name, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup
            FROM WorkoutHistory
            ORDER BY StartTime, Id;
            """;

        using var reader = command.ExecuteReader();
        var workouts = new List<Workout>();
        while (reader.Read())
        {
            workouts.Add(WorkoutPlanService.ReadWorkout(reader, 0));
        }

        _cachedWorkouts = workouts;
        return _cachedWorkouts;
    }

    private Task SaveWorkoutsAsync(List<Workout> workouts)
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        using (var clearCommand = connection.CreateCommand())
        {
            clearCommand.Transaction = transaction;
            clearCommand.CommandText = "DELETE FROM WorkoutHistory;";
            clearCommand.ExecuteNonQuery();
        }

        foreach (var workout in workouts)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO WorkoutHistory
                (Name, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup)
                VALUES ($name, $muscleGroup, $gymLocation, $weight, $reps, $sets, $minReps, $maxReps, $targetRpe, $targetRestRange, $startTime, $endTime, $steps, $durationMinutes, $durationSeconds, $distanceMiles, $type, $day, $planWeekNumber, $isWarmup);
                """;
            WorkoutPlanService.AddWorkoutParameters(command, workout);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
        _cachedWorkouts = workouts.ToList();
        Interlocked.Increment(ref _changeVersion);
        return Task.CompletedTask;
    }

    private async Task MigrateLegacyJsonIfNeededAsync()
    {
        if (_cachedWorkouts != null || !File.Exists(_legacyWorkoutsFilePath) || HasWorkoutRows())
        {
            return;
        }

        try
        {
            List<Workout> workouts;
            await using (var stream = File.OpenRead(_legacyWorkoutsFilePath))
            {
                workouts = await JsonSerializer.DeserializeAsync<List<Workout>>(stream, _jsonOptions) ?? [];
            }

            await SaveWorkoutsAsync(workouts);
            File.Delete(_legacyWorkoutsFilePath);
        }
        catch
        {
            // Keep going even if legacy migration fails.
        }
    }

    private bool HasWorkoutRows()
    {
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM WorkoutHistory;";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
