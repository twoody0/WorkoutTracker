namespace WorkoutTracker.Services;

public sealed class BodyWeightService : IBodyWeightService
{
    private const string BodyWeightPreferenceKey = "body_weight_lbs";
    private readonly IAuthService _authService;

    public BodyWeightService(IAuthService authService)
    {
        _authService = authService;
    }

    public double? GetBodyWeight()
    {
        var userWeight = _authService.CurrentUser?.Weight;
        if (userWeight.HasValue && userWeight.Value > 0)
        {
            return userWeight.Value;
        }

        return Preferences.ContainsKey(BodyWeightPreferenceKey)
            ? Preferences.Get(BodyWeightPreferenceKey, 0d)
            : null;
    }

    public bool HasBodyWeight() => GetBodyWeight().GetValueOrDefault() > 0;

    public Task SetBodyWeightAsync(double weight)
    {
        Preferences.Set(BodyWeightPreferenceKey, weight);

        if (_authService.CurrentUser != null)
        {
            _authService.CurrentUser.Weight = weight;
        }

        return Task.CompletedTask;
    }
}
