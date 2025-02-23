﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class LeaderboardViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;
    public LeaderboardViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        LeaderboardWorkouts = new ObservableCollection<Workout>();
        GymLocation = string.Empty;
    }

    // Gym location entered by the user.
    private string _gymLocation;
    public string GymLocation
    {
        get => _gymLocation;
        set { _gymLocation = value; OnPropertyChanged(); }
    }

    // The collection of workouts that match the gym location.
    public ObservableCollection<Workout> LeaderboardWorkouts { get; set; }

    public ICommand LoadLeaderboardCommand => new Command(async () => await LoadLeaderboardAsync());

    private async Task LoadLeaderboardAsync()
    {
        // Get all workouts from the service.
        IEnumerable<Workout> allWorkouts = await _workoutService.GetWorkouts();
        // Filter workouts that have a matching GymLocation.
        IEnumerable<Workout> filtered = allWorkouts.Where(w =>
            !string.IsNullOrWhiteSpace(w.GymLocation) &&
            w.GymLocation.Equals(GymLocation, StringComparison.OrdinalIgnoreCase));

        LeaderboardWorkouts.Clear();
        foreach (Workout workout in filtered)
        {
            LeaderboardWorkouts.Add(workout);
        }
    }
}
