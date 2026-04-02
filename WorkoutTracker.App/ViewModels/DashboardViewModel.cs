using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.ViewModels;

public sealed class DashboardWorkoutHistoryItem
{
    public required Workout Workout { get; init; }
    public required string CaloriesSummary { get; init; }
    public bool HasCalories => !string.IsNullOrWhiteSpace(CaloriesSummary);
    public string Name => Workout.Name;
    public DateTime StartTime => Workout.StartTime;
    public string MuscleGroup => Workout.MuscleGroup;
    public int Sets => Workout.Sets;
    public string RepDisplay => Workout.RepDisplay;
    public double Weight => Workout.Weight;
    public int DurationMinutes => Workout.DurationMinutes;
    public string DurationValueDisplay => Workout.DurationValueDisplay;
    public double DistanceMiles => Workout.DistanceMiles;
    public int Steps => Workout.Steps;
    public WorkoutType Type => Workout.Type;
    public bool HasRepTarget => Workout.HasRepTarget;
    public bool HasWeightTarget => Workout.HasWeightTarget;
    public bool HasDuration => Workout.HasDuration;
    public bool HasDistance => Workout.HasDistance;
    public bool HasSteps => Workout.HasSteps;
}

public sealed class DashboardCalendarDayItem : BaseViewModel
{
    public required DateTime Date { get; init; }
    public required bool IsCurrentMonth { get; init; }
    private bool _isSelected;
    private bool _isDarkTheme;
    private bool _hasWorkout;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                RaiseAppearanceChanged();
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                RaiseAppearanceChanged();
            }
        }
    }

    public bool HasWorkout
    {
        get => _hasWorkout;
        set
        {
            if (SetProperty(ref _hasWorkout, value))
            {
                RaiseAppearanceChanged();
            }
        }
    }

    public bool IsToday => Date.Date == DateTime.Today;
    public string DayNumberText => Date.Day.ToString(CultureInfo.InvariantCulture);
    public Color BackgroundColor => IsSelected
        ? Color.FromArgb("#0F766E")
        : HasWorkout
            ? Color.FromArgb(IsDarkTheme
                ? (IsCurrentMonth ? "#134E4A" : "#1F2937")
                : (IsCurrentMonth ? "#CCFBF1" : "#E2E8F0"))
            : Colors.Transparent;
    public Color BorderColor => IsSelected
        ? Color.FromArgb("#0F766E")
        : HasWorkout
            ? Color.FromArgb("#0F766E")
            : IsToday
                ? Color.FromArgb(IsDarkTheme ? "#CBD5E1" : "#94A3B8")
                : Color.FromArgb(IsDarkTheme
                    ? (IsCurrentMonth ? "#475569" : "#334155")
                    : (IsCurrentMonth ? "#D8E0EB" : "#E5E7EB"));
    public Color TextColor => IsSelected
        ? Colors.White
        : HasWorkout
            ? Color.FromArgb(IsDarkTheme ? "#5EEAD4" : "#0F766E")
            : IsCurrentMonth
                ? Color.FromArgb(IsDarkTheme ? "#F8FAFC" : "#18212B")
                : Color.FromArgb(IsDarkTheme ? "#94A3B8" : "#8D9BAC");
    public Color WorkoutDotColor => IsSelected
        ? Colors.White
        : Color.FromArgb("#0F766E");
    public FontAttributes DayFontAttributes => IsSelected || IsToday || HasWorkout
        ? FontAttributes.Bold
        : FontAttributes.None;

    private void RaiseAppearanceChanged()
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(WorkoutDotColor));
        OnPropertyChanged(nameof(DayFontAttributes));
    }
}

public sealed class DashboardDateSnapshot
{
    public required List<DashboardWorkoutHistoryItem> Workouts { get; init; }
    public required int WorkoutCount { get; init; }
    public required bool HasWeightlifting { get; init; }
    public required bool HasCardio { get; init; }
    public required double TotalWeightLifted { get; init; }
    public required double CaloriesBurned { get; init; }
    public WorkoutType? FirstWorkoutType { get; init; }
}

