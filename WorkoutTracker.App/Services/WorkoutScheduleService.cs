using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();
    public WorkoutPlan? ActivePlan { get; private set; }

    public WorkoutScheduleService()
    {
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _weeklySchedule[day] = new List<Workout>();
        }
    }

    public void AddPlanToWeeklySchedule(WorkoutPlan plan)
    {
        ActivePlan = plan; // Track the active plan

        // Clear existing schedule
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
    void AddPlanToWeeklySchedule(WorkoutPlan plan);
    void AddWorkoutToDay(DayOfWeek day, Workout workout);
    void RemoveWorkoutFromDay(DayOfWeek day, Workout workout);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
    IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day);
}
