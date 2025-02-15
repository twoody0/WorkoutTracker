using System;

namespace WorkoutTracker.Models;

public enum WorkoutType
{
    WeightLifting,
    Cardio
}

public class Workout
{
    public string Name { get; set; }
    public WorkoutType Type { get; set; }

    // For weight lifting workouts:
    public double Weight { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; }

    // For cardio workouts:
    public int Steps { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
