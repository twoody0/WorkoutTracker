namespace WorkoutTracker.Services;

public sealed class StepCounterServiceNull : IStepCounterService
{
    public event EventHandler<int>? StepsUpdated;

    public string SourceDescription => "Step tracking is unavailable on this device.";

    public bool IsAvailable => false;

    public Task<bool> EnsureAccessAsync() => Task.FromResult(false);

    public void StartTracking(DateTimeOffset sessionStartedAtUtc)
    {
        StepsUpdated?.Invoke(this, 0);
    }

    public void StopTracking()
    {
    }
}
