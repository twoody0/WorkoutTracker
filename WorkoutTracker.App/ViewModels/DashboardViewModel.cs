using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;
    private readonly IAuthService _authService;
    private readonly IBodyWeightService _bodyWeightService;
    private readonly IThemeService _themeService;
    private readonly IWorkoutScheduleService _scheduleService;

    private ObservableCollection<Workout> _workouts = new();
    private DateTime _selectedDate;
    private double _totalWeightLifted;
    private double _caloriesBurned;
    private bool _hasWeightlifting;
    private bool _hasCardio;
    private int _totalWorkoutSessions;
    private int _strengthWorkoutSessions;
    private int _cardioWorkoutSessions;
    private double _topBenchPatternOneRepMax;
    private double _topSquatPatternOneRepMax;
    private double _topDeadliftPatternOneRepMax;
    private double _topPullPatternOneRepMax;
    private double _lifetimeStrengthVolume;
    private string _biggestLiftSummary = "Biggest lift: log a few strength sessions first.";
    private string _favoriteTrainingDaySummary = "Most active day: not enough history yet.";

    public DashboardViewModel(
        IWorkoutService workoutService,
        IAuthService authService,
        IBodyWeightService bodyWeightService,
        IThemeService themeService,
        IWorkoutScheduleService scheduleService)
    {
        _workoutService = workoutService;
        _authService = authService;
        _bodyWeightService = bodyWeightService;
        _themeService = themeService;
        _scheduleService = scheduleService;

        LoadWorkoutsCommand = new Command(async () => await LoadWorkoutsAsync());
        ToggleThemeCommand = new Command(() =>
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeLabel));
            OnPropertyChanged(nameof(ThemeButtonText));
        });

        SelectedDate = DateTime.Today;
    }

    public ICommand LoadWorkoutsCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public ObservableCollection<Workout> Workouts
    {
        get => _workouts;
        set => SetProperty(ref _workouts, value);
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                OnPropertyChanged(nameof(SelectedDateSummary));
                LoadWorkoutsCommand.Execute(null);
            }
        }
    }

    public double TotalWeightLifted
    {
        get => _totalWeightLifted;
        set => SetProperty(ref _totalWeightLifted, value);
    }

    public double CaloriesBurned
    {
        get => _caloriesBurned;
        set => SetProperty(ref _caloriesBurned, value);
    }

    public bool HasWeightlifting
    {
        get => _hasWeightlifting;
        set => SetProperty(ref _hasWeightlifting, value);
    }

    public bool HasCardio
    {
        get => _hasCardio;
        set => SetProperty(ref _hasCardio, value);
    }

    public int TotalWorkoutSessions
    {
        get => _totalWorkoutSessions;
        set => SetProperty(ref _totalWorkoutSessions, value);
    }

    public int StrengthWorkoutSessions
    {
        get => _strengthWorkoutSessions;
        set => SetProperty(ref _strengthWorkoutSessions, value);
    }

    public int CardioWorkoutSessions
    {
        get => _cardioWorkoutSessions;
        set => SetProperty(ref _cardioWorkoutSessions, value);
    }

    public bool ShowStrengthStats => StrengthWorkoutSessions > 0;
    public bool ShowEmptyStrengthStats => !ShowStrengthStats;
    public string StrengthStatsSubtitle => "Estimated 1RMs update from your logged weight and reps.";
    public string EmptyStrengthStatsMessage => "Log a few strength workouts and your estimated 1RM stats will show up here.";
    public string TopBenchPatternSummary => FormatWeightStat(_topBenchPatternOneRepMax);
    public string TopSquatPatternSummary => FormatWeightStat(_topSquatPatternOneRepMax);
    public string TopDeadliftPatternSummary => FormatWeightStat(_topDeadliftPatternOneRepMax);
    public string TopPullPatternSummary => FormatWeightStat(_topPullPatternOneRepMax);
    public string LifetimeStrengthVolumeSummary => _lifetimeStrengthVolume > 0
        ? $"{_lifetimeStrengthVolume:N0} lb lifetime strength volume"
        : "No lifetime strength volume yet.";
    public string BiggestLiftSummary => _biggestLiftSummary;
    public string FavoriteTrainingDaySummary => _favoriteTrainingDaySummary;

    public bool HasWorkoutsForSelectedDate => Workouts.Count > 0;
    public bool ShowEmptyWorkoutState => !HasWorkoutsForSelectedDate;

    public bool HasBodyWeight => _bodyWeightService.HasBodyWeight();

    public string BodyWeightSummary => HasBodyWeight
        ? $"Weight: {_bodyWeightService.GetBodyWeight():N0} lb"
        : "Weight not set yet";

    public string BodyWeightInputValue => _bodyWeightService.GetBodyWeight()?.ToString("0.#") ?? string.Empty;

    public string BodyWeightButtonText => HasBodyWeight ? "Edit Weight" : "Set Weight";

    public bool IsDarkTheme => _themeService.IsDarkTheme;

    public string ThemeLabel => IsDarkTheme ? "Dark mode on" : "Light mode on";

    public string ThemeButtonText => IsDarkTheme ? "Use Light Mode" : "Use Dark Mode";

    public string SelectedDateSummary => SelectedDate.Date == DateTime.Today
        ? "Showing today’s workout history."
        : $"Showing workouts from {SelectedDate:dddd, MMM d}.";

    public string EmptyWorkoutMessage => "No workouts were logged for this date yet. Use this page to review past training as your history grows.";

    public string TotalWorkoutSessionsSummary => $"{TotalWorkoutSessions} total logged session{(TotalWorkoutSessions == 1 ? string.Empty : "s")}";

    public string StrengthWorkoutSessionsSummary => $"{StrengthWorkoutSessions} strength";

    public string CardioWorkoutSessionsSummary => $"{CardioWorkoutSessions} cardio";

    public string ActivePlanSummary => _scheduleService.ActivePlan == null
        ? "No active plan right now."
        : $"Active plan: {_scheduleService.ActivePlan.Name}";

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
        await LoadWorkoutsAsync();
        return true;
    }

    private async Task LoadWorkoutsAsync()
    {
        var allWorkouts = (await _workoutService.GetWorkouts()).ToList();
        var filtered = allWorkouts
            .Where(workout => workout.StartTime.Date == SelectedDate.Date)
            .OrderBy(workout => workout.StartTime)
            .ToList();

        TotalWorkoutSessions = allWorkouts.Count;
        StrengthWorkoutSessions = allWorkouts.Count(workout => workout.Type == WorkoutType.WeightLifting);
        CardioWorkoutSessions = allWorkouts.Count(workout => workout.Type == WorkoutType.Cardio);

        var strengthHistory = allWorkouts
            .Where(workout => workout.Type == WorkoutType.WeightLifting)
            .ToList();

        _topBenchPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "bench press", "close-grip bench press");
        _topSquatPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "squat");
        _topDeadliftPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "deadlift");
        _topPullPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "pull-up", "pull up", "weighted pull-up", "weighted pull up");
        _lifetimeStrengthVolume = strengthHistory.Sum(workout => workout.TrainingVolume);
        _biggestLiftSummary = GetBiggestLiftSummary(strengthHistory);
        _favoriteTrainingDaySummary = GetFavoriteTrainingDaySummary(allWorkouts);

        HasWeightlifting = filtered.Any(workout =>
            workout.Type == WorkoutType.WeightLifting && workout.Reps > 0 && workout.Sets > 0);
        HasCardio = filtered.Any(workout =>
            workout.Type == WorkoutType.Cardio && (workout.DurationMinutes > 0 || workout.DistanceMiles > 0 || workout.Steps > 0));

        Workouts.Clear();
        double total = 0;

        foreach (var workout in filtered)
        {
            Workouts.Add(workout);
            if (workout.Type == WorkoutType.WeightLifting && workout.Reps > 0 && workout.Sets > 0)
            {
                total += workout.Weight * workout.Reps * workout.Sets;
            }
        }

        TotalWeightLifted = total;

        var totalCardioMinutes = filtered
            .Where(workout => workout.Type == WorkoutType.Cardio)
            .Sum(GetEstimatedCardioMinutes);

        var weightLbs = _bodyWeightService.GetBodyWeight() ?? _authService.CurrentUser?.Weight ?? 154;
        var weightKg = weightLbs * 0.453592;
        const double moderateCardioMet = 6.0;
        CaloriesBurned = totalCardioMinutes * 0.0175 * moderateCardioMet * weightKg;

        OnPropertyChanged(nameof(HasWorkoutsForSelectedDate));
        OnPropertyChanged(nameof(ShowEmptyWorkoutState));
        OnPropertyChanged(nameof(BodyWeightSummary));
        OnPropertyChanged(nameof(BodyWeightInputValue));
        OnPropertyChanged(nameof(BodyWeightButtonText));
        OnPropertyChanged(nameof(TotalWorkoutSessionsSummary));
        OnPropertyChanged(nameof(StrengthWorkoutSessionsSummary));
        OnPropertyChanged(nameof(CardioWorkoutSessionsSummary));
        OnPropertyChanged(nameof(ActivePlanSummary));
        OnPropertyChanged(nameof(ShowStrengthStats));
        OnPropertyChanged(nameof(ShowEmptyStrengthStats));
        OnPropertyChanged(nameof(StrengthStatsSubtitle));
        OnPropertyChanged(nameof(TopBenchPatternSummary));
        OnPropertyChanged(nameof(TopSquatPatternSummary));
        OnPropertyChanged(nameof(TopDeadliftPatternSummary));
        OnPropertyChanged(nameof(TopPullPatternSummary));
        OnPropertyChanged(nameof(LifetimeStrengthVolumeSummary));
        OnPropertyChanged(nameof(BiggestLiftSummary));
        OnPropertyChanged(nameof(FavoriteTrainingDaySummary));
    }

    private static int GetEstimatedCardioMinutes(Workout workout)
    {
        if (workout.DurationMinutes > 0)
        {
            return workout.DurationMinutes;
        }

        if (workout.DistanceMiles > 0)
        {
            return (int)Math.Round(workout.DistanceMiles * 12);
        }

        if (workout.Steps > 0)
        {
            return Math.Max(1, workout.Steps / 100);
        }

        return 0;
    }

    private static string FormatWeightStat(double value)
    {
        return value > 0 ? $"{value:N0} lb" : "--";
    }

    private static double GetBestEstimatedOneRepMax(IEnumerable<Workout> workouts, params string[] keywords)
    {
        return workouts
            .Where(workout => workout.HasEstimatedOneRepMax &&
                keywords.Any(keyword => workout.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(workout => workout.EstimatedOneRepMax)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static string GetBiggestLiftSummary(IEnumerable<Workout> workouts)
    {
        var bestWorkout = workouts
            .Where(workout => workout.HasEstimatedOneRepMax)
            .OrderByDescending(workout => workout.EstimatedOneRepMax)
            .FirstOrDefault();

        return bestWorkout == null
            ? "Biggest lift: log a few strength sessions first."
            : $"Biggest lift: {bestWorkout.Name} at {bestWorkout.EstimatedOneRepMax:N0} lb est. 1RM";
    }

    private static string GetFavoriteTrainingDaySummary(IEnumerable<Workout> workouts)
    {
        var favoriteDay = workouts
            .GroupBy(workout => workout.StartTime.DayOfWeek)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault();

        return favoriteDay == null
            ? "Most active day: not enough history yet."
            : $"Most active day: {favoriteDay.Key} ({favoriteDay.Count()} session{(favoriteDay.Count() == 1 ? string.Empty : "s")})";
    }
}
