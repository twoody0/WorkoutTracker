using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    public ICommand LoadWorkoutsCommand { get; set; }
    public ObservableCollection<Workout> Workouts { get; set; }
    public double CaloriesBurned
    {
        get => _caloriesBurned;
        set { _caloriesBurned = value; OnPropertyChanged(); }
    }
    public bool HasWeightlifting
    {
        get => _hasWeightlifting;
        set { _hasWeightlifting = value; OnPropertyChanged(); }
    }
    public bool HasCardio
    {
        get => _hasCardio;
        set { _hasCardio = value; OnPropertyChanged(); }
    }

    private readonly IWorkoutService _workoutService;
    private double _totalWeightLifted;
    private DateTime _selectedDate;
    private double _caloriesBurned;
    private readonly IAuthService _authService;
    private bool _hasWeightlifting;
    private bool _hasCardio;

    public DashboardViewModel(IWorkoutService workoutService, IAuthService authService)
    {
        _workoutService = workoutService;
        _authService = authService;
        Workouts = new ObservableCollection<Workout>();
        LoadWorkoutsCommand = new Command(async () => await LoadWorkoutsAsync());
        SelectedDate = DateTime.Today;
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadWorkoutsCommand.Execute(null);
            }
        }
    }

    private async Task LoadWorkoutsAsync()
    {
        IEnumerable<Workout> allWorkouts = await _workoutService.GetWorkouts();
        IEnumerable<Workout> filtered = allWorkouts.Where(w => w.StartTime.Date == SelectedDate.Date);
        HasWeightlifting = filtered.Any(w => w.Type == WorkoutType.WeightLifting && w.Reps > 0 && w.Sets > 0);
        HasCardio = filtered.Any(w => w.Type == WorkoutType.Cardio && w.Steps > 0);

        Workouts.Clear();
        double total = 0;

        foreach (var w in filtered)
        {
            Workouts.Add(w);
            if (w.Type == WorkoutType.WeightLifting && w.Reps > 0 && w.Sets > 0)
                total += w.Weight * w.Reps * w.Sets;
        }

        TotalWeightLifted = total;

        // Calculate calories from cardio steps
        int totalSteps = filtered.Where(w => w.Type == WorkoutType.Cardio).Sum(w => w.Steps);
        double weightLbs = _authService.CurrentUser?.Weight ?? 154;
        double weightKg = weightLbs * 0.453592;
        int age = _authService.CurrentUser?.Age ?? 30;

        CaloriesBurned = totalSteps * 0.04 * (weightKg / 70);
    }

    public double TotalWeightLifted
    {
        get => _totalWeightLifted;
        set { _totalWeightLifted = value; OnPropertyChanged(); }
    }
}
