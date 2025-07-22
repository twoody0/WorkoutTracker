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

        // Distribute workouts across the week
        int dayIndex = 0;
        foreach (var workout in plan.Workouts)
        {
            var day = (DayOfWeek)(dayIndex % 7);
            _weeklySchedule[day].Add(new Workout
            {
                Name = workout.Name,
                MuscleGroup = workout.MuscleGroup,
                GymLocation = workout.GymLocation,
                Reps = workout.Reps,
                Sets = workout.Sets,
                Type = workout.Type
                // Weight left blank for user to fill
            });
            dayIndex++;
        }
    }

    public IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule()
    {
        return _weeklySchedule;
    }
}

public interface IWorkoutScheduleService
{
    WorkoutPlan? ActivePlan { get; }
    void AddPlanToWeeklySchedule(WorkoutPlan plan);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
}
