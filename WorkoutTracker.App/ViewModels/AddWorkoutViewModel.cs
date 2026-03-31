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
    private readonly ObservableCollection<Workout> _workouts;
    private readonly INavigation _navigation;
    private readonly IReadOnlyList<Workout> _recommendedWorkoutSource;
    private readonly string _recommendationSourceName;
    private readonly Action<Workout> _saveWorkoutAction;
    private string _name = string.Empty;
    private string _muscleGroup = string.Empty;
    private string _selectedMuscleGroup = string.Empty;
    private WorkoutType _selectedType;
    private int _sets;
    private int _reps;
    private int _steps;
    private int _durationMinutes;
    private double _distanceMiles;
    private string _recommendedWorkoutSummary = "Build a custom workout for this day.";
    private RecommendedWorkoutOption? _selectedRecommendedWorkout;
    private bool _isApplyingLibrarySelection;
    private CancellationTokenSource? _exerciseSuggestionDebounceCts;
    private int _exerciseSuggestionRequestVersion;

    public DayOfWeek Day { get; }

    public string Name
    {
        get => _name;
        set
        {
            var sanitized = InputSanitizer.SanitizeName(value);
            if (SetProperty(ref _name, sanitized) && !_isApplyingLibrarySelection)
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
        set => SetProperty(ref _reps, Math.Clamp(value, 0, InputSanitizer.MaxReps));
    }

    public int Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, Math.Clamp(value, 0, InputSanitizer.MaxSteps));
    }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, Math.Clamp(value, 0, InputSanitizer.MaxDurationMinutes));
    }

    public double DistanceMiles
    {
        get => _distanceMiles;
        set => SetProperty(ref _distanceMiles, Math.Clamp(value, 0, InputSanitizer.MaxDistanceMiles));
    }

    public List<WorkoutType> WorkoutTypes { get; } = Enum.GetValues(typeof(WorkoutType)).Cast<WorkoutType>().ToList();
    public List<string> MuscleGroups { get; } = ["Back", "Arms", "Biceps", "Cardio", "Chest", "Core", "Abs", "Legs", "Shoulders", "Triceps"];
    public ObservableCollection<RecommendedWorkoutOption> RecommendedWorkouts { get; } = new();
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; } = new();

    public bool IsWeightLifting => SelectedType == WorkoutType.WeightLifting;
    public bool IsCardio => SelectedType == WorkoutType.Cardio;
    public bool HasRecommendedWorkouts => RecommendedWorkouts.Count > 0;
    public bool HasExerciseSuggestions => ExerciseSuggestions.Count > 0;
    public string ActivePlanName => _recommendationSourceName;
    public string RecommendedWorkoutSummary
    {
        get => _recommendedWorkoutSummary;
        private set => SetProperty(ref _recommendedWorkoutSummary, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand UseRecommendedWorkoutCommand { get; }
    public ICommand SelectExerciseSuggestionCommand { get; }

    public AddWorkoutViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService, IWorkoutLibraryService workoutLibraryService, ObservableCollection<Workout> workouts, INavigation navigation)
        : this(
            day,
            workoutLibraryService,
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
        ObservableCollection<Workout> workouts,
        INavigation navigation,
        IEnumerable<Workout>? recommendedWorkouts,
        string recommendationSourceName,
        Action<Workout> saveWorkoutAction)
    {
        Day = day;
        _workoutLibraryService = workoutLibraryService;
        _workouts = workouts;
        _navigation = navigation;
        _recommendedWorkoutSource = recommendedWorkouts?.ToList() ?? [];
        _recommendationSourceName = recommendationSourceName;
        _saveWorkoutAction = saveWorkoutAction;

        SelectedType = WorkoutType.WeightLifting; // Default
        SaveCommand = new Command(SaveWorkout);
        UseRecommendedWorkoutCommand = new Command<RecommendedWorkoutOption>(UseRecommendedWorkout);
        SelectExerciseSuggestionCommand = new Command<WeightliftingExercise>(SelectExerciseSuggestion);

        LoadRecommendations();
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

        if (SelectedType == WorkoutType.WeightLifting && (Reps <= 0 || Sets <= 0))
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Error", "Reps and sets must be greater than 0.", "OK");
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
            reps: Reps,
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

        // Add to WeeklySchedule service
        _saveWorkoutAction(newWorkout);

        // Add to EditDayPage ObservableCollection so UI updates live
        _workouts.Add(newWorkout);

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
        DistanceMiles = workout.DistanceMiles;
        Steps = workout.Steps;
        _isApplyingLibrarySelection = false;
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
}

