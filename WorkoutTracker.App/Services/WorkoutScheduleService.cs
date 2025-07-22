using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();

    public WorkoutScheduleService()
    {
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _weeklySchedule[day] = new List<Workout>();
        }
    }

    public void AddPlanToWeeklySchedule(WorkoutPlan plan)
    {
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
                // Weight left empty for user input
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
    void AddPlanToWeeklySchedule(WorkoutPlan plan);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
}
