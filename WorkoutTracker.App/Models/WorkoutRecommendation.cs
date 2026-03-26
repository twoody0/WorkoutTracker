namespace WorkoutTracker.Models;

public class WorkoutRecommendation
{
    public required Workout Workout { get; init; }
    public double? LastUsedWeight { get; init; }
    public bool HasLastUsedWeight => LastUsedWeight.HasValue;
}
