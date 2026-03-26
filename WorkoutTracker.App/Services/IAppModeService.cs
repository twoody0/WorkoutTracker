namespace WorkoutTracker.Services;

public enum AppEdition
{
    Free,
    Premium
}

public interface IAppModeService
{
    AppEdition Edition { get; }
    bool SupportsAccountFeatures { get; }
    bool HasLeaderboard { get; }
    bool UsesDeviceStorageOnly { get; }
    bool RequiresLoginOnLaunch { get; }
}
