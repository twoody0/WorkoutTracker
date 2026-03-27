namespace WorkoutTracker.Services;

public sealed class BodyWeightService : IBodyWeightService
{
    private const string BodyWeightPreferenceKey = "body_weight_lbs";
    private const string BodyWeightSettingKey = "body_weight_lbs";
    private readonly IAuthService _authService;
    private readonly WorkoutTrackerDatabase _database;

    public BodyWeightService(IAuthService authService, string? databasePath = null)
    {
        _authService = authService;
        _database = new WorkoutTrackerDatabase(databasePath);
    }

    public double? GetBodyWeight()
    {
        var userWeight = _authService.CurrentUser?.Weight;
        if (userWeight.HasValue && userWeight.Value > 0)
        {
            return userWeight.Value;
        }

        MigrateLegacyPreferenceIfNeeded();

        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AppSettings WHERE Key = $key;";
        command.Parameters.AddWithValue("$key", BodyWeightSettingKey);

        var result = command.ExecuteScalar()?.ToString();
        return double.TryParse(result, out var weight) && weight > 0
            ? weight
            : null;
    }

    public bool HasBodyWeight() => GetBodyWeight().GetValueOrDefault() > 0;

    public Task SetBodyWeightAsync(double weight)
    {
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AppSettings (Key, Value)
            VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        command.Parameters.AddWithValue("$key", BodyWeightSettingKey);
        command.Parameters.AddWithValue("$value", weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        if (_authService.CurrentUser != null)
        {
            _authService.CurrentUser.Weight = weight;
        }

        return Task.CompletedTask;
    }

    private void MigrateLegacyPreferenceIfNeeded()
    {
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM AppSettings WHERE Key = $key;";
        command.Parameters.AddWithValue("$key", BodyWeightSettingKey);
        var hasStoredValue = Convert.ToInt32(command.ExecuteScalar()) > 0;

        if (hasStoredValue || !Preferences.ContainsKey(BodyWeightPreferenceKey))
        {
            return;
        }

        var legacyWeight = Preferences.Get(BodyWeightPreferenceKey, 0d);
        if (legacyWeight <= 0)
        {
            return;
        }

        using var upsertCommand = connection.CreateCommand();
        upsertCommand.CommandText =
            """
            INSERT INTO AppSettings (Key, Value)
            VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        upsertCommand.Parameters.AddWithValue("$key", BodyWeightSettingKey);
        upsertCommand.Parameters.AddWithValue("$value", legacyWeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
        upsertCommand.ExecuteNonQuery();

        Preferences.Remove(BodyWeightPreferenceKey);
    }
}
