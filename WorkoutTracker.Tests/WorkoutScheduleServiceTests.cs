using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Data.Sqlite;

namespace WorkoutTracker.Tests;

[TestClass]
public class WorkoutScheduleServiceTests
{
    [TestMethod]
    public void Constructor_InitializesAllDaysOfWeek()
    {
        var service = CreateServiceWithPlans();

        var schedule = service.GetWeeklySchedule();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            Assert.IsTrue(schedule.ContainsKey(day), $"Expected schedule to contain {day}.");
        }
    }

    [TestMethod]
    public void AddWorkoutToDay_AddsWorkoutAndUpdatesDay()
    {
        var service = CreateServiceWithPlans();
        var workout = new Workout("Bench Press", 135, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym");

        service.AddWorkoutToDay(DayOfWeek.Wednesday, workout);

        Assert.AreEqual(DayOfWeek.Wednesday, workout.Day);
        CollectionAssert.Contains(service.GetWeeklySchedule()[DayOfWeek.Wednesday], workout);
    }

    [TestMethod]
    public void RemoveWorkoutFromDay_RemovesWorkout()
    {
        var service = CreateServiceWithPlans();
        var workout = new Workout("Row", 95, 10, 3, "Back", DayOfWeek.Tuesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym");
        service.AddWorkoutToDay(DayOfWeek.Tuesday, workout);

        service.RemoveWorkoutFromDay(DayOfWeek.Tuesday, workout);

        CollectionAssert.DoesNotContain(service.GetWeeklySchedule()[DayOfWeek.Tuesday], workout);
    }

    [TestMethod]
    public void AddPlanToWeeklySchedule_SetsActivePlanAndClearsPreviousSchedule()
    {
        var service = CreateServiceWithPlans();
        var existingWorkout = new Workout("Old Workout", 0, 12, 3, "Legs", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym");
        service.AddWorkoutToDay(DayOfWeek.Monday, existingWorkout);

        var plan = new WorkoutPlan("Test Plan", "Plan for tests", durationInWeeks: 6)
        {
            Workouts =
            [
                new Workout("Squat", 225, 5, 5, "Legs", DayOfWeek.Friday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Run", 0, 0, 1, "Cardio", DayOfWeek.Saturday, DateTime.Today, WorkoutType.Cardio, "Track")
            ]
        };

        service.AddPlanToWeeklySchedule(plan);

        Assert.AreSame(plan, service.ActivePlan);
        Assert.AreEqual(DateTime.Today, service.ActivePlanStartedOn);
        Assert.AreEqual(DateTime.Today.AddDays(41), service.ActivePlanEndsOn);
        CollectionAssert.DoesNotContain(service.GetWeeklySchedule()[DayOfWeek.Monday], existingWorkout);
        Assert.AreEqual(1, service.GetWeeklySchedule()[DayOfWeek.Friday].Count);
        Assert.AreEqual("Squat", service.GetWeeklySchedule()[DayOfWeek.Friday][0].Name);
        Assert.AreEqual(1, service.GetWeeklySchedule()[DayOfWeek.Saturday].Count);
        Assert.AreEqual("Run", service.GetWeeklySchedule()[DayOfWeek.Saturday][0].Name);
    }

    [TestMethod]
    public void GetActivePlanWorkoutsForDay_ReturnsPlanWorkoutsForRequestedDay()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Test Plan", "Plan for tests")
        {
            Workouts =
            [
                new Workout("Bench Press", 185, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Row", 135, 10, 3, "Back", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        service.AddPlanToWeeklySchedule(plan);

        var mondayWorkouts = service.GetActivePlanWorkoutsForDay(DayOfWeek.Monday);

        Assert.AreEqual(1, mondayWorkouts.Count);
        Assert.AreEqual("Bench Press", mondayWorkouts[0].Name);
        Assert.AreEqual(DayOfWeek.Monday, mondayWorkouts[0].Day);
        Assert.AreNotSame(plan.Workouts[0], mondayWorkouts[0]);
    }

    [TestMethod]
    public void GetActivePlanWorkoutsForDay_UsesCurrentTemplateWeek()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Rotating Plan", "Plan with weekly variation", durationInWeeks: 4)
        {
            Workouts =
            [
                new Workout("Week 1 Bench", 0, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 1
                },
                new Workout("Week 2 Incline Bench", 0, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 2
                }
            ]
        };

        service.AddPlanToWeeklySchedule(plan);
        SetPrivateAutoProperty(service, nameof(WorkoutScheduleService.ActivePlanStartedOn), DateTime.Today.AddDays(-7));

        var mondayWorkouts = service.GetActivePlanWorkoutsForDay(DayOfWeek.Monday);

        Assert.AreEqual(1, mondayWorkouts.Count);
        Assert.AreEqual("Week 2 Incline Bench", mondayWorkouts[0].Name);
    }

    [TestMethod]
    public void WorkoutPlan_GetWorkoutsForWeek_RepeatsTemplatesAcrossLongerPlan()
    {
        var plan = new WorkoutPlan("Rotating Plan", "Plan with weekly variation", durationInWeeks: 8)
        {
            Workouts =
            [
                new Workout("Week 1 Squat", 0, 5, 3, "Legs", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 1
                },
                new Workout("Week 2 Front Squat", 0, 5, 3, "Legs", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 2
                }
            ]
        };

        var weekFiveWorkouts = plan.GetWorkoutsForWeek(5);

        Assert.AreEqual(1, weekFiveWorkouts.Count);
        Assert.AreEqual("Week 1 Squat", weekFiveWorkouts[0].Name);
    }

    [TestMethod]
    public void RestartActivePlan_ResetsActivePlanDates()
    {
        var plan = new WorkoutPlan("Restartable", "Plan", category: "Beginner Strength", durationInWeeks: 4);
        var service = CreateServiceWithPlans(plan);

        service.AddPlanToWeeklySchedule(plan);
        var firstEndDate = service.ActivePlanEndsOn;

        service.RestartActivePlan();

        Assert.AreEqual(DateTime.Today, service.ActivePlanStartedOn);
        Assert.AreEqual(firstEndDate, service.ActivePlanEndsOn);
    }

    [TestMethod]
    public void GetSuggestedNextPlan_ReturnsMappedProgressionPlan()
    {
        var currentPlan = new WorkoutPlan("Beginner Full Body Foundation", "Plan", category: "Beginner Strength");
        var nextPlan = new WorkoutPlan("Upper/Lower Strength Builder", "Plan", category: "Strength Progression");
        var service = CreateServiceWithPlans(currentPlan, nextPlan);

        service.AddPlanToWeeklySchedule(currentPlan);

        var suggestion = service.GetSuggestedNextPlan();

        Assert.AreSame(nextPlan, suggestion);
    }

    [TestMethod]
    public void GetSuggestedNextPlan_ProgressesIntoHighVolumeMuscleBuildingOptions()
    {
        var currentPlan = new WorkoutPlan("Push/Pull/Legs Hypertrophy", "Plan", category: "Muscle Building");
        var nextPlan = new WorkoutPlan("Arnold Split Mass Builder", "Plan", category: "Muscle Building");
        var service = CreateServiceWithPlans(currentPlan, nextPlan);

        service.AddPlanToWeeklySchedule(currentPlan);

        var suggestion = service.GetSuggestedNextPlan();

        Assert.AreSame(nextPlan, suggestion);
    }

    [TestMethod]
    public void WorkoutScheduleService_RestoresSavedActivePlanState()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-active-plan.json");
        var plan = new WorkoutPlan("Saved Plan", "Plan for persistence", durationInWeeks: 4)
        {
            Workouts =
            [
                new Workout("Saved Squat", 0, 8, 3, "Legs", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        try
        {
            var planService = new FakeWorkoutPlanService([plan]);
            var firstService = new WorkoutScheduleService(planService, tempFilePath);
            firstService.AddPlanToWeeklySchedule(plan);

            var restoredService = new WorkoutScheduleService(planService, tempFilePath);

            Assert.IsNotNull(restoredService.ActivePlan);
            Assert.AreEqual("Saved Plan", restoredService.ActivePlan.Name);
            Assert.AreEqual(DateTime.Today, restoredService.ActivePlanStartedOn);
            Assert.AreEqual(DateTime.Today.AddDays(27), restoredService.ActivePlanEndsOn);
            Assert.AreEqual("Saved Squat", restoredService.GetWeeklySchedule()[DayOfWeek.Monday][0].Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [TestMethod]
    public void WorkoutPlanService_LoadsSavedCustomPlans()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-custom-plans.json");

        try
        {
            var firstService = new WorkoutPlanService(tempFilePath);
            firstService.AddWorkoutPlan(new WorkoutPlan("Custom Plan", "Saved custom plan", isCustom: true)
            {
                Workouts =
                [
                    new Workout("Custom Press", 0, 10, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                ]
            });

            var restoredService = new WorkoutPlanService(tempFilePath);
            var restoredCustomPlan = restoredService.GetWorkoutPlans()
                .FirstOrDefault(plan => plan.IsCustom && plan.Name == "Custom Plan");

            Assert.IsNotNull(restoredCustomPlan);
            Assert.AreEqual(1, restoredCustomPlan.Workouts.Count);
            Assert.AreEqual("Custom Press", restoredCustomPlan.Workouts[0].Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [TestMethod]
    public void WorkoutPlanService_StrengthPlansIncludeLowRepStrengthWeeks()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-plans.db");

        try
        {
            var service = new WorkoutPlanService(tempFilePath);
            var upperLowerPlan = service.GetWorkoutPlans().First(plan => plan.Name == "Upper/Lower Strength Builder");
            var hypertrophyPlan = service.GetWorkoutPlans().First(plan => plan.Name == "Push/Pull/Legs Hypertrophy");

            Assert.IsTrue(upperLowerPlan.Workouts.Any(workout => workout.Type == WorkoutType.WeightLifting && workout.Reps <= 3));
            Assert.IsTrue(hypertrophyPlan.Workouts.Any(workout => workout.Type == WorkoutType.WeightLifting && workout.Reps <= 4));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private static WorkoutScheduleService CreateServiceWithPlans(params WorkoutPlan[] plans)
    {
        var planService = new FakeWorkoutPlanService(plans);
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-active-plan.json");
        return new WorkoutScheduleService(planService, tempFilePath);
    }

    private sealed class FakeWorkoutPlanService : IWorkoutPlanService
    {
        private readonly List<WorkoutPlan> _plans;

        public FakeWorkoutPlanService(IEnumerable<WorkoutPlan> plans)
        {
            _plans = plans.ToList();
        }

        public IEnumerable<WorkoutPlan> GetWorkoutPlans() => _plans;

        public void AddWorkoutPlan(WorkoutPlan plan)
        {
            _plans.Add(plan);
        }

        public void SavePlans()
        {
        }
    }

    private static void SetPrivateAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(field, $"Could not find backing field for {propertyName}.");
        field.SetValue(target, value);
    }
}
