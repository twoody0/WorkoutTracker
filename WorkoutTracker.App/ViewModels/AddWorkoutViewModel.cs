using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

using WorkoutTracker.Helpers;
namespace WorkoutTracker.ViewModels;

public class AddWorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService? _scheduleService;
    private readonly IWorkoutLibraryService _workoutLibraryService;
    private readonly IWorkoutService _workoutService;
    private readonly ObservableCollection<Workout> _workouts;
    private readonly INavigation _navigation;
    private IReadOnlyList<Workout> _recommendedWorkoutSource;
    private IReadOnlyList<Workout> _workoutHistory = [];
    private string _recommendationSourceName;
    private readonly Action<Workout> _saveWorkoutAction;
    private string _name = string.Empty;
    private string _muscleGroup = string.Empty;
    private string _selectedMuscleGroup = string.Empty;
    private WorkoutType _selectedType;
    private int _sets;
    private int _reps;
    private int _steps;
    private int _durationMinutes;
    private int _durationSeconds;
    private double _distanceMiles;
    private string _distanceMilesText = string.Empty;
    private string _recommendedWorkoutSummary = "Build a custom workout for this day.";
    private string _lastRecommendationSignature = string.Empty;
    private RecommendedWorkoutOption? _selectedRecommendedWorkout;
    private bool _isApplyingLibrarySelection;
    private CancellationTokenSource? _exerciseSuggestionDebounceCts;
    private CancellationTokenSource? _timedStrengthCountdownCancellation;
    private int _exerciseSuggestionRequestVersion;
    private int _timedStrengthDefaultRequestVersion;
    private long _lastLoadedWorkoutChangeVersion = -1;
    private int _timedStrengthCountdownRemainingSeconds;
    private bool _isTimedStrengthCountdownRunning;
    private bool _hasTimedStrengthCountdownCompleted;
    private string _lastAppliedTimedExerciseKey = string.Empty;

    public DayOfWeek Day { get; }

    public string Name
    {
        get => _name;
        set
        {
            var sanitized = InputSanitizer.SanitizeName(value);
            if (!SetProperty(ref _name, sanitized))
            {
                return;
            }

            NotifyExerciseImageStateChanged();
            OnPropertyChanged(nameof(UsesTimedStrengthTarget));
            OnPropertyChanged(nameof(StrengthTargetLabel));
            OnPropertyChanged(nameof(StrengthTargetValue));
            HandleTimedStrengthExerciseChanged(applyDefaults: !_isApplyingLibrarySelection);
            if (!_isApplyingLibrarySelection)
            {
                _ = UpdateExerciseSuggestionsAsync();
            }
        }
    }

    public string MuscleGroup
    {
        get => _muscleGroup;
        set => SetProperty(ref _muscleGroup, InputSanitizer.SanitizeMuscleGroup(value));
    }

    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (SetProperty(ref _selectedMuscleGroup, value))
            {
                MuscleGroup = value;
                _ = UpdateExerciseSuggestionsAsync();
            }
        }
    }

    public WorkoutType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(IsWeightLifting));
                OnPropertyChanged(nameof(IsCardio));
                OnPropertyChanged(nameof(UsesTimedStrengthTarget));
                OnPropertyChanged(nameof(StrengthTargetLabel));
                OnPropertyChanged(nameof(StrengthTargetValue));
                NotifyExerciseImageStateChanged();
                HandleTimedStrengthExerciseChanged(applyDefaults: !_isApplyingLibrarySelection);
                _ = UpdateExerciseSuggestionsAsync();
            }
        }
    }

    public int Sets
    {
        get => _sets;
        set => SetProperty(ref _sets, Math.Clamp(value, 0, InputSanitizer.MaxSets));
    }

    public int Reps
    {
        get => _reps;
        set
        {
            if (SetProperty(ref _reps, Math.Clamp(value, 0, InputSanitizer.MaxReps)))
            {
                OnPropertyChanged(nameof(StrengthTargetValue));
            }
        }
    }

    public int Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, Math.Clamp(value, 0, InputSanitizer.MaxSteps));
    }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetProperty(ref _durationMinutes, Math.Clamp(value, 0, InputSanitizer.MaxDurationMinutes)))
            {
                OnPropertyChanged(nameof(StrengthTargetValue));
            }
        }
    }

    public int DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (SetProperty(ref _durationSeconds, Math.Clamp(value, 0, InputSanitizer.MaxTimedStrengthSeconds)))
            {
                OnPropertyChanged(nameof(StrengthTargetValue));
                if (UsesTimedStrengthTarget)
                {
                    _lastAppliedTimedExerciseKey = GetTimedExerciseKey();
                }

                SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: true);
            }
        }
    }

    public double DistanceMiles
    {
        get => _distanceMiles;
        set
        {
            var clamped = Math.Clamp(value, 0, InputSanitizer.MaxDistanceMiles);
            if (SetProperty(ref _distanceMiles, clamped))
            {
                var formatted = clamped > 0 ? clamped.ToString("0.##") : string.Empty;
                if (!string.Equals(_distanceMilesText, formatted, StringComparison.Ordinal))
                {
                    _distanceMilesText = formatted;
                    OnPropertyChanged(nameof(DistanceMilesText));
                }
            }
        }
    }

    public string DistanceMilesText
    {
        get => _distanceMilesText;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveDecimalText(value, InputSanitizer.MaxDistanceMiles, decimals: 2);
            if (!SetProperty(ref _distanceMilesText, sanitized))
            {
                return;
            }

            if (double.TryParse(sanitized, out var parsedDistance))
            {
                if (Math.Abs(_distanceMiles - parsedDistance) > 0.0001)
                {
                    _distanceMiles = parsedDistance;
                    OnPropertyChanged(nameof(DistanceMiles));
                }
            }
            else if (_distanceMiles != 0)
            {
                _distanceMiles = 0;
                OnPropertyChanged(nameof(DistanceMiles));
            }
        }
    }

    public List<WorkoutType> WorkoutTypes { get; } = Enum.GetValues(typeof(WorkoutType)).Cast<WorkoutType>().ToList();
    public List<string> MuscleGroups { get; } = ["Back", "Arms", "Biceps", "Cardio", "Chest", "Core", "Legs", "Shoulders", "Triceps"];
    public ObservableCollection<RecommendedWorkoutOption> RecommendedWorkouts { get; } = new();
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; } = new();

    public bool IsWeightLifting => SelectedType == WorkoutType.WeightLifting;
    public bool IsCardio => SelectedType == WorkoutType.Cardio;
    public bool UsesTimedStrengthTarget => IsWeightLifting && Workout.PrefersTimedTarget(Name);
    public string StrengthTargetLabel => UsesTimedStrengthTarget ? "Time (seconds)" : "Reps";
    public int StrengthTargetValue
    {
        get => UsesTimedStrengthTarget ? DurationSeconds : Reps;
        set
        {
            if (UsesTimedStrengthTarget)
            {
                DurationSeconds = value;
                return;
            }

            Reps = value;
        }
    }
    public bool HasRecommendedWorkouts => RecommendedWorkouts.Count > 0;
    public bool HasExerciseSuggestions => ExerciseSuggestions.Count > 0;
    public bool HasSelectedExerciseInfo => IsWeightLifting && ExerciseInfoCatalog.HasInfo(Name);
    public bool HasSelectedExerciseImage => IsWeightLifting && ExerciseImageCatalog.HasImage(Name);
    public string SelectedExerciseImageSource => ExerciseImageCatalog.GetImageSource(Name);
    public bool ShowTimedStrengthCountdown => UsesTimedStrengthTarget;
    public string TimedStrengthCountdownDisplay => FormatCountdownDisplay(GetEffectiveTimedStrengthCountdownSeconds());
    public string TimedStrengthStartButtonText => _isTimedStrengthCountdownRunning
        ? "Restart"
        : _hasTimedStrengthCountdownCompleted
            ? "Start Again"
            : "Start";
    public bool CanStartTimedStrengthCountdown => UsesTimedStrengthTarget && GetConfiguredTimedStrengthSeconds() > 0;
    public string ActivePlanName => _recommendationSourceName;
    public string RecommendedWorkoutSummary
    {
        get => _recommendedWorkoutSummary;
        private set => SetProperty(ref _recommendedWorkoutSummary, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand UseRecommendedWorkoutCommand { get; }
    public ICommand SelectExerciseSuggestionCommand { get; }
    public ICommand StartTimedStrengthCountdownCommand { get; }

    public AddWorkoutViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService, IWorkoutLibraryService workoutLibraryService, IWorkoutService workoutService, ObservableCollection<Workout> workouts, INavigation navigation)
        : this(
            day,
            workoutLibraryService,
            workoutService,
            workouts,
            navigation,
            scheduleService.GetActivePlanWorkoutsForDay(day),
            scheduleService.ActivePlan?.Name ?? string.Empty,
            workout => scheduleService.AddWorkoutToDay(day, workout))
    {
        _scheduleService = scheduleService;
    }

    public AddWorkoutViewModel(
        DayOfWeek day,
        IWorkoutLibraryService workoutLibraryService,
        IWorkoutService workoutService,
        ObservableCollection<Workout> workouts,
        INavigation navigation,
        IEnumerable<Workout>? recommendedWorkouts,
        string recommendationSourceName,
        Action<Workout> saveWorkoutAction)
    {
        Day = day;
        _workoutLibraryService = workoutLibraryService;
        _workoutService = workoutService;
        _workouts = workouts;
        _navigation = navigation;
        _recommendedWorkoutSource = recommendedWorkouts?.ToList() ?? [];
        _recommendationSourceName = recommendationSourceName;
        _lastRecommendationSignature = BuildRecommendationSignature(_recommendationSourceName, _recommendedWorkoutSource);
        _saveWorkoutAction = saveWorkoutAction;

        SelectedType = WorkoutType.WeightLifting; // Default
        SaveCommand = new Command(SaveWorkout);
        UseRecommendedWorkoutCommand = new Command<RecommendedWorkoutOption>(UseRecommendedWorkout);
        SelectExerciseSuggestionCommand = new Command<WeightliftingExercise>(SelectExerciseSuggestion);
        StartTimedStrengthCountdownCommand = new Command(StartTimedStrengthCountdown);

        LoadRecommendations();
    }

    public void RefreshRecommendations()
    {
        if (_scheduleService != null)
        {
            var refreshedSource = _scheduleService.GetActivePlanWorkoutsForDay(Day).ToList();
            var refreshedPlanName = _scheduleService.ActivePlan?.Name ?? string.Empty;
            var refreshedSignature = BuildRecommendationSignature(refreshedPlanName, refreshedSource);

            if (string.Equals(_lastRecommendationSignature, refreshedSignature, StringComparison.Ordinal))
            {
                return;
            }

            _recommendedWorkoutSource = refreshedSource;
            _recommendationSourceName = refreshedPlanName;
            _lastRecommendationSignature = refreshedSignature;
        }

        LoadRecommendations();

        if (HasRecommendedWorkouts)
        {
            if (_selectedRecommendedWorkout == null ||
                !RecommendedWorkouts.Any(option => ReferenceEquals(option, _selectedRecommendedWorkout)) &&
                !RecommendedWorkouts.Any(option => AreWorkoutsEquivalent(option.Workout, _selectedRecommendedWorkout.Workout)))
            {
                _selectedRecommendedWorkout = null;
                InitializeDefaultRecommendation();
            }
        }
        else
        {
            if (_selectedRecommendedWorkout != null)
            {
                _selectedRecommendedWorkout.IsSelected = false;
                _selectedRecommendedWorkout = null;
            }
        }
    }

    private async void SaveWorkout()
    {
        var recommendedWorkout = _selectedRecommendedWorkout?.Workout;
        var effectiveName = string.IsNullOrWhiteSpace(Name) ? recommendedWorkout?.Name ?? string.Empty : Name;
        var effectiveMuscleGroup = string.IsNullOrWhiteSpace(MuscleGroup) ? recommendedWorkout?.MuscleGroup ?? string.Empty : MuscleGroup;
        effectiveName = InputSanitizer.SanitizeName(effectiveName);
        effectiveMuscleGroup = InputSanitizer.SanitizeMuscleGroup(effectiveMuscleGroup);

        if (string.IsNullOrWhiteSpace(effectiveName) || string.IsNullOrWhiteSpace(effectiveMuscleGroup))
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        if (SelectedType == WorkoutType.WeightLifting && Sets <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var message = UsesTimedStrengthTarget
                    ? "Time and sets must be greater than 0."
                    : "Reps and sets must be greater than 0.";
                await page.DisplayAlert("Error", message, "OK");
            }
            return;
        }

        if (SelectedType == WorkoutType.WeightLifting && UsesTimedStrengthTarget && DurationSeconds <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlert("Error", "Time and sets must be greater than 0.", "OK");
            }
            return;
        }

        if (SelectedType == WorkoutType.WeightLifting && !UsesTimedStrengthTarget && Reps <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlert("Error", "Reps and sets must be greater than 0.", "OK");
            }
            return;
        }

        if (SelectedType == WorkoutType.Cardio && DurationMinutes <= 0 && DistanceMiles <= 0 && Steps <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Error", "For cardio, enter time, distance, or optional tracked steps.", "OK");
            return;
        }

        var newWorkout = new Workout(
            name: effectiveName,
            weight: 0, // User can edit later in EditDayPage
            reps: UsesTimedStrengthTarget ? 0 : Reps,
            sets: Sets,
            muscleGroup: effectiveMuscleGroup,
            day: Day,
            startTime: DateTime.Now,
            type: SelectedType,
            gymLocation: string.Empty // We don't care about GymLocation
        )
        {
            MinReps = recommendedWorkout?.MinReps,
            MaxReps = recommendedWorkout?.MaxReps
        };

        if (SelectedType == WorkoutType.Cardio)
        {
            newWorkout.DurationMinutes = DurationMinutes;
            newWorkout.DistanceMiles = DistanceMiles;
            newWorkout.Steps = Steps;
        }
        else if (UsesTimedStrengthTarget)
        {
            newWorkout.DurationSeconds = DurationSeconds;
            newWorkout.EndTime = DateTime.Now.AddSeconds(DurationSeconds);
        }

        // Add to WeeklySchedule service
        _saveWorkoutAction(newWorkout);

        // Add to EditDayPage ObservableCollection so UI updates live
        _workouts.Add(newWorkout);

        CancelTimedStrengthCountdown(resetToTarget: true);
        NotifyTimedStrengthCountdownStateChanged();

        // Go back to EditDayPage
        await _navigation.PopAsync();
    }

    private void LoadRecommendations()
    {
        RecommendedWorkouts.Clear();

        foreach (var workout in _recommendedWorkoutSource)
        {
            RecommendedWorkouts.Add(new RecommendedWorkoutOption(workout));
        }

        OnPropertyChanged(nameof(HasRecommendedWorkouts));
        OnPropertyChanged(nameof(ActivePlanName));

        if (HasRecommendedWorkouts)
        {
            RecommendedWorkoutSummary = $"Use a workout from '{ActivePlanName}' or tweak it before saving.";
        }
        else if (!string.IsNullOrWhiteSpace(ActivePlanName))
        {
            RecommendedWorkoutSummary = $"'{ActivePlanName}' has no workout on {Day}, so you can add one from scratch.";
        }
    }

    public void InitializeDefaultRecommendation()
    {
        if (HasRecommendedWorkouts && _selectedRecommendedWorkout == null)
        {
            UseRecommendedWorkout(RecommendedWorkouts[0]);
        }
    }

    private void UseRecommendedWorkout(RecommendedWorkoutOption? workoutOption)
    {
        if (workoutOption == null)
        {
            return;
        }

        if (_selectedRecommendedWorkout != null)
        {
            _selectedRecommendedWorkout.IsSelected = false;
        }

        _selectedRecommendedWorkout = workoutOption;
        _selectedRecommendedWorkout.IsSelected = true;

        var workout = workoutOption.Workout;
        _isApplyingLibrarySelection = true;
        Name = workout.Name;
        SelectedMuscleGroup = workout.MuscleGroup;
        SelectedType = workout.Type;
        Sets = workout.Sets;
        Reps = workout.HasRepRange && workout.MaxReps.HasValue
            ? (workout.MaxReps.Value <= 5 ? workout.MinReps ?? workout.Reps : workout.MaxReps.Value)
            : workout.Reps;
        DurationMinutes = workout.DurationMinutes;
        DurationSeconds = workout.TimedTargetSeconds;
        DistanceMiles = workout.DistanceMiles;
        Steps = workout.Steps;
        _isApplyingLibrarySelection = false;
        _lastAppliedTimedExerciseKey = GetTimedExerciseKey();
        ExerciseSuggestions.Clear();
        OnPropertyChanged(nameof(HasExerciseSuggestions));
    }

    private void SelectExerciseSuggestion(WeightliftingExercise? exercise)
    {
        if (exercise == null)
        {
            return;
        }

        _isApplyingLibrarySelection = true;
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.MuscleGroup;
        _isApplyingLibrarySelection = false;
        ExerciseSuggestions.Clear();
        OnPropertyChanged(nameof(HasExerciseSuggestions));
        _ = ApplyTimedStrengthDefaultAsync(forceRefresh: true);
    }

    public async Task UpdateExerciseSuggestionsAsync()
    {
        _exerciseSuggestionDebounceCts?.Cancel();

        if (SelectedType != WorkoutType.WeightLifting || string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            ExerciseSuggestions.Clear();
            OnPropertyChanged(nameof(HasExerciseSuggestions));
            return;
        }

        var requestVersion = Interlocked.Increment(ref _exerciseSuggestionRequestVersion);
        var debounceCts = new CancellationTokenSource();
        _exerciseSuggestionDebounceCts = debounceCts;

        try
        {
            await Task.Delay(175, debounceCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var results = await _workoutLibraryService.SearchExercisesByName(SelectedMuscleGroup, Name ?? string.Empty);

        if (debounceCts.IsCancellationRequested || requestVersion != _exerciseSuggestionRequestVersion)
        {
            return;
        }

        ExerciseSuggestions.Clear();
        foreach (var exercise in results.OrderBy(exercise => exercise.Name).Take(6))
        {
            ExerciseSuggestions.Add(exercise);
        }

        OnPropertyChanged(nameof(HasExerciseSuggestions));
    }

    private void NotifyExerciseImageStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedExerciseInfo));
        OnPropertyChanged(nameof(HasSelectedExerciseImage));
        OnPropertyChanged(nameof(SelectedExerciseImageSource));
    }

    private void HandleTimedStrengthExerciseChanged(bool applyDefaults)
    {
        if (!UsesTimedStrengthTarget)
        {
            _lastAppliedTimedExerciseKey = string.Empty;
        }

        SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: true);

        if (applyDefaults)
        {
            _ = ApplyTimedStrengthDefaultAsync();
        }
    }

    private void StartTimedStrengthCountdown()
    {
        if (!UsesTimedStrengthTarget)
        {
            return;
        }

        var targetSeconds = GetConfiguredTimedStrengthSeconds();
        if (targetSeconds <= 0)
        {
            return;
        }

        CancelTimedStrengthCountdown(resetToTarget: false);
        _timedStrengthCountdownRemainingSeconds = targetSeconds;
        _hasTimedStrengthCountdownCompleted = false;
        _isTimedStrengthCountdownRunning = true;
        NotifyTimedStrengthCountdownStateChanged();

        var countdownCancellation = new CancellationTokenSource();
        _timedStrengthCountdownCancellation = countdownCancellation;
        _ = RunTimedStrengthCountdownAsync(countdownCancellation.Token);
    }

    private async Task RunTimedStrengthCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_timedStrengthCountdownRemainingSeconds > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _timedStrengthCountdownRemainingSeconds = Math.Max(0, _timedStrengthCountdownRemainingSeconds - 1);
                NotifyTimedStrengthCountdownStateChanged();
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                _isTimedStrengthCountdownRunning = false;
                _hasTimedStrengthCountdownCompleted = true;
                NotifyTimedStrengthCountdownStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SyncTimedStrengthCountdownWithTarget(bool resetRunningCountdown)
    {
        if (resetRunningCountdown)
        {
            CancelTimedStrengthCountdown(resetToTarget: false);
        }

        if (!UsesTimedStrengthTarget)
        {
            _timedStrengthCountdownRemainingSeconds = 0;
            _hasTimedStrengthCountdownCompleted = false;
            NotifyTimedStrengthCountdownStateChanged();
            return;
        }

        if (!_isTimedStrengthCountdownRunning)
        {
            _timedStrengthCountdownRemainingSeconds = GetConfiguredTimedStrengthSeconds();
            _hasTimedStrengthCountdownCompleted = false;
        }

        NotifyTimedStrengthCountdownStateChanged();
    }

    private void CancelTimedStrengthCountdown(bool resetToTarget)
    {
        _timedStrengthCountdownCancellation?.Cancel();
        _timedStrengthCountdownCancellation?.Dispose();
        _timedStrengthCountdownCancellation = null;
        _isTimedStrengthCountdownRunning = false;

        if (resetToTarget)
        {
            _timedStrengthCountdownRemainingSeconds = GetConfiguredTimedStrengthSeconds();
            _hasTimedStrengthCountdownCompleted = false;
        }
    }

    private int GetConfiguredTimedStrengthSeconds()
        => UsesTimedStrengthTarget ? Math.Max(0, DurationSeconds) : 0;

    private int GetEffectiveTimedStrengthCountdownSeconds()
    {
        if (_timedStrengthCountdownRemainingSeconds > 0 || _isTimedStrengthCountdownRunning || _hasTimedStrengthCountdownCompleted)
        {
            return Math.Max(0, _timedStrengthCountdownRemainingSeconds);
        }

        return GetConfiguredTimedStrengthSeconds();
    }

    private void NotifyTimedStrengthCountdownStateChanged()
    {
        OnPropertyChanged(nameof(ShowTimedStrengthCountdown));
        OnPropertyChanged(nameof(TimedStrengthCountdownDisplay));
        OnPropertyChanged(nameof(TimedStrengthStartButtonText));
        OnPropertyChanged(nameof(CanStartTimedStrengthCountdown));
    }

    private async Task ApplyTimedStrengthDefaultAsync(bool forceRefresh = false)
    {
        var timedExerciseKey = GetTimedExerciseKey();
        if (string.IsNullOrWhiteSpace(timedExerciseKey))
        {
            NotifyTimedStrengthCountdownStateChanged();
            return;
        }

        if (!forceRefresh &&
            DurationSeconds > 0 &&
            string.Equals(_lastAppliedTimedExerciseKey, timedExerciseKey, StringComparison.OrdinalIgnoreCase))
        {
            NotifyTimedStrengthCountdownStateChanged();
            return;
        }

        var requestVersion = Interlocked.Increment(ref _timedStrengthDefaultRequestVersion);
        var history = await GetWorkoutHistoryAsync();

        if (requestVersion != _timedStrengthDefaultRequestVersion ||
            !string.Equals(GetTimedExerciseKey(), timedExerciseKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var resolvedSeconds = GetLastUsedTimedStrengthSeconds(history, timedExerciseKey);
        if (resolvedSeconds <= 0)
        {
            resolvedSeconds = GetDefaultTimedStrengthSeconds(timedExerciseKey);
        }

        if (resolvedSeconds <= 0)
        {
            return;
        }

        DurationSeconds = resolvedSeconds;
        _lastAppliedTimedExerciseKey = timedExerciseKey;
    }

    private async Task<IReadOnlyList<Workout>> GetWorkoutHistoryAsync()
    {
        if (_lastLoadedWorkoutChangeVersion == _workoutService.ChangeVersion && _workoutHistory.Count > 0)
        {
            return _workoutHistory;
        }

        var history = await _workoutService.GetWorkouts();
        _workoutHistory = history.ToList();
        _lastLoadedWorkoutChangeVersion = _workoutService.ChangeVersion;
        return _workoutHistory;
    }

    private string GetTimedExerciseKey()
        => UsesTimedStrengthTarget
            ? Name.Trim()
            : string.Empty;

    private static int GetLastUsedTimedStrengthSeconds(IEnumerable<Workout> workoutHistory, string exerciseName)
        => workoutHistory
            .Where(workout =>
                workout.Type == WorkoutType.WeightLifting &&
                string.Equals(workout.Name, exerciseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(workout => workout.StartTime)
            .Select(workout => workout.TimedTargetSeconds)
            .FirstOrDefault(seconds => seconds > 0);

    private static int GetDefaultTimedStrengthSeconds(string exerciseName)
    {
        var normalizedName = exerciseName.Trim().ToLowerInvariant();
        if (normalizedName.Equals("plank", StringComparison.Ordinal))
        {
            return 30;
        }

        if (normalizedName.Contains("suitcase carry", StringComparison.Ordinal))
        {
            return 35;
        }

        if (normalizedName.Contains("farmer carry", StringComparison.Ordinal))
        {
            return 40;
        }

        if (normalizedName.Contains("balance hold", StringComparison.Ordinal) ||
            normalizedName.Contains("stance hold", StringComparison.Ordinal))
        {
            return 30;
        }

        return normalizedName.Contains("carry", StringComparison.Ordinal) ? 40 : 30;
    }

    private static string FormatCountdownDisplay(int totalSeconds)
    {
        var safeSeconds = Math.Max(0, totalSeconds);
        var time = TimeSpan.FromSeconds(safeSeconds);
        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    private static bool AreWorkoutsEquivalent(Workout left, Workout right)
    {
        return string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.MuscleGroup, right.MuscleGroup, StringComparison.OrdinalIgnoreCase) &&
               left.Type == right.Type &&
               left.Day == right.Day;
    }

    private static string BuildRecommendationSignature(string planName, IEnumerable<Workout> workouts)
    {
        return string.Join("||",
            planName.Trim(),
            string.Join("::", workouts.Select(workout => string.Join("|",
                workout.Day,
                workout.Type,
                workout.Name?.Trim(),
                workout.MuscleGroup?.Trim(),
                workout.Sets,
                workout.Reps,
                workout.DurationMinutes,
                workout.DurationSeconds,
                workout.DistanceMiles,
                workout.Steps,
                workout.PlanWeekNumber))));
    }
}

