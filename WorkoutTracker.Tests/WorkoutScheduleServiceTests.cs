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
    public void AddPlanToWeeklySchedule_AlignsFirstWorkoutDayToToday_WhenRequested()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Shifted Plan", "Plan for tests")
        {
            Workouts =
            [
                new Workout("Day 1 Bench", 185, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Day 2 Row", 135, 10, 3, "Back", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        service.AddPlanToWeeklySchedule(plan, alignFirstWorkoutDayToToday: true);

        var dayOffset = GetDayOffsetFromMonday(DateTime.Today.DayOfWeek);
        var shiftedSecondDay = ShiftDayOfWeek(DayOfWeek.Wednesday, dayOffset);

        Assert.AreEqual("Day 1 Bench", service.GetWeeklySchedule()[DateTime.Today.DayOfWeek].Single().Name);
        Assert.AreEqual("Day 2 Row", service.GetWeeklySchedule()[shiftedSecondDay].Single().Name);
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
    public void GetActivePlanWorkoutsForDay_UsesShiftedDaysWhenPlanStartsToday()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Shifted Plan", "Plan for tests")
        {
            Workouts =
            [
                new Workout("Day 1 Bench", 185, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        service.AddPlanToWeeklySchedule(plan, alignFirstWorkoutDayToToday: true);

        var todaysWorkouts = service.GetActivePlanWorkoutsForDay(DateTime.Today.DayOfWeek);

        Assert.AreEqual(1, todaysWorkouts.Count);
        Assert.AreEqual("Day 1 Bench", todaysWorkouts[0].Name);
        Assert.AreEqual(DateTime.Today.DayOfWeek, todaysWorkouts[0].Day);
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
    public void GetActivePlanWorkoutsForDate_ReturnsWorkoutsForRequestedDateAndWeek()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Date Aware Plan", "Plan with weekly variation", durationInWeeks: 4)
        {
            Workouts =
            [
                new Workout("Week 1 Monday Bench", 0, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 1
                },
                new Workout("Week 2 Tuesday Row", 0, 10, 3, "Back", DayOfWeek.Tuesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 2
                }
            ]
        };

        service.AddPlanToWeeklySchedule(plan);
        SetPrivateAutoProperty(service, nameof(WorkoutScheduleService.ActivePlanStartedOn), new DateTime(2026, 4, 6));

        var requestedDate = new DateTime(2026, 4, 14);
        var workouts = service.GetActivePlanWorkoutsForDate(requestedDate);

        Assert.AreEqual(1, workouts.Count);
        Assert.AreEqual("Week 2 Tuesday Row", workouts[0].Name);
        Assert.AreEqual(DayOfWeek.Tuesday, workouts[0].Day);
    }

    [TestMethod]
    public void MoveMissedWorkoutToDate_UsesNextRestDayForTodaysWorkoutWhenAvailable()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Catchup Plan", "Plan")
        {
            Workouts =
            [
                new Workout("Missed Bench", 0, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Today Row", 0, 10, 3, "Back", DayOfWeek.Tuesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Wednesday Press", 0, 8, 3, "Shoulders", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Thursday Squat", 0, 5, 3, "Legs", DayOfWeek.Thursday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        service.AddPlanToWeeklySchedule(plan);
        SetPrivateAutoProperty(service, nameof(WorkoutScheduleService.ActivePlanStartedOn), new DateTime(2026, 4, 13));

        var moved = service.MoveMissedWorkoutToDate(new DateTime(2026, 4, 13), new DateTime(2026, 4, 14));

        Assert.IsTrue(moved);
        Assert.AreEqual("Missed Bench", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 14)).Single().Name);
        Assert.AreEqual("Today Row", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 15)).Single().Name);
        Assert.AreEqual("Wednesday Press", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 16)).Single().Name);
        Assert.AreEqual("Thursday Squat", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 17)).Single().Name);
    }

    [TestMethod]
    public void MoveMissedWorkoutToDate_ShiftsScheduleForwardWhenNoRestDayExists()
    {
        var service = CreateServiceWithPlans();
        var plan = new WorkoutPlan("Dense Plan", "Plan")
        {
            Workouts =
            [
                new Workout("Monday Lift", 0, 8, 3, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Tuesday Lift", 0, 10, 3, "Back", DayOfWeek.Tuesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Wednesday Lift", 0, 10, 3, "Shoulders", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Thursday Lift", 0, 10, 3, "Legs", DayOfWeek.Thursday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Friday Lift", 0, 10, 3, "Core", DayOfWeek.Friday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Saturday Lift", 0, 10, 3, "Arms", DayOfWeek.Saturday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Sunday Lift", 0, 10, 3, "Back", DayOfWeek.Sunday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };

        service.AddPlanToWeeklySchedule(plan);
        SetPrivateAutoProperty(service, nameof(WorkoutScheduleService.ActivePlanStartedOn), new DateTime(2026, 4, 13));

        var moved = service.MoveMissedWorkoutToDate(new DateTime(2026, 4, 14), new DateTime(2026, 4, 15));

        Assert.IsTrue(moved);
        Assert.AreEqual("Tuesday Lift", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 15)).Single().Name);
        Assert.AreEqual("Wednesday Lift", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 16)).Single().Name);
        Assert.AreEqual("Sunday Lift", service.GetActivePlanWorkoutsForDate(new DateTime(2026, 4, 20)).Single().Name);
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
    public void GetSuggestedNextPlan_ProgressesAtHomeStrengthIntoGymStrengthBuilder()
    {
        var currentPlan = new WorkoutPlan("At-Home Strength Builder", "Plan", category: "At-Home Strength");
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
    public void ReplaceActivePlanExercise_UpdatesWholePlanAndPersistsSubstitutions()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-active-plan.db");
        var plan = new WorkoutPlan("Substitution Plan", "Plan for persistence", durationInWeeks: 2)
        {
            Workouts =
            [
                new Workout("Barbell Bench Press", 185, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 1
                },
                new Workout("Barbell Bench Press", 190, 6, 4, "Chest", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
                {
                    PlanWeekNumber = 2
                }
            ]
        };

        try
        {
            var planService = new FakeWorkoutPlanService([plan]);
            var firstService = new WorkoutScheduleService(planService, tempFilePath);
            firstService.AddPlanToWeeklySchedule(plan);

            var exerciseOptions = firstService.GetActivePlanExerciseOptions();
            Assert.AreEqual(1, exerciseOptions.Count);
            Assert.AreEqual("Barbell Bench Press", exerciseOptions[0].Name);

            firstService.ReplaceActivePlanExercise("Barbell Bench Press", "Dumbbell Bench Press");

            var mondayWorkout = firstService.GetWeeklySchedule()[DayOfWeek.Monday].Single();
            Assert.AreEqual("Dumbbell Bench Press", mondayWorkout.Name);
            Assert.AreEqual("Barbell Bench Press", mondayWorkout.PlannedExerciseName);

            var weekTwoPreviewWorkout = firstService.GetPlanWorkoutsForPreview(plan, 2).Single();
            Assert.AreEqual("Dumbbell Bench Press", weekTwoPreviewWorkout.Name);
            Assert.AreEqual("Barbell Bench Press", weekTwoPreviewWorkout.PlannedExerciseName);

            var restoredService = new WorkoutScheduleService(planService, tempFilePath);

            var restoredMondayWorkout = restoredService.GetWeeklySchedule()[DayOfWeek.Monday].Single();
            Assert.AreEqual("Dumbbell Bench Press", restoredMondayWorkout.Name);
            Assert.AreEqual("Barbell Bench Press", restoredMondayWorkout.PlannedExerciseName);

            var restoredWeekTwoPreviewWorkout = restoredService.GetPlanWorkoutsForPreview(plan, 2).Single();
            Assert.AreEqual("Dumbbell Bench Press", restoredWeekTwoPreviewWorkout.Name);
            Assert.AreEqual("Barbell Bench Press", restoredWeekTwoPreviewWorkout.PlannedExerciseName);
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
    public void ReplaceActivePlanExercise_PreventsDuplicateAlternativesAndAllowsResetToOriginal()
    {
        var plan = new WorkoutPlan("Alternative Rules", "Plan")
        {
            Workouts =
            [
                new Workout("Barbell Bench Press", 185, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Incline Bench Press", 155, 10, 3, "Chest", DayOfWeek.Wednesday, DateTime.Today, WorkoutType.WeightLifting, "Main Gym")
            ]
        };
        var service = CreateServiceWithPlans(plan);

        service.AddPlanToWeeklySchedule(plan);
        service.ReplaceActivePlanExercise("Barbell Bench Press", "Dumbbell Bench Press");

        var mondayWorkout = service.GetWeeklySchedule()[DayOfWeek.Monday].Single();
        Assert.AreEqual("Dumbbell Bench Press", mondayWorkout.Name);
        Assert.AreEqual("Barbell Bench Press", mondayWorkout.PlannedExerciseName);

        service.ReplaceActivePlanExercise("Incline Bench Press", "Dumbbell Bench Press");

        var wednesdayWorkout = service.GetWeeklySchedule()[DayOfWeek.Wednesday].Single();
        Assert.AreEqual("Incline Bench Press", wednesdayWorkout.Name);
        Assert.AreEqual(string.Empty, wednesdayWorkout.PlannedExerciseName);

        service.ReplaceActivePlanExercise("Barbell Bench Press", "Barbell Bench Press");

        mondayWorkout = service.GetWeeklySchedule()[DayOfWeek.Monday].Single();
        Assert.AreEqual("Barbell Bench Press", mondayWorkout.Name);
        Assert.AreEqual(string.Empty, mondayWorkout.PlannedExerciseName);
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

    private static int GetDayOffsetFromMonday(DayOfWeek day)
    {
        var desiredIndex = GetMondayFirstDayIndex(day);
        var mondayIndex = GetMondayFirstDayIndex(DayOfWeek.Monday);
        return desiredIndex - mondayIndex;
    }

    private static int GetMondayFirstDayIndex(DayOfWeek day)
        => day == DayOfWeek.Sunday ? 6 : ((int)day - 1);

    private static DayOfWeek ShiftDayOfWeek(DayOfWeek day, int offset)
    {
        var shiftedIndex = (((int)day + offset) % 7 + 7) % 7;
        return (DayOfWeek)shiftedIndex;
    }

    private static void SetPrivateAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(field, $"Could not find backing field for {propertyName}.");
        field.SetValue(target, value);
    }
}
