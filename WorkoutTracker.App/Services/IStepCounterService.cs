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

    #region Properties

    /// <summary>
    /// Human-readable description of the source currently used for steps.
    /// </summary>
    string SourceDescription { get; }

    /// <summary>
    /// Indicates whether step tracking is available on this device.
    /// </summary>
    bool IsAvailable { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Requests any platform permissions or authorizations required for step tracking.
    /// </summary>
    Task<bool> EnsureAccessAsync();

    /// <summary>
    /// Starts tracking steps for a session beginning at the supplied time.
    /// </summary>
    void StartTracking(DateTimeOffset sessionStartedAtUtc);

    /// <summary>
    /// Stops tracking steps.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Reconciles step data for a completed session window and returns the best available total.
    /// </summary>
    Task<int> GetFinalStepCountAsync(DateTimeOffset sessionStartedAtUtc, DateTimeOffset sessionEndedAtUtc);

    #endregion
}
