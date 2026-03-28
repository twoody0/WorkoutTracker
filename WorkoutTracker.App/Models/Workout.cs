using System.Diagnostics.CodeAnalysis;

namespace WorkoutTracker.Models;

public enum WorkoutType
{
    WeightLifting,
    Cardio
}

public class Workout
{
    public required string Name { get; set; } = default!;
    public required string MuscleGroup { get; set; } = default!;
    public required string GymLocation { get; set; } = string.Empty;

    public double Weight { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; }
    public int? MinReps { get; set; }
    public int? MaxReps { get; set; }
    public double? TargetRpe { get; set; }
    public string TargetRestRange { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Steps { get; set; }
    public int DurationMinutes { get; set; }
    public double DistanceMiles { get; set; }
    public WorkoutType Type { get; set; }
    public DayOfWeek Day { get; set; }
    public int? PlanWeekNumber { get; set; }
    public bool HasSteps => Steps > 0;
    public bool HasDuration => DurationMinutes > 0;
    public bool HasDistance => DistanceMiles > 0;
    public bool HasRepRange => MinReps.HasValue && MaxReps.HasValue && MinReps.Value > 0 && MaxReps.Value >= MinReps.Value;
    public string RepDisplay => HasRepRange ? $"{MinReps}-{MaxReps}" : Reps.ToString();
    public bool HasTargetRpe => TargetRpe.HasValue && TargetRpe.Value > 0;
    public string TargetRpeDisplay => HasTargetRpe ? TargetRpe.GetValueOrDefault().ToString("0.#") : string.Empty;
    public bool HasTargetRestRange => !string.IsNullOrWhiteSpace(TargetRestRange);
    public double TrainingVolume => CalculateTrainingVolume(Weight, Reps, Sets);
    public double EstimatedOneRepMax => CalculateEstimatedOneRepMax(Weight, Reps);
    public bool HasEstimatedOneRepMax => Type == WorkoutType.WeightLifting && EstimatedOneRepMax > 0;

    public Workout() { }

    [SetsRequiredMembers]
    public Workout(string name, double weight, int reps, int sets, string muscleGroup, DayOfWeek day, DateTime startTime, WorkoutType type, string gymLocation)
    {
        Name = name;
        Weight = weight;
        Reps = reps;
        Sets = sets;
        MuscleGroup = muscleGroup;
        Day = day;
        StartTime = startTime;
        Type = type;
        GymLocation = gymLocation;
    }

    [SetsRequiredMembers]
    public Workout(
    string name,
    double weight,
    int reps,
    int sets,
    string muscleGroup,
    DateTime startTime,
    WorkoutType type,
    string gymLocation)
    : this(name, weight, reps, sets, muscleGroup, DayOfWeek.Monday, startTime, type, gymLocation)
    {
        // Default DayOfWeek to Monday if not provided
    }

    public static double CalculateEstimatedOneRepMax(double load, int reps)
    {
        if (load <= 0)
        {
            return 0;
        }

        if (reps <= 1)
        {
            return load;
        }

        var cappedReps = Math.Min(reps, 15);
        return load * (1 + ((cappedReps - 1) / 30.0));
    }

    public static double CalculateTrainingVolume(double load, int reps, int sets)
    {
        if (load <= 0 || reps <= 0 || sets <= 0)
        {
            return 0;
        }

        return load * reps * sets;
    }
}
