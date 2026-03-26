using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IWorkoutService _workoutService;
    private readonly IServiceProvider _services;
    private string _welcomeMessage = string.Empty;
    private string _todaySummary = "No lifting logged yet today.";
    private Color _headColor = Color.FromArgb("#D7D9DE");
    private Color _shouldersColor = Color.FromArgb("#D7D9DE");
    private Color _leftArmColor = Color.FromArgb("#D7D9DE");
    private Color _rightArmColor = Color.FromArgb("#D7D9DE");
    private Color _chestColor = Color.FromArgb("#D7D9DE");
    private Color _absColor = Color.FromArgb("#D7D9DE");
    private Color _leftLegColor = Color.FromArgb("#D7D9DE");
    private Color _rightLegColor = Color.FromArgb("#D7D9DE");

    public HomeViewModel(IAuthService authService, IWorkoutService workoutService, IServiceProvider services)
    {
        _authService = authService;
        _workoutService = workoutService;
        _services = services;
        UpdateWelcomeMessage();
        _ = RefreshHeatMapAsync();
    }

    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            if (SetProperty(ref _welcomeMessage, value))
            {
                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
        }
    }

    public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(WelcomeMessage);

    public string TodaySummary
    {
        get => _todaySummary;
        set => SetProperty(ref _todaySummary, value);
    }

    public Color HeadColor
    {
        get => _headColor;
        set => SetProperty(ref _headColor, value);
    }

    public Color ShouldersColor
    {
        get => _shouldersColor;
        set => SetProperty(ref _shouldersColor, value);
    }

    public Color LeftArmColor
    {
        get => _leftArmColor;
        set => SetProperty(ref _leftArmColor, value);
    }

    public Color RightArmColor
    {
        get => _rightArmColor;
        set => SetProperty(ref _rightArmColor, value);
    }

    public Color ChestColor
    {
        get => _chestColor;
        set => SetProperty(ref _chestColor, value);
    }

    public Color AbsColor
    {
        get => _absColor;
        set => SetProperty(ref _absColor, value);
    }

    public Color LeftLegColor
    {
        get => _leftLegColor;
        set => SetProperty(ref _leftLegColor, value);
    }

    public Color RightLegColor
    {
        get => _rightLegColor;
        set => SetProperty(ref _rightLegColor, value);
    }

    public ICommand NavigateToLoginCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("LoginPage");
    });

    public ICommand NavigateToSignupCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("SignupPage");
    });

    public ICommand NavigateToDashboardCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("DashboardPage");
    });

    public ICommand NavigateToLeaderboardCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("LeaderboardPage");
    });

    public ICommand SignOutCommand => new Command(async () =>
    {
        _authService.SignOut();
        UpdateWelcomeMessage();
        ResetHeatMap();
        App.SetRootPage(_services.GetRequiredService<SignedOutShell>());
        await Task.CompletedTask;
    });

    public void UpdateWelcomeMessage()
    {
        var user = _authService.CurrentUser;
        WelcomeMessage = user != null ? $"Welcome, {user.Username}" : string.Empty;
    }

    public async Task RefreshHeatMapAsync()
    {
        if (_authService.CurrentUser == null)
        {
            ResetHeatMap();
            return;
        }

        var today = DateTime.Today;
        var workouts = (await _workoutService.GetWorkouts())
            .Where(workout =>
                workout.Type == WorkoutType.WeightLifting &&
                workout.StartTime.Date == today)
            .ToList();

        if (workouts.Count == 0)
        {
            ResetHeatMap();
            return;
        }

        var volumeByRegion = workouts
            .GroupBy(workout => NormalizeRegion(workout.MuscleGroup))
            .ToDictionary(
                group => group.Key,
                group => group.Sum(workout => Math.Max(1, workout.Weight) * Math.Max(1, workout.Reps) * Math.Max(1, workout.Sets)));

        var maxVolume = volumeByRegion.Values.DefaultIfEmpty(0).Max();

        var armsColor = GetHeatColor(volumeByRegion, "Arms", maxVolume);
        var legsColor = GetHeatColor(volumeByRegion, "Legs", maxVolume);

        HeadColor = Color.FromArgb("#D7D9DE");
        ShouldersColor = GetHeatColor(volumeByRegion, "Shoulders", maxVolume);
        LeftArmColor = armsColor;
        RightArmColor = armsColor;
        ChestColor = GetHeatColor(volumeByRegion, "Chest", maxVolume);
        AbsColor = GetHeatColor(volumeByRegion, "Abs", maxVolume);
        LeftLegColor = legsColor;
        RightLegColor = legsColor;

        TodaySummary = $"Today's muscle heat is based on {workouts.Count} lift{(workouts.Count == 1 ? string.Empty : "s")} logged on {today:MMMM d}.";
    }

    private void ResetHeatMap()
    {
        var baseColor = Color.FromArgb("#D7D9DE");
        HeadColor = baseColor;
        ShouldersColor = baseColor;
        LeftArmColor = baseColor;
        RightArmColor = baseColor;
        ChestColor = baseColor;
        AbsColor = baseColor;
        LeftLegColor = baseColor;
        RightLegColor = baseColor;
        TodaySummary = "No lifting logged yet today.";
    }

    private static string NormalizeRegion(string muscleGroup)
    {
        return muscleGroup.Trim().ToLowerInvariant() switch
        {
            "biceps" => "Arms",
            "triceps" => "Arms",
            "arms" => "Arms",
            "shoulders" => "Shoulders",
            "back" => "Shoulders",
            "chest" => "Chest",
            "abs" => "Abs",
            "core" => "Abs",
            "legs" => "Legs",
            _ => "Chest"
        };
    }

    private static Color GetHeatColor(IReadOnlyDictionary<string, double> volumeByRegion, string region, double maxVolume)
    {
        var baseColor = Color.FromArgb("#D7D9DE");
        var warmColor = Color.FromArgb("#FF7043");

        if (!volumeByRegion.TryGetValue(region, out var volume) || maxVolume <= 0)
        {
            return baseColor;
        }

        var intensity = Math.Clamp(volume / maxVolume, 0.0, 1.0);

        return new Color(
            red: (float)(baseColor.Red + ((warmColor.Red - baseColor.Red) * intensity)),
            green: (float)(baseColor.Green + ((warmColor.Green - baseColor.Green) * intensity)),
            blue: (float)(baseColor.Blue + ((warmColor.Blue - baseColor.Blue) * intensity)));
    }
}
