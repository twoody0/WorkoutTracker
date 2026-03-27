using Microsoft.Data.Sqlite;

namespace WorkoutTracker.Services;

public sealed class WorkoutTrackerDatabase
{
    public string DatabasePath { get; }

    public WorkoutTrackerDatabase(string? databasePath = null)
    {
        DatabasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkoutTracker",
            "workouttracker.db");

        var directoryPath = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS CustomWorkoutPlans (
                Name TEXT PRIMARY KEY,
                Description TEXT NOT NULL,
                Category TEXT NOT NULL,
                DurationInWeeks INTEGER NOT NULL,
                IsCustom INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CustomWorkoutPlanWorkouts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlanName TEXT NOT NULL,
                Name TEXT NOT NULL,
                MuscleGroup TEXT NOT NULL,
                GymLocation TEXT NOT NULL,
                Weight REAL NOT NULL,
                Reps INTEGER NOT NULL,
                Sets INTEGER NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL,
                FOREIGN KEY (PlanName) REFERENCES CustomWorkoutPlans(Name) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ActivePlanState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                ActivePlanName TEXT NOT NULL,
                ActivePlanStartedOn TEXT NOT NULL,
                ActivePlanEndsOn TEXT NOT NULL,
                ActivePlanScheduleWeekNumber INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ActivePlanScheduledWorkouts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                MuscleGroup TEXT NOT NULL,
                GymLocation TEXT NOT NULL,
                Weight REAL NOT NULL,
                Reps INTEGER NOT NULL,
                Sets INTEGER NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL
            );

            CREATE TABLE IF NOT EXISTS WorkoutHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                MuscleGroup TEXT NOT NULL,
                GymLocation TEXT NOT NULL,
                Weight REAL NOT NULL,
                Reps INTEGER NOT NULL,
                Sets INTEGER NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }
}
