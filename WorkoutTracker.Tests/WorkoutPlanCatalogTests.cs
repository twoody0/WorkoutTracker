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
    public void ExerciseCatalog_DoesNotContainUnexpectedDuplicateOrDeprecatedNames()
    {
        var exercises = LoadExerciseCatalog();
        var allowedDuplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Dip",
            "Pull-Up",
            "Push-Up"
        };

        var unexpectedDuplicateNames = exercises
            .GroupBy(exercise => exercise.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Where(name => !allowedDuplicateNames.Contains(name))
            .OrderBy(name => name)
            .ToList();

        Assert.AreEqual(0, unexpectedDuplicateNames.Count, $"Unexpected duplicate exercise names found: {string.Join(", ", unexpectedDuplicateNames)}");

        var deprecatedNames = new[]
        {
            "Assisted Pull-Up",
            "Chest Press Machine",
            "Chest Dip",
            "Dips",
            "Elevated Push-Up",
            "Face Pulls",
            "Rear-Foot Elevated Split Squat",
            "Weighted Pull-Up"
        };

        foreach (var deprecatedName in deprecatedNames)
        {
            Assert.IsFalse(
                exercises.Any(exercise => string.Equals(exercise.Name, deprecatedName, StringComparison.OrdinalIgnoreCase)),
                $"Exercise catalog still contains deprecated alias '{deprecatedName}'.");
        }

        var dipEntries = exercises
            .Where(exercise => string.Equals(exercise.Name, "Dip", StringComparison.OrdinalIgnoreCase))
            .Select(exercise => exercise.MuscleGroup)
            .OrderBy(muscleGroup => muscleGroup)
            .ToList();

        CollectionAssert.AreEquivalent(new[] { "Chest", "Triceps" }, dipEntries);

        var pullUpEntries = exercises
            .Where(exercise => string.Equals(exercise.Name, "Pull-Up", StringComparison.OrdinalIgnoreCase))
            .Select(exercise => exercise.MuscleGroup)
            .OrderBy(muscleGroup => muscleGroup)
            .ToList();

        CollectionAssert.AreEquivalent(new[] { "Back", "Biceps" }, pullUpEntries);

        var pushUpEntries = exercises
            .Where(exercise => string.Equals(exercise.Name, "Push-Up", StringComparison.OrdinalIgnoreCase))
            .Select(exercise => exercise.MuscleGroup)
            .OrderBy(muscleGroup => muscleGroup)
            .ToList();

        CollectionAssert.AreEquivalent(new[] { "Chest", "Triceps" }, pushUpEntries);
    }

    [TestMethod]
    public void WorkoutPlanService_UsesGenericDipAndPullUpNames()
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-generic-calisthenics-names.db");

        try
        {
            var service = new WorkoutPlanService(tempDatabasePath);
            var deprecatedPlanNames = service.GetWorkoutPlans()
                .Where(plan => !plan.IsCustom)
                .SelectMany(plan => plan.Workouts)
                .Where(workout =>
                    workout.Type == WorkoutType.WeightLifting &&
                    (string.Equals(workout.Name, "Assisted Pull-Up", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(workout.Name, "Weighted Pull-Up", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(workout.Name, "Chest Dip", StringComparison.OrdinalIgnoreCase)))
                .Select(workout => workout.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            Assert.AreEqual(0, deprecatedPlanNames.Count, $"Built-in plans still use deprecated exercise names: {string.Join(", ", deprecatedPlanNames)}");
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
    public void ExerciseCatalog_UsesCoreInsteadOfAbsMuscleGroup()
    {
        var exercises = LoadExerciseCatalog();
        var absEntries = exercises
            .Where(exercise => string.Equals(exercise.MuscleGroup, "Abs", StringComparison.OrdinalIgnoreCase))
            .Select(exercise => exercise.Name)
            .ToList();

        Assert.AreEqual(0, absEntries.Count, $"Exercise catalog still uses 'Abs': {string.Join(", ", absEntries)}");
        Assert.IsTrue(exercises.Any(exercise => string.Equals(exercise.MuscleGroup, "Core", StringComparison.OrdinalIgnoreCase)));
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

    [TestMethod]
    public void WorkoutPlanService_UsesTimedTargetsForHoldAndCarryExercises()
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-timed-strength-plans.db");

        try
        {
            var service = new WorkoutPlanService(tempDatabasePath);
            var timedSupportWorkouts = service.GetWorkoutPlans()
                .Where(plan => !plan.IsCustom)
                .SelectMany(plan => plan.Workouts)
                .Where(workout => workout.Type == WorkoutType.WeightLifting && Workout.PrefersTimedTarget(workout.Name))
                .ToList();

            Assert.IsTrue(timedSupportWorkouts.Count > 0, "Expected at least one hold/carry workout in the built-in plans.");
            Assert.IsTrue(
                timedSupportWorkouts.All(workout => workout.DurationSeconds > 0 && workout.Reps == 0 && !workout.HasRepTarget),
                "Expected all built-in hold/carry workouts to use timed targets instead of reps.");
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
    public void WorkoutPlanService_IncludesDedicatedAtHomeStrengthPlan()
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-at-home-plan.db");

        try
        {
            var service = new WorkoutPlanService(tempDatabasePath);
            var atHomePlan = service.GetWorkoutPlans().FirstOrDefault(plan => plan.Name == "At-Home Strength Builder");

            Assert.IsNotNull(atHomePlan);
            Assert.AreEqual(8, atHomePlan.DurationInWeeks);
            Assert.IsTrue(atHomePlan.Workouts.Any(workout => workout.Name == "Pike Push-Up"));
            Assert.IsTrue(atHomePlan.Workouts.Any(workout => workout.Name == "Glute Bridge"));
            Assert.IsTrue(atHomePlan.Workouts.Any(workout => workout.Name == "Bodyweight Good Morning"));
            Assert.IsTrue(atHomePlan.Workouts.Any(workout => workout.Name == "Band Pull-Apart"));
            Assert.IsTrue(atHomePlan.Workouts.All(workout => string.Equals(workout.GymLocation, "Home", StringComparison.OrdinalIgnoreCase)));
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
