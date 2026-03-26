namespace WorkoutTracker.Services;

public sealed class AppModeService : IAppModeService
{
#if PREMIUM_EDITION
    public AppEdition Edition => AppEdition.Premium;
#else
    public AppEdition Edition => AppEdition.Free;
#endif

    public bool SupportsAccountFeatures => Edition == AppEdition.Premium;
    public bool HasLeaderboard => Edition == AppEdition.Premium;
    public bool UsesDeviceStorageOnly => Edition == AppEdition.Free;
    public bool RequiresLoginOnLaunch => Edition == AppEdition.Premium;
}
