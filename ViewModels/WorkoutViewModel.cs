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
    }

    // Properties bound to the UI
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    private double _weight;
    public double Weight
    {
        get => _weight;
        set
        {
            if (_weight != value)
            {
                _weight = value;
                OnPropertyChanged();
            }
        }
    }

    private int _reps;
    public int Reps
    {
        get => _reps;
        set
        {
            if (_reps != value)
            {
                _reps = value;
                OnPropertyChanged();
            }
        }
    }

    private int _sets;
    public int Sets
    {
        get => _sets;
        set
        {
            if (_sets != value)
            {
                _sets = value;
                OnPropertyChanged();
            }
        }
    }

    // Command to add the workout
    private ICommand _addWorkoutCommand;
    public ICommand AddWorkoutCommand
        => _addWorkoutCommand ??= new Command(async () => await AddWorkoutAsync());

    private async Task AddWorkoutAsync()
    {
        // Build a new Workout object
        var workout = new Workout
        {
            Name = Name,
            Weight = Weight,
            Reps = Reps,
            Sets = Sets,
            StartTime = DateTime.Now
        };

        // Call service to save it
        await _workoutService.AddWorkout(workout);

        // Clear the fields after adding
        Name = string.Empty;
        Weight = 0;
        Reps = 0;
        Sets = 0;
    }
}
