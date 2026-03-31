using System.Text.Json;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private static readonly TimeSpan HeatFadeWindow = TimeSpan.FromHours(24);
    private const double MinimumHeatOpacity = 0.05;
    private const double MaximumHeatOpacity = 0.60;
    private const double HighHeatVolumeThreshold = 60.0;
    private const int RecentExerciseComparisonWindow = 10;
    private const double HistoricalLoadPreference = 0.65;
    private static Dictionary<string, ExerciseHeatMapDefinition>? _exerciseHeatMapLookup;

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
    private double _frontForearmsOpacity;
    private double _backShouldersOpacity;
    private double _backTricepsOpacity;
    private double _backLatsOpacity;
    private double _backLowerBackOpacity;
    private double _backGlutesOpacity;
    private double _backHamstringsOpacity;
    private double _backCalvesOpacity;
    private double _backTrapsOpacity;
    private double _backRhomboidsOpacity;

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

    public string BodyWeightButtonText => HasBodyWeight ? "Edit Weight" : "Set Weight";

    public bool ShowBodyWeightReminder => !HasBodyWeight;

    public string BodyWeightReminderText => "Set your body weight to improve heat map accuracy.";

    public bool IsDarkTheme => _themeService.IsDarkTheme;

    public string ThemeLabel => IsDarkTheme ? "Dark theme is on" : "Light theme is on";

    public string ThemeSupportingText => IsDarkTheme
        ? "Lower glare and stronger contrast for evening use."
        : "Bright, clean contrast for daylight and quick scanning.";

    public string ThemeButtonText => IsDarkTheme ? "Use Light Mode" : "Use Dark Mode";

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

    public double FrontForearmsOpacity
    {
        get => _frontForearmsOpacity;
        set => SetProperty(ref _frontForearmsOpacity, value);
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

    public double BackTrapsOpacity
    {
        get => _backTrapsOpacity;
        set => SetProperty(ref _backTrapsOpacity, value);
    }

    public double BackRhomboidsOpacity
    {
        get => _backRhomboidsOpacity;
        set => SetProperty(ref _backRhomboidsOpacity, value);
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
        await Shell.Current.GoToAsync("//dashboard");
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
            : "Welcome to Megnor";
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
        var allWorkouts = (await _workoutService.GetWorkouts())
            .Where(workout => workout.Type == WorkoutType.WeightLifting)
            .OrderBy(workout => workout.StartTime)
            .ToList();
        var workouts = allWorkouts
            .Where(workout => now - workout.StartTime <= HeatFadeWindow)
            .ToList();

        if (workouts.Count == 0)
        {
            ResetHeatMap();
            return;
        }

        await EnsureHeatMapLookupLoadedAsync();
        var volumeByRegion = BuildVolumeByRegion(workouts, allWorkouts, effectiveBodyWeight, now);

        FrontShouldersOpacity = GetHeatOpacity(volumeByRegion, "FrontShoulders");
        FrontChestOpacity = GetHeatOpacity(volumeByRegion, "FrontChest");
        FrontBicepsOpacity = GetHeatOpacity(volumeByRegion, "FrontBiceps");
        FrontTricepsOpacity = GetHeatOpacity(volumeByRegion, "FrontTriceps");
        FrontAbsOpacity = GetHeatOpacity(volumeByRegion, "FrontAbs");
        FrontQuadsOpacity = GetHeatOpacity(volumeByRegion, "FrontQuads");
        FrontForearmsOpacity = GetHeatOpacity(volumeByRegion, "FrontForearms");

        BackShouldersOpacity = GetHeatOpacity(volumeByRegion, "BackShoulders");
        BackTricepsOpacity = GetHeatOpacity(volumeByRegion, "BackTriceps");
        BackLatsOpacity = GetHeatOpacity(volumeByRegion, "BackLats");
        BackLowerBackOpacity = GetHeatOpacity(volumeByRegion, "BackLowerBack");
        BackGlutesOpacity = GetHeatOpacity(volumeByRegion, "BackGlutes");
        BackHamstringsOpacity = GetHeatOpacity(volumeByRegion, "BackHamstrings");
        BackCalvesOpacity = GetHeatOpacity(volumeByRegion, "BackCalves");
        BackTrapsOpacity = GetHeatOpacity(volumeByRegion, "BackTraps");
        BackRhomboidsOpacity = GetHeatOpacity(volumeByRegion, "BackRhomboids");

        var mostRecentWorkout = workouts.MaxBy(workout => workout.StartTime);
        var lastWorkoutAge = mostRecentWorkout == null
            ? string.Empty
            : GetRelativeAge(now - mostRecentWorkout.StartTime);
        var progressionCount = workouts.Count(workout => GetProgressionHeatMultiplier(workout, FindRecentExerciseHistory(allWorkouts, workout)) > 1.05);
        var progressionText = progressionCount > 0
            ? $" {progressionCount} beat your previous mark."
            : string.Empty;
        TodaySummary = $"{workouts.Count} recent lift{(workouts.Count == 1 ? string.Empty : "s")}{lastWorkoutAge}.{progressionText}";
        HeatInfoText = $"Heat uses your recent lifts, body weight, estimated 1RM, and a 24-hour fade window. Beating your last workout adds extra heat. Current body weight: {effectiveBodyWeight:N0} lb.";
    }

    public async Task<bool> UpdateBodyWeightAsync(string? weightText)
    {
        if (!InputSanitizer.TryParseBodyWeight(weightText, out var weight))
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
        FrontForearmsOpacity = 0;
        BackShouldersOpacity = 0;
        BackTricepsOpacity = 0;
        BackLatsOpacity = 0;
        BackLowerBackOpacity = 0;
        BackGlutesOpacity = 0;
        BackHamstringsOpacity = 0;
        BackCalvesOpacity = 0;
        BackTrapsOpacity = 0;
        BackRhomboidsOpacity = 0;
        TodaySummary = "No recent lifting heat right now.";
        HeatInfoText = "Heat uses your recent lifts, body weight, and a 24-hour fade window.";
        IsHeatInfoVisible = false;
    }

    private static Dictionary<string, double> BuildVolumeByRegion(IEnumerable<Workout> recentWorkouts, IReadOnlyList<Workout> allWorkouts, double bodyWeight, DateTime now)
    {
        var volumeByRegion = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var workout in recentWorkouts)
        {
            var age = now - workout.StartTime;
            var fadeMultiplier = GetFadeMultiplier(age);

            if (fadeMultiplier <= 0)
            {
                continue;
            }

            var definition = FindHeatMapDefinition(workout);
            var recentHistory = FindRecentExerciseHistory(allWorkouts, workout);
            var loadMultiplier = GetHeatLoadMultiplier(workout, bodyWeight, recentHistory);
            var movementEffortMultiplier = definition?.EffortMultiplier ?? 1.0;
            var progressionMultiplier = GetProgressionHeatMultiplier(workout, recentHistory);
            var repIntentMultiplier = GetRepIntentHeatMultiplier(workout);
            var effortScore = loadMultiplier *
                              Math.Max(1, workout.Reps) *
                              Math.Max(1, workout.Sets) *
                              fadeMultiplier *
                              movementEffortMultiplier *
                              progressionMultiplier *
                              repIntentMultiplier;

            foreach (var regionContribution in GetHeatRegions(workout, definition))
            {
                var weightedEffort = effortScore * regionContribution.Value;
                if (weightedEffort <= 0)
                {
                    continue;
                }

                if (volumeByRegion.TryGetValue(regionContribution.Key, out var existing))
                {
                    volumeByRegion[regionContribution.Key] = existing + weightedEffort;
                }
                else
                {
                    volumeByRegion[regionContribution.Key] = weightedEffort;
                }
            }
        }

        return volumeByRegion;
    }

    private static IReadOnlyList<Workout> FindRecentExerciseHistory(IReadOnlyList<Workout> allWorkouts, Workout currentWorkout)
    {
        return allWorkouts
            .Where(workout =>
                workout.StartTime < currentWorkout.StartTime &&
                string.Equals(workout.Name, currentWorkout.Name, StringComparison.OrdinalIgnoreCase) &&
                workout.Type == WorkoutType.WeightLifting)
            .OrderByDescending(workout => workout.StartTime)
            .Take(RecentExerciseComparisonWindow)
            .ToList();
    }

    private static double GetProgressionHeatMultiplier(Workout workout, IReadOnlyList<Workout> recentHistory)
    {
        if (recentHistory.Count == 0 || workout.Type != WorkoutType.WeightLifting)
        {
            return 1.0;
        }

        var currentEstimatedMax = workout.EstimatedOneRepMax;
        var currentVolume = workout.TrainingVolume;
        var averageEstimatedMax = recentHistory
            .Where(previousWorkout => previousWorkout.EstimatedOneRepMax > 0)
            .Select(previousWorkout => previousWorkout.EstimatedOneRepMax)
            .DefaultIfEmpty(0)
            .Average();
        var bestEstimatedMax = recentHistory.Max(previousWorkout => previousWorkout.EstimatedOneRepMax);
        var averageVolume = recentHistory
            .Where(previousWorkout => previousWorkout.TrainingVolume > 0)
            .Select(previousWorkout => previousWorkout.TrainingVolume)
            .DefaultIfEmpty(0)
            .Average();
        var bestVolume = recentHistory.Max(previousWorkout => previousWorkout.TrainingVolume);
        var bestRecentWeight = recentHistory.Max(previousWorkout => previousWorkout.Weight);

        var averageEstimatedMaxGain = GetPositiveImprovement(currentEstimatedMax, averageEstimatedMax);
        var bestEstimatedMaxGain = GetPositiveImprovement(currentEstimatedMax, bestEstimatedMax);
        var averageVolumeGain = GetPositiveImprovement(currentVolume, averageVolume);
        var bestVolumeGain = GetPositiveImprovement(currentVolume, bestVolume);
        var lowRepPushBonus = workout.Reps <= 3 && workout.Weight > bestRecentWeight ? 0.08 : 0.0;

        var bonus = Math.Min(0.18, averageEstimatedMaxGain * 0.35) +
                    Math.Min(0.10, bestEstimatedMaxGain * 0.25) +
                    Math.Min(0.12, averageVolumeGain * 0.16) +
                    Math.Min(0.08, bestVolumeGain * 0.10) +
                    lowRepPushBonus;

        return 1.0 + Math.Min(0.35, bonus);
    }

    private static double GetHeatLoadMultiplier(Workout workout, double bodyWeight, IReadOnlyList<Workout> recentHistory)
    {
        var bodyWeightRatio = Math.Max(1, workout.Weight) / bodyWeight;
        if (recentHistory.Count == 0)
        {
            return bodyWeightRatio;
        }

        var currentEstimatedMax = workout.EstimatedOneRepMax;
        var averageEstimatedMax = recentHistory
            .Where(previousWorkout => previousWorkout.EstimatedOneRepMax > 0)
            .Select(previousWorkout => previousWorkout.EstimatedOneRepMax)
            .DefaultIfEmpty(0)
            .Average();
        var recentAverageLoad = recentHistory
            .Where(previousWorkout => previousWorkout.Weight > 0)
            .Select(previousWorkout => previousWorkout.Weight)
            .DefaultIfEmpty(0)
            .Average();

        var historicalEstimatedMaxRatio = currentEstimatedMax > 0 && averageEstimatedMax > 0
            ? currentEstimatedMax / averageEstimatedMax
            : 1.0;
        var historicalLoadRatio = recentAverageLoad > 0
            ? Math.Max(1, workout.Weight) / recentAverageLoad
            : 1.0;
        var historicalSignal = Math.Clamp((historicalEstimatedMaxRatio * 0.65) + (historicalLoadRatio * 0.35), 0.75, 1.5);
        var historyBlend = Math.Min(1.0, recentHistory.Count / (double)RecentExerciseComparisonWindow);

        return (bodyWeightRatio * (1.0 - (historyBlend * HistoricalLoadPreference))) +
               (historicalSignal * (historyBlend * HistoricalLoadPreference));
    }

    private static double GetRepIntentHeatMultiplier(Workout workout)
    {
        if (workout.Type != WorkoutType.WeightLifting)
        {
            return 1.0;
        }

        return workout.Reps switch
        {
            <= 1 => 1.35,
            <= 3 => 1.22,
            <= 5 => 1.10,
            _ => 1.0
        };
    }

    private static double GetPositiveImprovement(double currentValue, double previousValue)
    {
        if (currentValue <= 0 || previousValue <= 0 || currentValue <= previousValue)
        {
            return 0;
        }

        return (currentValue - previousValue) / previousValue;
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

    private static ExerciseHeatMapDefinition? FindHeatMapDefinition(Workout workout)
    {
        if (_exerciseHeatMapLookup == null)
        {
            return null;
        }

        var possibleNames = new[]
        {
            workout.Name?.Trim(),
            workout.Name?.Replace("’", "'").Trim()
        };

        foreach (var possibleName in possibleNames)
        {
            if (!string.IsNullOrWhiteSpace(possibleName) &&
                _exerciseHeatMapLookup.TryGetValue(possibleName, out var definition))
            {
                return definition;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, double> GetHeatRegions(Workout workout, ExerciseHeatMapDefinition? definition)
    {
        if (definition?.Regions?.Count > 0)
        {
            return definition.Regions;
        }

        var tokens = GetSearchTokens(workout);
        var muscleGroup = workout.MuscleGroup?.Trim().ToLowerInvariant() ?? string.Empty;

        if (MatchesAny(tokens, "calf", "calves", "seated calf", "standing calf"))
        {
            return WeightedRegions(("BackCalves", 1.0));
        }

        if (MatchesAny(tokens, "hamstring curl", "leg curl"))
        {
            return WeightedRegions(("BackHamstrings", 1.0), ("BackGlutes", 0.15));
        }

        if (MatchesAny(tokens, "romanian deadlift", "rdl", "stiff leg", "stiff-leg", "good morning"))
        {
            return WeightedRegions(
                ("BackHamstrings", 1.0),
                ("BackGlutes", 0.65),
                ("BackLowerBack", 0.45),
                ("BackLats", 0.15));
        }

        if (MatchesAny(tokens, "glute", "hip thrust", "glute bridge", "kickback"))
        {
            return WeightedRegions(("BackGlutes", 1.0), ("BackHamstrings", 0.25));
        }

        if (MatchesAny(tokens, "deadlift", "trap bar deadlift"))
        {
            return WeightedRegions(
                ("BackGlutes", 0.9),
                ("BackHamstrings", 0.8),
                ("BackLowerBack", 0.65),
                ("FrontQuads", 0.35),
                ("BackLats", 0.2));
        }

        if (MatchesAny(tokens, "pull up", "pull-up", "chin up", "chin-up", "pulldown", "lat pulldown"))
        {
            return WeightedRegions(
                ("BackLats", 1.0),
                ("FrontBiceps", 0.45),
                ("BackShoulders", 0.2));
        }

        if (MatchesAny(tokens, "row", "seated row", "cable row", "barbell row", "dumbbell row", "chest-supported row", "t-bar row"))
        {
            return WeightedRegions(
                ("BackLats", 0.9),
                ("BackShoulders", 0.45),
                ("FrontBiceps", 0.35));
        }

        if (MatchesAny(tokens, "lower back", "erector", "superman", "back extension"))
        {
            return WeightedRegions(
                ("BackLowerBack", 1.0),
                ("BackGlutes", 0.25),
                ("BackHamstrings", 0.2));
        }

        if (MatchesAny(tokens, "rear delt", "reverse fly", "face pull"))
        {
            return WeightedRegions(
                ("BackShoulders", 1.0),
                ("BackLats", 0.2));
        }

        if (MatchesAny(tokens, "tricep", "skull crusher", "pushdown", "overhead extension"))
        {
            return WeightedRegions(
                ("FrontTriceps", 0.9),
                ("BackTriceps", 1.0));
        }

        if (MatchesAny(tokens, "triceps dip", "bodyweight dip", "dips", "dip"))
        {
            if (MatchesAny(tokens, "chest dip"))
            {
                return WeightedRegions(
                    ("FrontChest", 0.75),
                    ("FrontTriceps", 0.5),
                    ("BackTriceps", 0.55),
                    ("FrontShoulders", 0.35));
            }

            return WeightedRegions(
                ("FrontTriceps", 0.65),
                ("BackTriceps", 0.8),
                ("FrontChest", 0.25),
                ("FrontShoulders", 0.2));
        }

        if (MatchesAny(tokens, "hammer curl", "reverse curl"))
        {
            return WeightedRegions(("FrontBiceps", 0.85));
        }

        if (MatchesAny(tokens, "bicep", "curl", "preacher curl", "concentration curl", "ez-bar curl"))
        {
            return WeightedRegions(("FrontBiceps", 1.0));
        }

        if (MatchesAny(tokens, "landmine press"))
        {
            return WeightedRegions(
                ("FrontShoulders", 0.8),
                ("BackShoulders", 0.35),
                ("FrontChest", 0.3),
                ("FrontTriceps", 0.3));
        }

        if (MatchesAny(tokens, "overhead press", "shoulder press", "arnold press", "push press", "machine shoulder press"))
        {
            return WeightedRegions(
                ("FrontShoulders", 1.0),
                ("BackShoulders", 0.4),
                ("FrontTriceps", 0.45),
                ("BackTriceps", 0.2));
        }

        if (MatchesAny(tokens, "lateral raise", "front raise", "upright row"))
        {
            return WeightedRegions(
                ("FrontShoulders", 0.9),
                ("BackShoulders", 0.45));
        }

        if (MatchesAny(tokens, "shoulder"))
        {
            return WeightedRegions(
                ("FrontShoulders", 1.0),
                ("BackShoulders", 0.45));
        }

        if (MatchesAny(tokens, "push up", "push-up", "incline push-up", "wall push-up", "elevated push-up"))
        {
            return WeightedRegions(
                ("FrontChest", 0.85),
                ("FrontTriceps", 0.4),
                ("FrontShoulders", 0.3));
        }

        if (MatchesAny(tokens, "bench", "chest press", "pec", "fly", "crossover", "press"))
        {
            return WeightedRegions(
                ("FrontChest", 1.0),
                ("FrontTriceps", 0.4),
                ("FrontShoulders", 0.35));
        }

        if (MatchesAny(tokens, "carry", "pallof", "dead bug", "bird dog"))
        {
            return WeightedRegions(
                ("FrontAbs", 0.85),
                ("BackLowerBack", 0.45),
                ("FrontShoulders", 0.15));
        }

        if (MatchesAny(tokens, "ab", "core", "crunch", "sit up", "sit-up", "leg raise", "plank", "twist", "rollout", "woodchopper"))
        {
            return WeightedRegions(
                ("FrontAbs", 1.0),
                ("BackLowerBack", 0.3));
        }

        if (MatchesAny(tokens, "leg extension"))
        {
            return WeightedRegions(("FrontQuads", 1.0));
        }

        if (MatchesAny(tokens, "lunge", "split squat", "step up", "step-up", "walking lunge"))
        {
            return WeightedRegions(
                ("FrontQuads", 0.9),
                ("BackGlutes", 0.55),
                ("BackHamstrings", 0.25));
        }

        if (MatchesAny(tokens, "squat", "leg press", "hack squat"))
        {
            return WeightedRegions(
                ("FrontQuads", 1.0),
                ("BackGlutes", 0.6),
                ("BackHamstrings", 0.25),
                ("BackLowerBack", 0.15));
        }

        return muscleGroup switch
        {
            "biceps" => WeightedRegions(("FrontBiceps", 1.0)),
            "triceps" => WeightedRegions(("FrontTriceps", 0.8), ("BackTriceps", 1.0)),
            "arms" => WeightedRegions(("FrontBiceps", 0.65), ("FrontTriceps", 0.6), ("BackTriceps", 0.6)),
            "shoulders" => WeightedRegions(("FrontShoulders", 1.0), ("BackShoulders", 0.45)),
            "rear delts" => WeightedRegions(("BackShoulders", 1.0)),
            "back" => WeightedRegions(("BackLats", 1.0), ("BackShoulders", 0.35), ("BackLowerBack", 0.3)),
            "lats" => WeightedRegions(("BackLats", 1.0), ("FrontBiceps", 0.25)),
            "lower back" => WeightedRegions(("BackLowerBack", 1.0), ("BackGlutes", 0.25)),
            "chest" => WeightedRegions(("FrontChest", 1.0), ("FrontTriceps", 0.25)),
            "abs" => WeightedRegions(("FrontAbs", 1.0), ("BackLowerBack", 0.25)),
            "core" => WeightedRegions(("FrontAbs", 0.9), ("BackLowerBack", 0.45)),
            "legs" => WeightedRegions(("FrontQuads", 0.85), ("BackGlutes", 0.6), ("BackHamstrings", 0.45)),
            "quads" => WeightedRegions(("FrontQuads", 1.0)),
            "glutes" => WeightedRegions(("BackGlutes", 1.0), ("BackHamstrings", 0.2)),
            "hamstrings" => WeightedRegions(("BackHamstrings", 1.0), ("BackGlutes", 0.2)),
            "calves" => WeightedRegions(("BackCalves", 1.0)),
            _ => WeightedRegions(("FrontChest", 1.0))
        };
    }

    private static string GetSearchTokens(Workout workout)
    {
        return $"{workout.MuscleGroup} {workout.Name}".Trim().ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, double> WeightedRegions(params (string Region, double Weight)[] contributions)
    {
        return contributions
            .Where(contribution => contribution.Weight > 0)
            .GroupBy(contribution => contribution.Region, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Max(contribution => contribution.Weight),
                StringComparer.OrdinalIgnoreCase);
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

        // Around 60 means a region needs more than a single hard bodyweight lift
        // session to hit the top of the heat scale.
        var intensity = Math.Clamp(volume / HighHeatVolumeThreshold, 0.0, 1.0);
        var easedIntensity = Math.Pow(intensity, 1.35);
        return MinimumHeatOpacity + (easedIntensity * (MaximumHeatOpacity - MinimumHeatOpacity));
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

    private static async Task EnsureHeatMapLookupLoadedAsync()
    {
        if (_exerciseHeatMapLookup != null)
        {
            return;
        }

        using Stream stream = await FileSystem.OpenAppPackageFileAsync("exercise_heat_map_weights.json");
        var definitions = await JsonSerializer.DeserializeAsync<List<ExerciseHeatMapDefinition>>(stream)
            ?? [];

        _exerciseHeatMapLookup = definitions
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Name))
            .ToDictionary(definition => definition.Name.Trim(), definition => definition, StringComparer.OrdinalIgnoreCase);
    }
}
