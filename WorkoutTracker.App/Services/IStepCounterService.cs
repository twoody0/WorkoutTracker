namespace WorkoutTracker.Services;

/// <summary>
/// Provides functionality to track and update step counts in real-time.
/// </summary>
public interface IStepCounterService
{
    #region Events

    /// <summary>
    /// Raised whenever the step count is updated.
    /// </summary>
    event EventHandler<int> StepsUpdated;

    #endregion

    #region Methods

    /// <summary>
    /// Starts tracking steps.
    /// </summary>
    void StartTracking();

    /// <summary>
    /// Stops tracking steps.
    /// </summary>
    void StopTracking();

    #endregion
}
