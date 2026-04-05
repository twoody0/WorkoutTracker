using System.Text.Json;
using Microsoft.Data.Sqlite;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

[TestClass]
public class WorkoutServiceTests
{
    [TestMethod]
    public void Workout_CalculatesEstimatedOneRepMaxAndVolume()
    {
        var workout = new Workout("Bench Press", 225, 5, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym");

        Assert.AreEqual(3375, workout.TrainingVolume, 0.001);
        Assert.AreEqual(255, workout.EstimatedOneRepMax, 0.001);
        Assert.IsTrue(workout.HasEstimatedOneRepMax);
    }

    [TestMethod]
    public void TimedStrengthWorkout_UsesTimeWithoutRepDisplay()
    {
        var workout = new Workout("Plank", 0, 0, 3, "Core", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Studio")
        {
            DurationSeconds = 30
        };

        Assert.IsTrue(workout.HasTimedTarget);
        Assert.IsFalse(workout.HasRepTarget);
        Assert.AreEqual(string.Empty, workout.RepDisplay);
        Assert.AreEqual("30 sec", workout.DurationValueDisplay);
        Assert.IsFalse(workout.HasEstimatedOneRepMax);
    }

    [TestMethod]
    public void LegacyTimedStrengthWorkout_ConvertsMinutesToSecondsForDisplay()
    {
        var workout = new Workout("Plank", 0, 0, 3, "Core", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Studio")
        {
            DurationMinutes = 1
        };

        Assert.IsTrue(workout.HasTimedTarget);
        Assert.AreEqual(60, workout.DurationSeconds);
        Assert.AreEqual(60, workout.TimedTargetSeconds);
        Assert.AreEqual("60 sec", workout.DurationValueDisplay);
    }

    [TestMethod]
    public async Task AddWorkout_StoresWorkoutInSQLite()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var databasePath = Path.Combine(tempDirectory, "workouts.db");

        try
        {
            var service = new WorkoutService(databasePath);
            var workout = new Workout("Bench Press", 185, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym");

            await service.AddWorkout(workout);
            var workouts = (await service.GetWorkouts()).ToList();

            Assert.AreEqual(1, workouts.Count);
            Assert.AreEqual("Bench Press", workouts[0].Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task AddWorkout_PreservesPlannedExerciseName()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var databasePath = Path.Combine(tempDirectory, "workouts.db");

        try
        {
            var service = new WorkoutService(databasePath);
            var workout = new Workout("Dumbbell Bench Press", 70, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            {
                PlannedExerciseName = "Barbell Bench Press"
            };

            await service.AddWorkout(workout);
            var workouts = (await service.GetWorkouts()).ToList();

            Assert.AreEqual(1, workouts.Count);
            Assert.AreEqual("Dumbbell Bench Press", workouts[0].Name);
            Assert.AreEqual("Barbell Bench Press", workouts[0].PlannedExerciseName);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task WorkoutService_MigratesLegacyJsonHistoryIntoSQLite()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var databasePath = Path.Combine(tempDirectory, "workouts.db");
        var legacyFilePath = Path.Combine(Path.GetDirectoryName(databasePath)!, "workouts.json");
        var legacyWorkouts = new List<Workout>
        {
            new("Morning Run", 0, 0, 0, "Cardio", DayOfWeek.Tuesday, DateTime.Today, WorkoutType.Cardio, "Track")
            {
                DurationMinutes = 30,
                DistanceMiles = 2.5
            }
        };

        try
        {
            await File.WriteAllTextAsync(legacyFilePath, JsonSerializer.Serialize(legacyWorkouts));

            var service = new WorkoutService(databasePath);
            var workouts = (await service.GetWorkouts()).ToList();

            Assert.AreEqual(1, workouts.Count);
            Assert.AreEqual("Morning Run", workouts[0].Name);
            Assert.AreEqual(30, workouts[0].DurationMinutes);
            Assert.IsFalse(File.Exists(legacyFilePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
