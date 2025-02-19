namespace WorkoutTracker.Services;

public interface IStepCounterService
{
    event EventHandler<int> StepsUpdated;

    void StartTracking();
    void StopTracking();
}
