using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for the dashboard page showing workouts and analytics per day.
/// </summary>
public class DashboardViewModel : BaseViewModel
{
    #region Private Fields

    private readonly IWorkoutService _workoutService;
    private readonly IAuthService _authService;

    private ObservableCollection<Workout> _workouts = new();
    private DateTime _selectedDate;
    private double _totalWeightLifted;
    private double _caloriesBurned;
    private bool _hasWeightlifting;
    private bool _hasCardio;

    #endregion

    #region Constructor

    public DashboardViewModel(IWorkoutService workoutService, IAuthService authService)
    {
        _workoutService = workoutService;
        _authService = authService;

        LoadWorkoutsCommand = new Command(async () => await LoadWorkoutsAsync());
        SelectedDate = DateTime.Today;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Command to trigger loading of workouts based on the selected date.
    /// </summary>
    public ICommand LoadWorkoutsCommand { get; }

    /// <summary>
    /// Workouts displayed for the selected date.
    /// </summary>
    public ObservableCollection<Workout> Workouts
    {
        get => _workouts;
        set => SetProperty(ref _workouts, value);
    }

    /// <summary>
    /// The currently selected date.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                LoadWorkoutsCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// The total amount of weight lifted for the selected day.
    /// </summary>
    public double TotalWeightLifted
    {
        get => _totalWeightLifted;
        set => SetProperty(ref _totalWeightLifted, value);
    }

    /// <summary>
    /// The estimated calories burned based on steps and body weight.
    /// </summary>
    public double CaloriesBurned
    {
        get => _caloriesBurned;
        set => SetProperty(ref _caloriesBurned, value);
    }

    /// <summary>
    /// Indicates whether any weightlifting workouts were logged.
    /// </summary>
    public bool HasWeightlifting
    {
        get => _hasWeightlifting;
        set => SetProperty(ref _hasWeightlifting, value);
    }

    /// <summary>
    /// Indicates whether any cardio workouts were logged.
    /// </summary>
    public bool HasCardio
    {
        get => _hasCardio;
        set => SetProperty(ref _hasCardio, value);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads workouts for the selected date and calculates analytics.
    /// </summary>
    private async Task LoadWorkoutsAsync()
    {
        IEnumerable<Workout> allWorkouts = await _workoutService.GetWorkouts();
        IEnumerable<Workout> filtered = allWorkouts
            .Where(w => w.StartTime.Date == SelectedDate.Date);

        HasWeightlifting = filtered.Any(w =>
            w.Type == WorkoutType.WeightLifting && w.Reps > 0 && w.Sets > 0);
        HasCardio = filtered.Any(w =>
            w.Type == WorkoutType.Cardio && w.Steps > 0);

        Workouts.Clear();
        double total = 0;

        foreach (var w in filtered)
        {
            Workouts.Add(w);
            if (w.Type == WorkoutType.WeightLifting && w.Reps > 0 && w.Sets > 0)
                total += w.Weight * w.Reps * w.Sets;
        }

        TotalWeightLifted = total;

        int totalSteps = filtered
            .Where(w => w.Type == WorkoutType.Cardio)
            .Sum(w => w.Steps);

        double weightLbs = _authService.CurrentUser?.Weight ?? 154;
        double weightKg = weightLbs * 0.453592;
        CaloriesBurned = totalSteps * 0.04 * (weightKg / 70);
    }

    #endregion
}
