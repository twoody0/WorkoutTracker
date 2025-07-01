using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for displaying the leaderboard based on gym location.
/// </summary>
public class LeaderboardViewModel : BaseViewModel
{
    #region Private Fields

    private readonly IWorkoutService _workoutService;
    private string _gymLocation;

    #endregion

    #region Constructor

    public LeaderboardViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        LeaderboardWorkouts = new ObservableCollection<Workout>();
        GymLocation = string.Empty;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gym location used to filter leaderboard entries.
    /// </summary>
    public string GymLocation
    {
        get => _gymLocation;
        set => SetProperty(ref _gymLocation, value);
    }

    /// <summary>
    /// Workouts filtered by gym location for the leaderboard.
    /// </summary>
    public ObservableCollection<Workout> LeaderboardWorkouts { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Command to load and filter leaderboard workouts by location.
    /// </summary>
    public ICommand LoadLeaderboardCommand => new Command(async () => await LoadLeaderboardAsync());

    #endregion

    #region Private Methods

    private async Task LoadLeaderboardAsync()
    {
        IEnumerable<Workout> allWorkouts = await _workoutService.GetWorkouts();
        IEnumerable<Workout> filtered = allWorkouts.Where(w =>
            !string.IsNullOrWhiteSpace(w.GymLocation) &&
            w.GymLocation.Equals(GymLocation, StringComparison.OrdinalIgnoreCase));

        LeaderboardWorkouts.Clear();
        foreach (var workout in filtered)
        {
            LeaderboardWorkouts.Add(workout);
        }
    }

    #endregion
}
