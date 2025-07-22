namespace WorkoutTracker.Models;

public class WorkoutDisplay
{
    public Workout Workout { get; set; }
    public bool IsExpanded { get; set; } = false;

    public WorkoutDisplay(Workout workout)
    {
        Workout = workout;
    }
}
