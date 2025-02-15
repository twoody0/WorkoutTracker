using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutService : IWorkoutService
{
    private readonly List<Workout> _workouts = new();

    public async Task AddWorkout(Workout workout)
    {
        // For MVP, just store in memory
        _workouts.Add(workout);

        // Simulate async
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Workout>> GetWorkouts()
    {
        // Return the in-memory list
        return await Task.FromResult(_workouts);
    }
}