public class DashboardViewModel : BaseViewModel
{
    private enum PlanStatsMode
    {
        None,
        Strength,
        Cardio
    }

    private readonly IWorkoutService _workoutService;
    private readonly IAuthService _authService;
    private readonly IBodyWeightService _bodyWeightService;
    private readonly IThemeService _themeService;
    private readonly IWorkoutScheduleService _scheduleService;
    private readonly ObservableCollection<DashboardCalendarDayItem> _calendarDays = new();

    private ObservableCollection<DashboardWorkoutHistoryItem> _workouts = new();
    private DateTime _selectedDate;
    private DateTime _visibleCalendarMonth;
    private double _totalWeightLifted;
    private double _caloriesBurned;
    private bool _hasWeightlifting;
    private bool _hasCardio;
    private WorkoutType? _firstWorkoutTypeForSelectedDate;
    private int _currentWorkoutIndex;
    private int _totalWorkoutsForSelectedDate;
    private int _totalWorkoutSessions;
    private int _strengthWorkoutSessions;
    private int _cardioWorkoutSessions;
    private double _topBenchPatternOneRepMax;
    private double _topSquatPatternOneRepMax;
    private double _topDeadliftPatternOneRepMax;
    private double _topPullPatternOneRepMax;
    private double _lifetimeStrengthVolume;
    private int _lifetimeCardioMinutes;
    private double _lifetimeCardioDistance;
    private int _longestCardioSessionMinutes;
    private double _longestCardioDistance;
    private string _biggestLiftSummary = "Biggest lift: log a few strength sessions first.";
    private string _favoriteTrainingDaySummary = "Most active day: not enough history yet.";
    private string _favoriteCardioWorkoutSummary = "Favorite cardio session: not enough history yet.";
    private PlanStatsMode _activePlanStatsMode;
    private HashSet<DateTime> _workoutDates = new();
    private bool _isCalendarExpanded;
    private List<Workout> _allWorkouts = [];
    private bool _hasLoadedWorkoutHistory;
    private Dictionary<DateTime, DashboardDateSnapshot> _workoutSnapshotsByDate = [];
    private DateTime _calendarGridStartDate;

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
        ShowPreviousWorkoutCommand = new Command(ShowPreviousWorkout);
        ShowNextWorkoutCommand = new Command(ShowNextWorkout);
        ShowPreviousMonthCommand = new Command(() => ChangeCalendarMonth(-1));
        ShowNextMonthCommand = new Command(() => ChangeCalendarMonth(1));
        SelectCalendarDateCommand = new Command<DateTime>(SelectCalendarDate);
        ToggleCalendarVisibilityCommand = new Command(ToggleCalendarVisibility);
        CloseCalendarVisibilityCommand = new Command(CloseCalendarVisibility);
        ShowStrengthStatsCommand = new Command(() => SetActiveStatsMode(PlanStatsMode.Strength));
        ShowCardioStatsCommand = new Command(() => SetActiveStatsMode(PlanStatsMode.Cardio));
        ToggleThemeCommand = new Command(() =>
        {
            _themeService.ToggleTheme();
            UpdateCalendarDays();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeLabel));
            OnPropertyChanged(nameof(ThemeButtonText));
        });

        _visibleCalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;
    }

    public ICommand LoadWorkoutsCommand { get; }
    public ICommand ShowPreviousWorkoutCommand { get; }
    public ICommand ShowNextWorkoutCommand { get; }
    public ICommand ShowPreviousMonthCommand { get; }
    public ICommand ShowNextMonthCommand { get; }
    public ICommand SelectCalendarDateCommand { get; }
    public ICommand ToggleCalendarVisibilityCommand { get; }
    public ICommand CloseCalendarVisibilityCommand { get; }
    public ICommand ShowStrengthStatsCommand { get; }
    public ICommand ShowCardioStatsCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public ObservableCollection<DashboardWorkoutHistoryItem> Workouts
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
                if (_visibleCalendarMonth.Year != value.Year || _visibleCalendarMonth.Month != value.Month)
                {
                    _visibleCalendarMonth = new DateTime(value.Year, value.Month, 1);
                    OnPropertyChanged(nameof(CurrentCalendarMonthLabel));
                }

                OnPropertyChanged(nameof(SelectedDateSummary));
                UpdateCalendarDays();
                if (_hasLoadedWorkoutHistory)
                {
                    ApplySelectedDateWorkouts();
                }
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

    public int TotalWorkoutsForSelectedDate
    {
        get => _totalWorkoutsForSelectedDate;
        set => SetProperty(ref _totalWorkoutsForSelectedDate, value);
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

    public bool ShowPlanStats => TotalWorkoutSessions > 0 && _activePlanStatsMode != PlanStatsMode.None;
    public bool ShowStatsToggle => TotalWorkoutSessions > 0 && (StrengthWorkoutSessions > 0 || CardioWorkoutSessions > 0);
    public bool ShowStrengthStatsSection => _activePlanStatsMode == PlanStatsMode.Strength;
    public bool ShowCardioStatsSection => _activePlanStatsMode == PlanStatsMode.Cardio;
    public bool IsStrengthStatsSelected => _activePlanStatsMode == PlanStatsMode.Strength;
    public bool IsCardioStatsSelected => _activePlanStatsMode == PlanStatsMode.Cardio;
    public bool ShowStrengthStats => ShowStrengthStatsSection && StrengthWorkoutSessions > 0;
    public bool ShowEmptyStrengthStats => ShowStrengthStatsSection && !ShowStrengthStats;
    public bool ShowCardioStats => ShowCardioStatsSection && CardioWorkoutSessions > 0;
    public bool ShowEmptyCardioStats => ShowCardioStatsSection && !ShowCardioStats;
    public bool ShowTrainingSnapshot => true;
    public bool ShowStrengthDaySummaryCard => HasWeightlifting;
    public bool ShowCardioDaySummaryCard => HasCardio;
    public bool ShowStrengthDaySummaryOnLeft => ShowStrengthDaySummaryCard && (!HasCardio || _firstWorkoutTypeForSelectedDate != WorkoutType.Cardio);
    public bool ShowStrengthDaySummaryOnRight => ShowStrengthDaySummaryCard && HasCardio && _firstWorkoutTypeForSelectedDate == WorkoutType.Cardio;
    public bool ShowCardioDaySummaryOnLeft => ShowCardioDaySummaryCard && (!HasWeightlifting || _firstWorkoutTypeForSelectedDate == WorkoutType.Cardio);
    public bool ShowCardioDaySummaryOnRight => ShowCardioDaySummaryCard && HasWeightlifting && _firstWorkoutTypeForSelectedDate != WorkoutType.Cardio;
    public string StrengthStatsSubtitle => "Estimated 1RMs update from your logged weight and reps.";
    public string EmptyStrengthStatsMessage => "Log a few strength workouts to see these stats.";
    public string TopBenchPatternSummary => FormatWeightStat(_topBenchPatternOneRepMax);
    public string TopSquatPatternSummary => FormatWeightStat(_topSquatPatternOneRepMax);
    public string TopDeadliftPatternSummary => FormatWeightStat(_topDeadliftPatternOneRepMax);
    public string TopPullPatternSummary => FormatWeightStat(_topPullPatternOneRepMax);
    public string LifetimeStrengthVolumeSummary => _lifetimeStrengthVolume > 0
        ? $"{_lifetimeStrengthVolume:N0} lb lifetime strength volume"
        : "No lifetime strength volume yet.";
    public string BiggestLiftSummary => _biggestLiftSummary;
    public string FavoriteTrainingDaySummary => _favoriteTrainingDaySummary;
    public string CardioStatsSubtitle => "Cardio stats update from your logged time and distance.";
    public string EmptyCardioStatsMessage => "Log a few cardio sessions to see these stats.";
    public string LifetimeCardioMinutesSummary => _lifetimeCardioMinutes > 0
        ? $"{_lifetimeCardioMinutes:N0} total cardio minutes"
        : "No cardio minutes yet.";
    public string LifetimeCardioDistanceSummary => _lifetimeCardioDistance > 0
        ? $"{_lifetimeCardioDistance:0.##} total cardio miles"
        : "No cardio distance yet.";
    public string LongestCardioSessionSummary => _longestCardioSessionMinutes > 0
        ? $"{_longestCardioSessionMinutes} min longest session"
        : "--";
    public string LongestCardioDistanceSummary => _longestCardioDistance > 0
        ? $"{_longestCardioDistance:0.##} mi longest distance"
        : "--";
    public string FavoriteCardioWorkoutSummary => _favoriteCardioWorkoutSummary;

    public bool HasWorkoutsForSelectedDate => Workouts.Count > 0;
    public bool ShowEmptyWorkoutState => !HasWorkoutsForSelectedDate;
    public DashboardWorkoutHistoryItem? CurrentWorkout => HasWorkoutsForSelectedDate && _currentWorkoutIndex >= 0 && _currentWorkoutIndex < Workouts.Count
        ? Workouts[_currentWorkoutIndex]
        : null;
    public bool CanShowPreviousWorkout => _currentWorkoutIndex > 0;
    public bool CanShowNextWorkout => _currentWorkoutIndex >= 0 && _currentWorkoutIndex < Workouts.Count - 1;

    public bool HasBodyWeight => _bodyWeightService.HasBodyWeight();

    public string BodyWeightSummary => HasBodyWeight
        ? $"Weight: {_bodyWeightService.GetBodyWeight():N0} lb"
        : "Weight not set yet";

    public string BodyWeightInputValue => _bodyWeightService.GetBodyWeight()?.ToString("0.#") ?? string.Empty;

    public string BodyWeightButtonText => HasBodyWeight ? "Edit Weight" : "Set Weight";

    public bool IsDarkTheme => _themeService.IsDarkTheme;

    public string ThemeLabel => IsDarkTheme ? "Dark mode on" : "Light mode on";

    public string ThemeButtonText => IsDarkTheme ? "Use Light Mode" : "Use Dark Mode";

    public ObservableCollection<DashboardCalendarDayItem> CalendarDays => _calendarDays;

    public string CurrentCalendarMonthLabel => _visibleCalendarMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public bool IsCalendarExpanded
    {
        get => _isCalendarExpanded;
        set
        {
            if (SetProperty(ref _isCalendarExpanded, value))
            {
                OnPropertyChanged(nameof(CalendarToggleText));
            }
        }
    }

    public string CalendarToggleText => IsCalendarExpanded ? "Hide Calendar" : "Open Calendar";

    public string SelectedDateSummary => SelectedDate.Date == DateTime.Today
        ? "Showing today's workout history."
        : $"Showing workouts from {SelectedDate:dddd, MMM d}.";

    public string EmptyWorkoutMessage => "No workouts were logged for this date yet. Use this page to review past training as your history grows.";

    public string WorkoutHistorySummary => TotalWorkoutsForSelectedDate switch
    {
        <= 0 => "No workouts logged for this date yet.",
        _ => $"Showing {TotalWorkoutsForSelectedDate} workout{(TotalWorkoutsForSelectedDate == 1 ? string.Empty : "s")} for this date."
    };

    public string CurrentWorkoutPositionSummary => CurrentWorkout == null
        ? string.Empty
        : $"Workout {_currentWorkoutIndex + 1} of {Workouts.Count}";

    public string TotalWorkoutSessionsSummary => $"{TotalWorkoutSessions} total logged session{(TotalWorkoutSessions == 1 ? string.Empty : "s")}";

    public string StrengthWorkoutSessionsSummary => $"{StrengthWorkoutSessions} strength";

    public string CardioWorkoutSessionsSummary => $"{CardioWorkoutSessions} cardio";

    public string ActivePlanSummary => _scheduleService.ActivePlan == null
        ? "No active plan right now."
        : $"Active plan: {_scheduleService.ActivePlan.Name}";

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
        await LoadWorkoutsAsync();
        return true;
    }

    private async Task LoadWorkoutsAsync()
    {
        _allWorkouts = (await _workoutService.GetWorkouts()).ToList();
        _hasLoadedWorkoutHistory = true;
        _workoutDates = _allWorkouts
            .Select(workout => workout.StartTime.Date)
            .ToHashSet();
        _workoutSnapshotsByDate = BuildWorkoutSnapshotsByDate(_allWorkouts);

        TotalWorkoutSessions = _allWorkouts.Count;
        StrengthWorkoutSessions = _allWorkouts.Count(workout => workout.Type == WorkoutType.WeightLifting);
        CardioWorkoutSessions = _allWorkouts.Count(workout => workout.Type == WorkoutType.Cardio);

        var strengthHistory = _allWorkouts
            .Where(workout => workout.Type == WorkoutType.WeightLifting)
            .ToList();
        var cardioHistory = _allWorkouts
            .Where(workout => workout.Type == WorkoutType.Cardio)
            .ToList();

        _topBenchPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "bench press", "close-grip bench press");
        _topSquatPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "squat");
        _topDeadliftPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "deadlift");
        _topPullPatternOneRepMax = GetBestEstimatedOneRepMax(strengthHistory, "pull-up", "pull up", "weighted pull-up", "weighted pull up");
        _lifetimeStrengthVolume = strengthHistory.Sum(workout => workout.TrainingVolume);
        _lifetimeCardioMinutes = cardioHistory.Sum(GetEstimatedCardioMinutes);
        _lifetimeCardioDistance = cardioHistory.Sum(workout => workout.DistanceMiles);
        _longestCardioSessionMinutes = cardioHistory.Select(GetEstimatedCardioMinutes).DefaultIfEmpty(0).Max();
        _longestCardioDistance = cardioHistory.Select(workout => workout.DistanceMiles).DefaultIfEmpty(0).Max();
        _biggestLiftSummary = GetBiggestLiftSummary(strengthHistory);
        _favoriteTrainingDaySummary = GetFavoriteTrainingDaySummary(_allWorkouts);
        _favoriteCardioWorkoutSummary = GetFavoriteCardioWorkoutSummary(cardioHistory);
        if (_activePlanStatsMode == PlanStatsMode.None)
        {
            _activePlanStatsMode = GetDefaultStatsMode();
        }

        ApplySelectedDateWorkouts();
        UpdateCalendarDays();

        OnPropertyChanged(nameof(BodyWeightSummary));
        OnPropertyChanged(nameof(BodyWeightInputValue));
        OnPropertyChanged(nameof(BodyWeightButtonText));
        OnPropertyChanged(nameof(TotalWorkoutSessionsSummary));
        OnPropertyChanged(nameof(StrengthWorkoutSessionsSummary));
        OnPropertyChanged(nameof(CardioWorkoutSessionsSummary));
        OnPropertyChanged(nameof(ActivePlanSummary));
        OnPropertyChanged(nameof(ShowPlanStats));
        OnPropertyChanged(nameof(ShowStatsToggle));
        OnPropertyChanged(nameof(ShowTrainingSnapshot));
        OnPropertyChanged(nameof(ShowStrengthStatsSection));
        OnPropertyChanged(nameof(ShowCardioStatsSection));
        OnPropertyChanged(nameof(IsStrengthStatsSelected));
        OnPropertyChanged(nameof(IsCardioStatsSelected));
        OnPropertyChanged(nameof(ShowStrengthStats));
        OnPropertyChanged(nameof(ShowEmptyStrengthStats));
        OnPropertyChanged(nameof(ShowCardioStats));
        OnPropertyChanged(nameof(ShowEmptyCardioStats));
        OnPropertyChanged(nameof(ShowStrengthDaySummaryCard));
        OnPropertyChanged(nameof(ShowCardioDaySummaryCard));
        OnPropertyChanged(nameof(StrengthStatsSubtitle));
        OnPropertyChanged(nameof(EmptyStrengthStatsMessage));
        OnPropertyChanged(nameof(TopBenchPatternSummary));
        OnPropertyChanged(nameof(TopSquatPatternSummary));
        OnPropertyChanged(nameof(TopDeadliftPatternSummary));
        OnPropertyChanged(nameof(TopPullPatternSummary));
        OnPropertyChanged(nameof(LifetimeStrengthVolumeSummary));
        OnPropertyChanged(nameof(BiggestLiftSummary));
        OnPropertyChanged(nameof(FavoriteTrainingDaySummary));
        OnPropertyChanged(nameof(CardioStatsSubtitle));
        OnPropertyChanged(nameof(EmptyCardioStatsMessage));
        OnPropertyChanged(nameof(LifetimeCardioMinutesSummary));
        OnPropertyChanged(nameof(LifetimeCardioDistanceSummary));
        OnPropertyChanged(nameof(LongestCardioSessionSummary));
        OnPropertyChanged(nameof(LongestCardioDistanceSummary));
        OnPropertyChanged(nameof(FavoriteCardioWorkoutSummary));
    }

    private void ApplySelectedDateWorkouts()
    {
        var snapshot = _workoutSnapshotsByDate.GetValueOrDefault(SelectedDate.Date, new DashboardDateSnapshot
        {
            Workouts = [],
            WorkoutCount = 0,
            HasWeightlifting = false,
            HasCardio = false,
            TotalWeightLifted = 0,
            CaloriesBurned = 0
        });

        TotalWorkoutsForSelectedDate = snapshot.WorkoutCount;
        HasWeightlifting = snapshot.HasWeightlifting;
        HasCardio = snapshot.HasCardio;
        _firstWorkoutTypeForSelectedDate = snapshot.FirstWorkoutType;

        Workouts.Clear();
        _currentWorkoutIndex = 0;

        foreach (var workout in snapshot.Workouts)
        {
            Workouts.Add(workout);
        }

        TotalWeightLifted = snapshot.TotalWeightLifted;
        CaloriesBurned = snapshot.CaloriesBurned;

        OnPropertyChanged(nameof(HasWorkoutsForSelectedDate));
        OnPropertyChanged(nameof(ShowEmptyWorkoutState));
        OnPropertyChanged(nameof(WorkoutHistorySummary));
        OnPropertyChanged(nameof(CurrentWorkout));
        OnPropertyChanged(nameof(CurrentWorkoutPositionSummary));
        OnPropertyChanged(nameof(CanShowPreviousWorkout));
        OnPropertyChanged(nameof(CanShowNextWorkout));
        OnPropertyChanged(nameof(ShowStrengthDaySummaryCard));
        OnPropertyChanged(nameof(ShowCardioDaySummaryCard));
        OnPropertyChanged(nameof(ShowStrengthDaySummaryOnLeft));
        OnPropertyChanged(nameof(ShowStrengthDaySummaryOnRight));
        OnPropertyChanged(nameof(ShowCardioDaySummaryOnLeft));
        OnPropertyChanged(nameof(ShowCardioDaySummaryOnRight));
    }

    private Dictionary<DateTime, DashboardDateSnapshot> BuildWorkoutSnapshotsByDate(IEnumerable<Workout> workouts)
    {
        var weightLbs = _bodyWeightService.GetBodyWeight() ?? _authService.CurrentUser?.Weight ?? 154;
        var weightKg = weightLbs * 0.453592;
        const double moderateCardioMet = 6.0;

        return workouts
            .GroupBy(workout => workout.StartTime.Date)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var orderedWorkouts = group
                        .OrderByDescending(workout => workout.StartTime)
                        .ToList();
                    var firstWorkoutOfDay = group
                        .OrderBy(workout => workout.StartTime)
                        .FirstOrDefault();

                    var items = orderedWorkouts
                        .Select(workout => new DashboardWorkoutHistoryItem
                        {
                            Workout = workout,
                            CaloriesSummary = GetWorkoutCaloriesSummary(workout, weightLbs)
                        })
                        .ToList();

                    var totalWeightLifted = orderedWorkouts
                        .Where(workout => workout.Type == WorkoutType.WeightLifting && workout.Reps > 0 && workout.Sets > 0)
                        .Sum(workout => workout.Weight * workout.Reps * workout.Sets);

                    var totalCardioMinutes = orderedWorkouts
                        .Where(workout => workout.Type == WorkoutType.Cardio)
                        .Sum(GetEstimatedCardioMinutes);

                    return new DashboardDateSnapshot
                    {
                        Workouts = items,
                        WorkoutCount = items.Count,
                        HasWeightlifting = orderedWorkouts.Any(workout =>
                            workout.Type == WorkoutType.WeightLifting &&
                            workout.Sets > 0 &&
                            (workout.HasRepTarget || workout.HasTimedTarget)),
                        HasCardio = orderedWorkouts.Any(workout =>
                            workout.Type == WorkoutType.Cardio && (workout.DurationMinutes > 0 || workout.DistanceMiles > 0 || workout.Steps > 0)),
                        TotalWeightLifted = totalWeightLifted,
                        CaloriesBurned = totalCardioMinutes * 0.0175 * moderateCardioMet * weightKg,
                        FirstWorkoutType = firstWorkoutOfDay?.Type
                    };
                });
    }

    private void ChangeCalendarMonth(int offset)
    {
        _visibleCalendarMonth = _visibleCalendarMonth.AddMonths(offset);
        OnPropertyChanged(nameof(CurrentCalendarMonthLabel));
        UpdateCalendarDays();
    }

    private void SelectCalendarDate(DateTime date)
    {
        SelectedDate = date.Date;
    }

    public void SelectCalendarDay(DashboardCalendarDayItem? day)
    {
        if (day == null)
        {
            return;
        }

        SelectCalendarDate(day.Date);
    }

    private void ToggleCalendarVisibility()
    {
        IsCalendarExpanded = !IsCalendarExpanded;
    }

    private void CloseCalendarVisibility()
    {
        IsCalendarExpanded = false;
    }

    private void UpdateCalendarDays()
    {
        var firstDayOfMonth = new DateTime(_visibleCalendarMonth.Year, _visibleCalendarMonth.Month, 1);
        var gridStart = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
        var isDarkTheme = _themeService.IsDarkTheme;

        if (_calendarDays.Count == 42 && _calendarGridStartDate == gridStart)
        {
            foreach (var day in _calendarDays)
            {
                day.IsSelected = day.Date.Date == SelectedDate.Date;
                day.IsDarkTheme = isDarkTheme;
                day.HasWorkout = _workoutDates.Contains(day.Date.Date);
            }

            return;
        }

        _calendarGridStartDate = gridStart;
        _calendarDays.Clear();

        for (var offset = 0; offset < 42; offset++)
        {
            var date = gridStart.AddDays(offset);
            _calendarDays.Add(new DashboardCalendarDayItem
            {
                Date = date,
                IsCurrentMonth = date.Month == _visibleCalendarMonth.Month && date.Year == _visibleCalendarMonth.Year,
                IsSelected = date.Date == SelectedDate.Date,
                HasWorkout = _workoutDates.Contains(date.Date),
                IsDarkTheme = isDarkTheme
            });
        }
    }

    private void ShowPreviousWorkout()
    {
        if (!CanShowPreviousWorkout)
        {
            return;
        }

        _currentWorkoutIndex--;
        OnPropertyChanged(nameof(CurrentWorkout));
        OnPropertyChanged(nameof(CurrentWorkoutPositionSummary));
        OnPropertyChanged(nameof(CanShowPreviousWorkout));
        OnPropertyChanged(nameof(CanShowNextWorkout));
    }

    private void ShowNextWorkout()
    {
        if (!CanShowNextWorkout)
        {
            return;
        }

        _currentWorkoutIndex++;
        OnPropertyChanged(nameof(CurrentWorkout));
        OnPropertyChanged(nameof(CurrentWorkoutPositionSummary));
        OnPropertyChanged(nameof(CanShowPreviousWorkout));
        OnPropertyChanged(nameof(CanShowNextWorkout));
    }

    private PlanStatsMode GetDefaultStatsMode()
    {
        var planMode = GetActivePlanStatsMode();
        if (planMode != PlanStatsMode.None)
        {
            return planMode;
        }

        if (StrengthWorkoutSessions > 0)
        {
            return PlanStatsMode.Strength;
        }

        if (CardioWorkoutSessions > 0)
        {
            return PlanStatsMode.Cardio;
        }

        return PlanStatsMode.Strength;
    }

    private PlanStatsMode GetActivePlanStatsMode()
    {
        var activePlan = _scheduleService.ActivePlan;
        if (activePlan == null)
        {
            return PlanStatsMode.None;
        }

        var category = activePlan.Category?.Trim() ?? string.Empty;
        if (category.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Conditioning", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Cardio", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Fat Loss", StringComparison.OrdinalIgnoreCase))
        {
            return PlanStatsMode.Cardio;
        }

        var cardioCount = activePlan.Workouts.Count(workout => workout.Type == WorkoutType.Cardio);
        var strengthCount = activePlan.Workouts.Count(workout => workout.Type == WorkoutType.WeightLifting);

        if (cardioCount == 0 && strengthCount == 0)
        {
            return PlanStatsMode.None;
        }

        return cardioCount > strengthCount ? PlanStatsMode.Cardio : PlanStatsMode.Strength;
    }

    private void SetActiveStatsMode(PlanStatsMode mode)
    {
        if (_activePlanStatsMode == mode)
        {
            return;
        }

        _activePlanStatsMode = mode;
        OnPropertyChanged(nameof(ShowPlanStats));
        OnPropertyChanged(nameof(ShowStrengthStatsSection));
        OnPropertyChanged(nameof(ShowCardioStatsSection));
        OnPropertyChanged(nameof(IsStrengthStatsSelected));
        OnPropertyChanged(nameof(IsCardioStatsSelected));
        OnPropertyChanged(nameof(ShowStrengthStats));
        OnPropertyChanged(nameof(ShowEmptyStrengthStats));
        OnPropertyChanged(nameof(ShowCardioStats));
        OnPropertyChanged(nameof(ShowEmptyCardioStats));
        OnPropertyChanged(nameof(ShowStrengthDaySummaryCard));
        OnPropertyChanged(nameof(ShowCardioDaySummaryCard));
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

    private static string GetWorkoutCaloriesSummary(Workout workout, double bodyWeightLbs)
    {
        var bodyWeightKg = bodyWeightLbs * 0.453592;
        var estimatedMinutes = workout.Type == WorkoutType.Cardio
            ? GetEstimatedCardioMinutes(workout)
            : GetEstimatedStrengthMinutes(workout);

        if (estimatedMinutes <= 0)
        {
            return string.Empty;
        }

        var met = workout.Type == WorkoutType.Cardio ? 6.0 : 3.5;
        var calories = estimatedMinutes * 0.0175 * met * bodyWeightKg;
        return calories > 0 ? $"{Math.Round(calories):N0} kcal" : string.Empty;
    }

    private static int GetEstimatedStrengthMinutes(Workout workout)
    {
        if (workout.Type != WorkoutType.WeightLifting || workout.Sets <= 0)
        {
            return 0;
        }

        if (workout.HasTimedTarget)
        {
            return Math.Max(1, (int)Math.Ceiling((workout.TimedTargetSeconds * Math.Max(1, workout.Sets)) / 60d));
        }

        return Math.Max(5, workout.Sets * 3);
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

    private static string GetFavoriteCardioWorkoutSummary(IEnumerable<Workout> workouts)
    {
        var favoriteWorkout = workouts
            .GroupBy(workout => workout.Name)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault();

        return favoriteWorkout == null
            ? "Favorite cardio session: not enough history yet."
            : $"Favorite cardio session: {favoriteWorkout.Key} ({favoriteWorkout.Count()} time{(favoriteWorkout.Count() == 1 ? string.Empty : "s")})";
    }
}
