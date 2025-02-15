using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;

    public WorkoutViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        // Populate the list of workout types from the enum:
        WorkoutTypes = Enum.GetValues(typeof(WorkoutType)).Cast<WorkoutType>().ToList();
        // Set a default type:
        SelectedWorkoutType = WorkoutType.WeightLifting;
    }

    public List<WorkoutType> WorkoutTypes { get; set; }

    private WorkoutType _selectedWorkoutType;
    public WorkoutType SelectedWorkoutType
    {
        get => _selectedWorkoutType;
        set
        {
            if (_selectedWorkoutType != value)
            {
                _selectedWorkoutType = value;
                OnPropertyChanged();
            }
        }
    }

    // Common property
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    // Weight lifting fields:
    private double _weight;
    public double Weight
    {
        get => _weight;
        set { _weight = value; OnPropertyChanged(); }
    }

    private int _reps;
    public int Reps
    {
        get => _reps;
        set { _reps = value; OnPropertyChanged(); }
    }

    private int _sets;
    public int Sets
    {
        get => _sets;
        set { _sets = value; OnPropertyChanged(); }
    }

    // Cardio field:
    private int _steps;
    public int Steps
    {
        get => _steps;
        set { _steps = value; OnPropertyChanged(); }
    }

    // Command to add the workout
    private ICommand _addWorkoutCommand;
    public ICommand AddWorkoutCommand => _addWorkoutCommand ??= new Command(async () => await AddWorkoutAsync());

    private async Task AddWorkoutAsync()
    {
        var workout = new Workout
        {
            Name = Name,
            Type = SelectedWorkoutType,
            StartTime = DateTime.Now
        };

        if (SelectedWorkoutType == WorkoutType.WeightLifting)
        {
            workout.Weight = Weight;
            workout.Reps = Reps;
            workout.Sets = Sets;
        }
        else if (SelectedWorkoutType == WorkoutType.Cardio)
        {
            workout.Steps = Steps;
        }

        await _workoutService.AddWorkout(workout);

        // Clear fields after adding:
        Name = string.Empty;
        Weight = 0;
        Reps = 0;
        Sets = 0;
        Steps = 0;
    }
}
