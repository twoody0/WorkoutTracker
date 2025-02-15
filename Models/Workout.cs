namespace WorkoutTracker.Models;

public class Workout
{
    public string Name { get; set; }       // Optional: name of the workout (e.g., "Bench Press")
    public double Weight { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
