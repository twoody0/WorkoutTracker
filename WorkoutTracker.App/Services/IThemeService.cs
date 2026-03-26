namespace WorkoutTracker.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    bool IsDarkTheme { get; }
    void ApplySavedTheme();
    AppTheme ToggleTheme();
}
