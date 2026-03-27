namespace WorkoutTracker.Services;

public class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "app_theme";
    private const string ThemeSettingKey = "app_theme";
    private readonly WorkoutTrackerDatabase _database;

    public ThemeService(string? databasePath = null)
    {
        _database = new WorkoutTrackerDatabase(databasePath);
    }

    public AppTheme CurrentTheme => Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

    public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;

    public void ApplySavedTheme()
    {
        MigrateLegacyPreferenceIfNeeded();
        var savedTheme = GetStoredTheme() ?? nameof(AppTheme.Dark);
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

        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AppSettings (Key, Value)
            VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        command.Parameters.AddWithValue("$key", ThemeSettingKey);
        command.Parameters.AddWithValue("$value", nextTheme.ToString());
        command.ExecuteNonQuery();
        return nextTheme;
    }

    private string? GetStoredTheme()
    {
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AppSettings WHERE Key = $key;";
        command.Parameters.AddWithValue("$key", ThemeSettingKey);
        return command.ExecuteScalar()?.ToString();
    }

    private void MigrateLegacyPreferenceIfNeeded()
    {
        if (!Preferences.ContainsKey(ThemePreferenceKey) || !string.IsNullOrWhiteSpace(GetStoredTheme()))
        {
            return;
        }

        var legacyTheme = Preferences.Get(ThemePreferenceKey, nameof(AppTheme.Dark));
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AppSettings (Key, Value)
            VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        command.Parameters.AddWithValue("$key", ThemeSettingKey);
        command.Parameters.AddWithValue("$value", legacyTheme);
        command.ExecuteNonQuery();

        Preferences.Remove(ThemePreferenceKey);
    }
}
