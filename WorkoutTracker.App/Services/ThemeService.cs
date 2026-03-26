namespace WorkoutTracker.Services;

public class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "app_theme";

    public AppTheme CurrentTheme => Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

    public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;

    public void ApplySavedTheme()
    {
        var savedTheme = Preferences.Get(ThemePreferenceKey, nameof(AppTheme.Dark));
        var theme = Enum.TryParse<AppTheme>(savedTheme, out var parsedTheme)
            ? parsedTheme
            : AppTheme.Dark;

        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = theme;
        }
    }

    public AppTheme ToggleTheme()
    {
        var nextTheme = IsDarkTheme ? AppTheme.Light : AppTheme.Dark;

        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = nextTheme;
        }

        Preferences.Set(ThemePreferenceKey, nextTheme.ToString());
        return nextTheme;
    }
}
