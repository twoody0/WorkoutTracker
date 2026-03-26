using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class AddWorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;
    private readonly ObservableCollection<Workout> _workouts;
    private readonly INavigation _navigation;
    private string _name = string.Empty;
    private string _muscleGroup = string.Empty;
    private WorkoutType _selectedType;
    private int _sets;
    private int _reps;
    private int _steps;
    private string _recommendedWorkoutSummary = "Build a custom workout for this day.";
    private RecommendedWorkoutOption? _selectedRecommendedWorkout;

    public DayOfWeek Day { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string MuscleGroup
    {
        get => _muscleGroup;
        set => SetProperty(ref _muscleGroup, value);
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
            }
        }
    }

    public int Sets
    {
        get => _sets;
        set => SetProperty(ref _sets, value);
    }

    public int Reps
    {
        get => _reps;
        set => SetProperty(ref _reps, value);
    }

    public int Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, value);
    }

    public List<WorkoutType> WorkoutTypes { get; } = Enum.GetValues(typeof(WorkoutType)).Cast<WorkoutType>().ToList();
    public ObservableCollection<RecommendedWorkoutOption> RecommendedWorkouts { get; } = new();

    public bool IsWeightLifting => SelectedType == WorkoutType.WeightLifting;
    public bool IsCardio => SelectedType == WorkoutType.Cardio;
    public bool HasRecommendedWorkouts => RecommendedWorkouts.Count > 0;
    public string ActivePlanName => _scheduleService.ActivePlan?.Name ?? string.Empty;
    public string RecommendedWorkoutSummary
    {
        get => _recommendedWorkoutSummary;
        private set => SetProperty(ref _recommendedWorkoutSummary, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand UseRecommendedWorkoutCommand { get; }

    public AddWorkoutViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService, ObservableCollection<Workout> workouts, INavigation navigation)
    {
        Day = day;
        _scheduleService = scheduleService;
        _workouts = workouts;
        _navigation = navigation;

        SelectedType = WorkoutType.WeightLifting; // Default
        SaveCommand = new Command(SaveWorkout);
        UseRecommendedWorkoutCommand = new Command<RecommendedWorkoutOption>(UseRecommendedWorkout);

        LoadRecommendations();
    }

    private async void SaveWorkout()
    {
        var recommendedWorkout = _selectedRecommendedWorkout?.Workout;
        var effectiveName = string.IsNullOrWhiteSpace(Name) ? recommendedWorkout?.Name ?? string.Empty : Name;
        var effectiveMuscleGroup = string.IsNullOrWhiteSpace(MuscleGroup) ? recommendedWorkout?.MuscleGroup ?? string.Empty : MuscleGroup;

        if (string.IsNullOrWhiteSpace(effectiveName) || string.IsNullOrWhiteSpace(effectiveMuscleGroup))
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Error", "Please fill in all required fields.", "OK");
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
        );

        if (SelectedType == WorkoutType.Cardio)
        {
            newWorkout.Steps = Steps;
        }

        // Add to WeeklySchedule service
        _scheduleService.AddWorkoutToDay(Day, newWorkout);

        // Add to EditDayPage ObservableCollection so UI updates live
        _workouts.Add(newWorkout);

        // Go back to EditDayPage
        await _navigation.PopAsync();
    }

    private void LoadRecommendations()
    {
        RecommendedWorkouts.Clear();

        foreach (var workout in _scheduleService.GetActivePlanWorkoutsForDay(Day))
        {
            RecommendedWorkouts.Add(new RecommendedWorkoutOption(workout));
        }

        OnPropertyChanged(nameof(HasRecommendedWorkouts));
        OnPropertyChanged(nameof(ActivePlanName));

        if (HasRecommendedWorkouts)
        {
            RecommendedWorkoutSummary = $"Use a workout from '{ActivePlanName}' or tweak it before saving.";
        }
        else if (_scheduleService.ActivePlan != null)
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
        Name = workout.Name;
        MuscleGroup = workout.MuscleGroup;
        SelectedType = workout.Type;
        Sets = workout.Sets;
        Reps = workout.Reps;
        Steps = workout.Steps;
    }
}

