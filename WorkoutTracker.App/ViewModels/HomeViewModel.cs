using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private static readonly TimeSpan HeatFadeWindow = TimeSpan.FromHours(24);

    private readonly IAppModeService _appModeService;
    private readonly IAuthService _authService;
    private readonly IBodyWeightService _bodyWeightService;
    private readonly IThemeService _themeService;
    private readonly IWorkoutService _workoutService;
    private readonly IServiceProvider _services;
    private string _welcomeMessage = string.Empty;
    private string _todaySummary = "No recent lifting heat right now.";
    private string _heatInfoText = "Heat uses your recent lifts, body weight, and a 24-hour fade window.";
    private bool _isHeatInfoVisible;
    private double _frontShouldersOpacity;
    private double _frontChestOpacity;
    private double _frontBicepsOpacity;
    private double _frontTricepsOpacity;
    private double _frontAbsOpacity;
    private double _frontQuadsOpacity;
    private double _backShouldersOpacity;
    private double _backTricepsOpacity;
    private double _backLatsOpacity;
    private double _backLowerBackOpacity;
    private double _backGlutesOpacity;
    private double _backHamstringsOpacity;
    private double _backCalvesOpacity;

    public HomeViewModel(
        IAppModeService appModeService,
        IAuthService authService,
        IBodyWeightService bodyWeightService,
        IThemeService themeService,
        IWorkoutService workoutService,
        IServiceProvider services)
    {
        _appModeService = appModeService;
        _authService = authService;
        _bodyWeightService = bodyWeightService;
        _themeService = themeService;
        _workoutService = workoutService;
        _services = services;
        UpdateWelcomeMessage();
    }

    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            if (SetProperty(ref _welcomeMessage, value))
            {
                OnPropertyChanged(nameof(IsUserLoggedIn));
                OnPropertyChanged(nameof(ShowAuthenticationActions));
                OnPropertyChanged(nameof(CanSignOut));
                OnPropertyChanged(nameof(HasBodyWeight));
                OnPropertyChanged(nameof(BodyWeightSummary));
                OnPropertyChanged(nameof(BodyWeightInputValue));
                OnPropertyChanged(nameof(BodyWeightButtonText));
                OnPropertyChanged(nameof(ShowBodyWeightReminder));
            }
        }
    }

    public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(WelcomeMessage);

    public bool SupportsAccounts => _appModeService.SupportsAccountFeatures;

    public bool HasLeaderboard => _appModeService.HasLeaderboard;

    public bool CanSignOut => SupportsAccounts && _authService.CurrentUser != null;

    public bool ShowAuthenticationActions => SupportsAccounts && !IsUserLoggedIn;

    public string ModeDescription => _appModeService.UsesDeviceStorageOnly
        ? "Free mode stores workouts only on this device and skips login, signup, and leaderboard features."
        : "Premium mode includes accounts, signup, and leaderboard features.";

    public bool HasBodyWeight => _bodyWeightService.HasBodyWeight();

    public string BodyWeightSummary => HasBodyWeight
        ? $"Body weight: {_bodyWeightService.GetBodyWeight():N0} lb"
        : "Body weight not set yet";

    public string BodyWeightInputValue => _bodyWeightService.GetBodyWeight()?.ToString("0.#") ?? string.Empty;

    public string BodyWeightButtonText => HasBodyWeight ? "Edit Body Weight" : "Enter Body Weight";

    public bool ShowBodyWeightReminder => !HasBodyWeight;

    public string BodyWeightReminderText => "Set your body weight to improve heat map accuracy.";

    public bool IsDarkTheme => _themeService.IsDarkTheme;

    public string ThemeLabel => IsDarkTheme ? "Dark theme is on" : "Light theme is on";

    public string ThemeSupportingText => IsDarkTheme
        ? "Lower glare and stronger contrast for evening use."
        : "Bright, clean contrast for daylight and quick scanning.";

    public string ThemeButtonText => IsDarkTheme ? "Switch to Light" : "Switch to Dark";

    public string TodaySummary
    {
        get => _todaySummary;
        set => SetProperty(ref _todaySummary, value);
    }

    public string HeatInfoText
    {
        get => _heatInfoText;
        set => SetProperty(ref _heatInfoText, value);
    }

    public bool IsHeatInfoVisible
    {
        get => _isHeatInfoVisible;
        set => SetProperty(ref _isHeatInfoVisible, value);
    }

    public double FrontShouldersOpacity
    {
        get => _frontShouldersOpacity;
        set => SetProperty(ref _frontShouldersOpacity, value);
    }

    public double FrontChestOpacity
    {
        get => _frontChestOpacity;
        set => SetProperty(ref _frontChestOpacity, value);
    }

    public double FrontBicepsOpacity
    {
        get => _frontBicepsOpacity;
        set => SetProperty(ref _frontBicepsOpacity, value);
    }

    public double FrontTricepsOpacity
    {
        get => _frontTricepsOpacity;
        set => SetProperty(ref _frontTricepsOpacity, value);
    }

    public double FrontAbsOpacity
    {
        get => _frontAbsOpacity;
        set => SetProperty(ref _frontAbsOpacity, value);
    }

    public double FrontQuadsOpacity
    {
        get => _frontQuadsOpacity;
        set => SetProperty(ref _frontQuadsOpacity, value);
    }

    public double BackShouldersOpacity
    {
        get => _backShouldersOpacity;
        set => SetProperty(ref _backShouldersOpacity, value);
    }

    public double BackTricepsOpacity
    {
        get => _backTricepsOpacity;
        set => SetProperty(ref _backTricepsOpacity, value);
    }

    public double BackLatsOpacity
    {
        get => _backLatsOpacity;
        set => SetProperty(ref _backLatsOpacity, value);
    }

    public double BackLowerBackOpacity
    {
        get => _backLowerBackOpacity;
        set => SetProperty(ref _backLowerBackOpacity, value);
    }

    public double BackGlutesOpacity
    {
        get => _backGlutesOpacity;
        set => SetProperty(ref _backGlutesOpacity, value);
    }

    public double BackHamstringsOpacity
    {
        get => _backHamstringsOpacity;
        set => SetProperty(ref _backHamstringsOpacity, value);
    }

    public double BackCalvesOpacity
    {
        get => _backCalvesOpacity;
        set => SetProperty(ref _backCalvesOpacity, value);
    }

    public ICommand NavigateToLoginCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("LoginPage");
    });

    public ICommand ToggleThemeCommand => new Command(() =>
    {
        _themeService.ToggleTheme();
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(ThemeSupportingText));
        OnPropertyChanged(nameof(ThemeButtonText));
    });

    public ICommand ToggleHeatInfoCommand => new Command(() =>
    {
        IsHeatInfoVisible = !IsHeatInfoVisible;
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
        if (SupportsAccounts)
        {
            App.SetRootPage(_services.GetRequiredService<SignedOutShell>());
        }
        await Task.CompletedTask;
    });

    public void UpdateWelcomeMessage()
    {
        var user = _authService.CurrentUser;
        if (user == null)
        {
            WelcomeMessage = string.Empty;
            return;
        }

        WelcomeMessage = SupportsAccounts
            ? $"Welcome, {user.Username}"
            : "Welcome to WorkoutTracker";
    }

    public async Task RefreshHeatMapAsync()
    {
        var bodyWeight = _bodyWeightService.GetBodyWeight();
        if (_authService.CurrentUser == null || !bodyWeight.HasValue || bodyWeight.Value <= 0)
        {
            ResetHeatMap();
            return;
        }

        var now = DateTime.Now;
        var effectiveBodyWeight = Math.Max(bodyWeight.Value, 1);
        var workouts = (await _workoutService.GetWorkouts())
            .Where(workout =>
                workout.Type == WorkoutType.WeightLifting &&
                now - workout.StartTime <= HeatFadeWindow)
            .ToList();

        if (workouts.Count == 0)
        {
            ResetHeatMap();
            return;
        }

        var volumeByRegion = BuildVolumeByRegion(workouts, effectiveBodyWeight, now);

        FrontShouldersOpacity = GetHeatOpacity(volumeByRegion, "FrontShoulders");
        FrontChestOpacity = GetHeatOpacity(volumeByRegion, "FrontChest");
        FrontBicepsOpacity = GetHeatOpacity(volumeByRegion, "FrontBiceps");
        FrontTricepsOpacity = GetHeatOpacity(volumeByRegion, "FrontTriceps");
        FrontAbsOpacity = GetHeatOpacity(volumeByRegion, "FrontAbs");
        FrontQuadsOpacity = GetHeatOpacity(volumeByRegion, "FrontQuads");

        BackShouldersOpacity = GetHeatOpacity(volumeByRegion, "BackShoulders");
        BackTricepsOpacity = GetHeatOpacity(volumeByRegion, "BackTriceps");
        BackLatsOpacity = GetHeatOpacity(volumeByRegion, "BackLats");
        BackLowerBackOpacity = GetHeatOpacity(volumeByRegion, "BackLowerBack");
        BackGlutesOpacity = GetHeatOpacity(volumeByRegion, "BackGlutes");
        BackHamstringsOpacity = GetHeatOpacity(volumeByRegion, "BackHamstrings");
        BackCalvesOpacity = GetHeatOpacity(volumeByRegion, "BackCalves");

        var mostRecentWorkout = workouts.MaxBy(workout => workout.StartTime);
        var lastWorkoutAge = mostRecentWorkout == null
            ? string.Empty
            : GetRelativeAge(now - mostRecentWorkout.StartTime);
        TodaySummary = $"{workouts.Count} recent lift{(workouts.Count == 1 ? string.Empty : "s")}{lastWorkoutAge}.";
        HeatInfoText = $"Heat uses your recent lifts, body weight, and a 24-hour fade window. Current body weight: {effectiveBodyWeight:N0} lb.";
    }

    public async Task<bool> UpdateBodyWeightAsync(string? weightText)
    {
        if (!double.TryParse(weightText?.Trim(), out var weight) || weight <= 0)
        {
            return false;
        }

        await _bodyWeightService.SetBodyWeightAsync(weight);
        OnPropertyChanged(nameof(HasBodyWeight));
        OnPropertyChanged(nameof(BodyWeightSummary));
        OnPropertyChanged(nameof(BodyWeightInputValue));
        OnPropertyChanged(nameof(BodyWeightButtonText));
        OnPropertyChanged(nameof(ShowBodyWeightReminder));
        UpdateWelcomeMessage();
        await RefreshHeatMapAsync();
        return true;
    }

    private void ResetHeatMap()
    {
        FrontShouldersOpacity = 0;
        FrontChestOpacity = 0;
        FrontBicepsOpacity = 0;
        FrontTricepsOpacity = 0;
        FrontAbsOpacity = 0;
        FrontQuadsOpacity = 0;
        BackShouldersOpacity = 0;
        BackTricepsOpacity = 0;
        BackLatsOpacity = 0;
        BackLowerBackOpacity = 0;
        BackGlutesOpacity = 0;
        BackHamstringsOpacity = 0;
        BackCalvesOpacity = 0;
        TodaySummary = "No recent lifting heat right now.";
        HeatInfoText = "Heat uses your recent lifts, body weight, and a 24-hour fade window.";
        IsHeatInfoVisible = false;
    }

    private static Dictionary<string, double> BuildVolumeByRegion(IEnumerable<Workout> workouts, double bodyWeight, DateTime now)
    {
        var volumeByRegion = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var workout in workouts)
        {
            var weightRatio = Math.Max(1, workout.Weight) / bodyWeight;
            var age = now - workout.StartTime;
            var fadeMultiplier = GetFadeMultiplier(age);

            if (fadeMultiplier <= 0)
            {
                continue;
            }

            var effortScore = weightRatio * Math.Max(1, workout.Reps) * Math.Max(1, workout.Sets) * fadeMultiplier;

            foreach (var region in InferHeatRegions(workout))
            {
                if (volumeByRegion.TryGetValue(region, out var existing))
                {
                    volumeByRegion[region] = existing + effortScore;
                }
                else
                {
                    volumeByRegion[region] = effortScore;
                }
            }
        }

        return volumeByRegion;
    }

    private static double GetFadeMultiplier(TimeSpan age)
    {
        if (age <= TimeSpan.Zero)
        {
            return 1;
        }

        if (age >= HeatFadeWindow)
        {
            return 0;
        }

        var progress = age.TotalHours / HeatFadeWindow.TotalHours;
        return 1 - progress;
    }

    private static IReadOnlyList<string> InferHeatRegions(Workout workout)
    {
        var tokens = GetSearchTokens(workout);
        var muscleGroup = workout.MuscleGroup?.Trim().ToLowerInvariant() ?? string.Empty;

        if (MatchesAny(tokens, "calf", "calves", "seated calf", "standing calf"))
        {
            return ["BackCalves"];
        }

        if (MatchesAny(tokens, "hamstring", "romanian deadlift", "rdl", "leg curl", "stiff leg", "good morning"))
        {
            return ["BackHamstrings"];
        }

        if (MatchesAny(tokens, "glute", "hip thrust", "glute bridge", "kickback"))
        {
            return ["BackGlutes"];
        }

        if (MatchesAny(tokens, "lat", "pull up", "pulldown", "row", "seated row", "cable row", "barbell row"))
        {
            return ["BackLats"];
        }

        if (MatchesAny(tokens, "lower back", "erector", "superman", "back extension"))
        {
            return ["BackLowerBack"];
        }

        if (MatchesAny(tokens, "rear delt", "reverse fly", "face pull"))
        {
            return ["BackShoulders"];
        }

        if (MatchesAny(tokens, "tricep", "skull crusher", "pushdown", "overhead extension", "dip"))
        {
            return ["FrontTriceps", "BackTriceps"];
        }

        if (MatchesAny(tokens, "bicep", "curl", "hammer curl", "preacher curl"))
        {
            return ["FrontBiceps"];
        }

        if (MatchesAny(tokens, "shoulder", "lateral raise", "front raise", "upright row", "overhead press", "shoulder press"))
        {
            return ["FrontShoulders", "BackShoulders"];
        }

        if (MatchesAny(tokens, "chest", "bench", "press", "pec", "fly", "push up"))
        {
            return ["FrontChest"];
        }

        if (MatchesAny(tokens, "ab", "core", "crunch", "sit up", "leg raise", "plank", "twist"))
        {
            return ["FrontAbs", "BackLowerBack"];
        }

        if (MatchesAny(tokens, "quad", "leg extension", "lunge", "split squat", "step up"))
        {
            return ["FrontQuads"];
        }

        if (MatchesAny(tokens, "leg", "squat", "deadlift"))
        {
            return ["FrontQuads", "BackGlutes", "BackHamstrings"];
        }

        return muscleGroup switch
        {
            "biceps" => ["FrontBiceps"],
            "triceps" => ["FrontTriceps", "BackTriceps"],
            "arms" => ["FrontBiceps", "FrontTriceps", "BackTriceps"],
            "shoulders" => ["FrontShoulders", "BackShoulders"],
            "rear delts" => ["BackShoulders"],
            "back" => ["BackLats", "BackLowerBack"],
            "lats" => ["BackLats"],
            "lower back" => ["BackLowerBack"],
            "chest" => ["FrontChest"],
            "abs" => ["FrontAbs"],
            "core" => ["FrontAbs", "BackLowerBack"],
            "legs" => ["FrontQuads", "BackGlutes", "BackHamstrings"],
            "quads" => ["FrontQuads"],
            "glutes" => ["BackGlutes"],
            "hamstrings" => ["BackHamstrings"],
            "calves" => ["BackCalves"],
            _ => ["FrontChest"]
        };
    }

    private static string GetSearchTokens(Workout workout)
    {
        return $"{workout.MuscleGroup} {workout.Name}".Trim().ToLowerInvariant();
    }

    private static bool MatchesAny(string tokens, params string[] keywords)
    {
        return keywords.Any(keyword => tokens.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static double GetHeatOpacity(IReadOnlyDictionary<string, double> volumeByRegion, string region)
    {
        if (!volumeByRegion.TryGetValue(region, out var volume) || volume <= 0)
        {
            return 0;
        }

        // Around 30 means roughly a "very hard" region for the day, e.g. several
        // challenging work sets at a substantial percentage of body weight.
        var intensity = Math.Clamp(volume / 30.0, 0.0, 1.0);
        var easedIntensity = Math.Pow(intensity, 1.35);
        return 0.05 + (easedIntensity * 0.7);
    }

    private static string GetRelativeAge(TimeSpan age)
    {
        if (age.TotalHours < 1)
        {
            var minutes = Math.Max(1, (int)Math.Round(age.TotalMinutes));
            return $", last logged about {minutes} minute{(minutes == 1 ? string.Empty : "s")} ago";
        }

        var hours = Math.Max(1, (int)Math.Round(age.TotalHours));
        return $", last logged about {hours} hour{(hours == 1 ? string.Empty : "s")} ago";
    }
}
