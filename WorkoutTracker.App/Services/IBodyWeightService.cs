namespace WorkoutTracker.Services;

public interface IBodyWeightService
{
    double? GetBodyWeight();
    bool HasBodyWeight();
    Task SetBodyWeightAsync(double weight);
}
