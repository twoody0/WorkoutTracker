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
                MinReps INTEGER NULL,
                MaxReps INTEGER NULL,
                TargetRpe REAL NULL,
                TargetRestRange TEXT NOT NULL DEFAULT '',
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL,
                IsWarmup INTEGER NOT NULL DEFAULT 0,
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
                MinReps INTEGER NULL,
                MaxReps INTEGER NULL,
                TargetRpe REAL NULL,
                TargetRestRange TEXT NOT NULL DEFAULT '',
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL,
                IsWarmup INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS WorkoutHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                MuscleGroup TEXT NOT NULL,
                GymLocation TEXT NOT NULL,
                Weight REAL NOT NULL,
                Reps INTEGER NOT NULL,
                Sets INTEGER NOT NULL,
                MinReps INTEGER NULL,
                MaxReps INTEGER NULL,
                TargetRpe REAL NULL,
                TargetRestRange TEXT NOT NULL DEFAULT '',
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Steps INTEGER NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                DistanceMiles REAL NOT NULL,
                Type INTEGER NOT NULL,
                Day INTEGER NOT NULL,
                PlanWeekNumber INTEGER NULL,
                IsWarmup INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "CustomWorkoutPlanWorkouts", "MinReps", "INTEGER NULL");
        EnsureColumnExists(connection, "CustomWorkoutPlanWorkouts", "MaxReps", "INTEGER NULL");
        EnsureColumnExists(connection, "CustomWorkoutPlanWorkouts", "TargetRpe", "REAL NULL");
        EnsureColumnExists(connection, "CustomWorkoutPlanWorkouts", "TargetRestRange", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "CustomWorkoutPlanWorkouts", "IsWarmup", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(connection, "ActivePlanScheduledWorkouts", "MinReps", "INTEGER NULL");
        EnsureColumnExists(connection, "ActivePlanScheduledWorkouts", "MaxReps", "INTEGER NULL");
        EnsureColumnExists(connection, "ActivePlanScheduledWorkouts", "TargetRpe", "REAL NULL");
        EnsureColumnExists(connection, "ActivePlanScheduledWorkouts", "TargetRestRange", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "ActivePlanScheduledWorkouts", "IsWarmup", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(connection, "WorkoutHistory", "MinReps", "INTEGER NULL");
        EnsureColumnExists(connection, "WorkoutHistory", "MaxReps", "INTEGER NULL");
        EnsureColumnExists(connection, "WorkoutHistory", "TargetRpe", "REAL NULL");
        EnsureColumnExists(connection, "WorkoutHistory", "TargetRestRange", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "WorkoutHistory", "IsWarmup", "INTEGER NOT NULL DEFAULT 0");
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

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = checkCommand.ExecuteReader();

        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alterCommand.ExecuteNonQuery();
    }
}
