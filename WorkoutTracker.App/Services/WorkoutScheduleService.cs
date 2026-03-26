using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();
    private readonly IWorkoutPlanService _workoutPlanService;
    public WorkoutPlan? ActivePlan { get; private set; }
    public DateTime? ActivePlanStartedOn { get; private set; }
    public DateTime? ActivePlanEndsOn { get; private set; }
    public bool HasCompletedActivePlan => ActivePlanEndsOn.HasValue && DateTime.Today > ActivePlanEndsOn.Value.Date;

    public WorkoutScheduleService(IWorkoutPlanService workoutPlanService)
    {
        _workoutPlanService = workoutPlanService;

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _weeklySchedule[day] = new List<Workout>();
        }
    }

    public void AddPlanToWeeklySchedule(WorkoutPlan plan)
    {
        ActivePlan = plan;
        ActivePlanStartedOn = DateTime.Today;
        ActivePlanEndsOn = DateTime.Today.AddDays((plan.DurationInWeeks * 7) - 1);

        foreach (var day in _weeklySchedule.Keys.ToList())
        {
            _weeklySchedule[day].Clear();
        }

        foreach (var workout in plan.Workouts)
        {
            AddWorkoutToDay(workout.Day, CloneWorkout(workout));
        }
    }

    public void AddWorkoutToDay(DayOfWeek day, Workout workout)
    {
        if (!_weeklySchedule.ContainsKey(day))
        {
            _weeklySchedule[day] = new List<Workout>();
        }

        workout.Day = day;
        _weeklySchedule[day].Add(workout);
    }

    public void RemoveWorkoutFromDay(DayOfWeek day, Workout workout)
    {
        if (_weeklySchedule.ContainsKey(day))
        {
            _weeklySchedule[day].Remove(workout);
        }
    }

    public IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule()
    {
        return _weeklySchedule;
    }

    public IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day)
    {
        if (ActivePlan == null)
        {
            return [];
        }

        return ActivePlan.Workouts
            .Where(workout => workout.Day == day)
            .Select(CloneWorkout)
            .ToList();
    }

    public void RestartActivePlan()
    {
        if (ActivePlan == null)
        {
            return;
        }

        AddPlanToWeeklySchedule(ActivePlan);
    }

    public WorkoutPlan? GetSuggestedNextPlan()
    {
        if (ActivePlan == null)
        {
            return null;
        }

        var planProgressionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Beginner Full Body Foundation"] = "Upper/Lower Strength Builder",
            ["Upper/Lower Strength Builder"] = "Push/Pull/Legs Hypertrophy",
            ["Brisk Walking Starter"] = "Interval Conditioning Builder",
            ["Interval Conditioning Builder"] = "Couch to 5K Starter"
        };

        if (planProgressionMap.TryGetValue(ActivePlan.Name, out var nextPlanName))
        {
            return _workoutPlanService.GetWorkoutPlans()
                .FirstOrDefault(plan =>
                    !plan.IsCustom &&
                    string.Equals(plan.Name, nextPlanName, StringComparison.OrdinalIgnoreCase));
        }

        return _workoutPlanService.GetWorkoutPlans()
            .FirstOrDefault(plan =>
                !plan.IsCustom &&
                !string.Equals(plan.Name, ActivePlan.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(plan.Category, ActivePlan.Category, StringComparison.OrdinalIgnoreCase));
    }

    public string GetActivePlanTimelineSummary()
    {
        if (ActivePlan == null || !ActivePlanStartedOn.HasValue || !ActivePlanEndsOn.HasValue)
        {
            return "No active plan timeline.";
        }

        if (HasCompletedActivePlan)
        {
            return $"Completed on {ActivePlanEndsOn.Value:d}.";
        }

        var daysRemaining = (ActivePlanEndsOn.Value.Date - DateTime.Today).Days + 1;
        var weeksRemaining = Math.Max(1, (int)Math.Ceiling(daysRemaining / 7d));
        return $"{ActivePlan.DurationDisplay} plan. {weeksRemaining} week{(weeksRemaining == 1 ? string.Empty : "s")} remaining until {ActivePlanEndsOn.Value:d}.";
    }

    private static Workout CloneWorkout(Workout workout)
    {
        return new Workout(
            name: workout.Name,
            weight: workout.Weight,
            reps: workout.Reps,
            sets: workout.Sets,
            muscleGroup: workout.MuscleGroup,
            day: workout.Day,
            startTime: workout.StartTime,
            type: workout.Type,
            gymLocation: workout.GymLocation)
        {
            EndTime = workout.EndTime,
            Steps = workout.Steps
        };
    }
}

public interface IWorkoutScheduleService
{
    WorkoutPlan? ActivePlan { get; }
    DateTime? ActivePlanStartedOn { get; }
    DateTime? ActivePlanEndsOn { get; }
    bool HasCompletedActivePlan { get; }
    void AddPlanToWeeklySchedule(WorkoutPlan plan);
    void AddWorkoutToDay(DayOfWeek day, Workout workout);
    void RemoveWorkoutFromDay(DayOfWeek day, Workout workout);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
    IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day);
    void RestartActivePlan();
    WorkoutPlan? GetSuggestedNextPlan();
    string GetActivePlanTimelineSummary();
}
