using WorkoutTracker.Models;
using WorkoutTracker.Services;

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

    private static WorkoutScheduleService CreateServiceWithPlans(params WorkoutPlan[] plans)
    {
        var planService = new FakeWorkoutPlanService(plans);
        return new WorkoutScheduleService(planService);
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
    }
}
