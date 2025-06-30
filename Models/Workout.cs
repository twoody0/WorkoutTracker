namespace WorkoutTracker.Models;

public enum WorkoutType
{
    WeightLifting,
    Cardio
}

public class Workout
{
    public string Name { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; }
    public string MuscleGroup { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } 
    public int Steps { get; set; } 
    public WorkoutType Type { get; set; }
    public string GymLocation { get; set; }
}
