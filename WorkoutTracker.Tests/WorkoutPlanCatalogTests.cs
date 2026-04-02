using System.Text.Json;
using Microsoft.Data.Sqlite;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

[TestClass]
public class WorkoutPlanCatalogTests
{
    [TestMethod]
    public void WorkoutPlanService_BuiltInStrengthExercisesExistInLibrary()
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-plan-catalog.db");

        try
        {
            var service = new WorkoutPlanService(tempDatabasePath);
            var exerciseNames = LoadExerciseCatalog()
                .Select(exercise => exercise.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingExercises = service.GetWorkoutPlans()
                .Where(plan => !plan.IsCustom)
                .SelectMany(plan => plan.Workouts)
                .Where(workout => workout.Type == WorkoutType.WeightLifting)
                .Select(workout => workout.Name)
                .Where(name => !exerciseNames.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            Assert.AreEqual(0, missingExercises.Count, $"Missing strength exercises in library: {string.Join(", ", missingExercises)}");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDatabasePath))
            {
                File.Delete(tempDatabasePath);
            }
        }
    }

    [TestMethod]
    public void ExerciseCatalog_DoesNotContainDeprecatedNearDuplicateNames()
    {
        var exercises = LoadExerciseCatalog();
        var duplicateNames = exercises
            .GroupBy(exercise => exercise.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name)
            .ToList();

        Assert.AreEqual(0, duplicateNames.Count, $"Duplicate exercise names found: {string.Join(", ", duplicateNames)}");

        var deprecatedNames = new[]
        {
            "Chest Press Machine",
            "Dips",
            "Elevated Push-Up",
            "Face Pulls",
            "Rear-Foot Elevated Split Squat"
        };

        foreach (var deprecatedName in deprecatedNames)
        {
            Assert.IsFalse(
                exercises.Any(exercise => string.Equals(exercise.Name, deprecatedName, StringComparison.OrdinalIgnoreCase)),
                $"Exercise catalog still contains deprecated alias '{deprecatedName}'.");
        }
    }

    [TestMethod]
    public void WorkoutPlanService_IncludesHighVolumeUpperBodyOptions()
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-high-volume-plans.db");

        try
        {
            var service = new WorkoutPlanService(tempDatabasePath);
            var arnoldPlan = service.GetWorkoutPlans().FirstOrDefault(plan => plan.Name == "Arnold Split Mass Builder");
            var classicSplit = service.GetWorkoutPlans().FirstOrDefault(plan => plan.Name == "Classic Body Part Split");

            Assert.IsNotNull(arnoldPlan);
            Assert.IsNotNull(classicSplit);
            Assert.AreEqual(8, arnoldPlan.DurationInWeeks);
            Assert.AreEqual(8, classicSplit.DurationInWeeks);

            Assert.IsTrue(GetWeightLiftingCountForDay(arnoldPlan, DayOfWeek.Monday) >= 7);
            Assert.IsTrue(GetWeightLiftingCountForDay(arnoldPlan, DayOfWeek.Tuesday) >= 8);
            Assert.IsTrue(GetWeightLiftingCountForDay(classicSplit, DayOfWeek.Monday) >= 7);
            Assert.IsTrue(GetWeightLiftingCountForDay(classicSplit, DayOfWeek.Thursday) >= 8);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDatabasePath))
            {
                File.Delete(tempDatabasePath);
            }
        }
    }

    private static int GetWeightLiftingCountForDay(WorkoutPlan? plan, DayOfWeek day)
    {
        Assert.IsNotNull(plan);
        return plan.Workouts.Count(workout => workout.Type == WorkoutType.WeightLifting && workout.Day == day);
    }

    private static IReadOnlyList<ExerciseCatalogEntry> LoadExerciseCatalog()
    {
        var catalogPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "WorkoutTracker.App",
            "Resources",
            "Raw",
            "exercises.json"));

        var json = File.ReadAllText(catalogPath);
        return JsonSerializer.Deserialize<List<ExerciseCatalogEntry>>(json) ?? [];
    }

    private sealed class ExerciseCatalogEntry
    {
        public required string Name { get; set; }
        public required string MuscleGroup { get; set; }
    }
}
