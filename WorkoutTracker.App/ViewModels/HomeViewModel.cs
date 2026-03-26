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

    public HomeViewModel(IAuthService authService, IWorkoutService workoutService, IServiceProvider services)
    {
        _authService = authService;
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
            }
        }
    }

    public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(WelcomeMessage);

    public string TodaySummary
    {
        get => _todaySummary;
        set => SetProperty(ref _todaySummary, value);
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
        var bodyWeight = Math.Max(_authService.CurrentUser?.Weight ?? 180, 1);
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

        var volumeByRegion = BuildVolumeByRegion(workouts, bodyWeight);

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

        TodaySummary = $"Today's muscle heat is based on {workouts.Count} lift{(workouts.Count == 1 ? string.Empty : "s")} logged on {today:MMMM d}, normalized to your {bodyWeight:N0} lb body weight.";
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
        TodaySummary = "No lifting logged yet today.";
    }

    private static Dictionary<string, double> BuildVolumeByRegion(IEnumerable<Workout> workouts, double bodyWeight)
    {
        var volumeByRegion = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var workout in workouts)
        {
            var weightRatio = Math.Max(1, workout.Weight) / bodyWeight;
            var effortScore = weightRatio * Math.Max(1, workout.Reps) * Math.Max(1, workout.Sets);

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
}
