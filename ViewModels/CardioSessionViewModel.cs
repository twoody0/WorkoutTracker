using System;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Maui.Controls;

namespace WorkoutTracker.ViewModels
{
    public class CardioWorkoutViewModel : BaseViewModel
    {
        private readonly IWorkoutService _workoutService;
        private readonly IStepCounterService _stepCounterService;

        public CardioWorkoutViewModel(IWorkoutService workoutService, IStepCounterService stepCounterService)
        {
            _workoutService = workoutService;
            _stepCounterService = stepCounterService;
            _stepCounterService.StepsUpdated += OnStepsUpdated;
            SessionSteps = 0;
            IsTracking = false;
        }

        private void OnStepsUpdated(object sender, int steps)
        {
            SessionSteps = steps;
        }

        private int _sessionSteps;
        public int SessionSteps
        {
            get => _sessionSteps;
            set { _sessionSteps = value; OnPropertyChanged(); }
        }

        private bool _isTracking;
        public bool IsTracking
        {
            get => _isTracking;
            set { _isTracking = value; OnPropertyChanged(); }
        }

        public ICommand StartSessionCommand => new Command(() =>
        {
            SessionSteps = 0;
            _stepCounterService.StartTracking();
            IsTracking = true;
        });

        public ICommand StopSessionCommand => new Command(async () =>
        {
            _stepCounterService.StopTracking();
            IsTracking = false;

            // Create a Cardio workout entry automatically
            var workout = new Workout
            {
                Name = "Cardio Session",
                Type = WorkoutType.Cardio,
                Steps = SessionSteps,
                StartTime = DateTime.Now,  // For simplicity; you may store the actual start time.
                EndTime = DateTime.Now
            };
            await _workoutService.AddWorkout(workout);

            // Reset session count if desired
            SessionSteps = 0;
        });
    }
}
