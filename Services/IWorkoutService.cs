using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public interface IWorkoutService
{
    Task AddWorkout(Workout workout);
    Task<IEnumerable<Workout>> GetWorkouts();
}