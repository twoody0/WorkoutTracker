using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class RecommendedWorkoutOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Workout Workout { get; }
    public string Name => Workout.Name;
    public string MuscleGroup => Workout.MuscleGroup;
    public WorkoutType Type => Workout.Type;
    public int Sets => Workout.Sets;
    public int Reps => Workout.Reps;
    public string RepDisplay => Workout.RepDisplay;
    public int Steps => Workout.Steps;
    public int DurationMinutes => Workout.DurationMinutes;
    public string DurationValueDisplay => Workout.DurationValueDisplay;
    public double DistanceMiles => Workout.DistanceMiles;
    public bool HasRepTarget => Workout.HasRepTarget;
    public bool HasSteps => Workout.HasSteps;
    public bool HasDuration => Workout.HasDuration;
    public bool HasDistance => Workout.HasDistance;
    public bool IsWeightLifting => Type == WorkoutType.WeightLifting;
    public bool IsCardio => Type == WorkoutType.Cardio;
    public bool ShowDetails => !IsSelected;
    public bool ShowWeightLiftingDetails => ShowDetails && IsWeightLifting;
    public bool ShowCardioDetails => ShowDetails && IsCardio;
    public bool ShowUseButton => !IsSelected;
    public bool ShowSelectedState => IsSelected;
    public bool HasExerciseInfo => IsWeightLifting && Helpers.ExerciseInfoCatalog.HasInfo(Name);
    public bool HasExerciseImage => IsWeightLifting && Helpers.ExerciseImageCatalog.HasImage(Name);
    public string ExerciseImageSource => Helpers.ExerciseImageCatalog.GetImageSource(Name);
    public string SelectedText => "Currently selected";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowWeightLiftingDetails));
            OnPropertyChanged(nameof(ShowCardioDetails));
            OnPropertyChanged(nameof(ShowUseButton));
            OnPropertyChanged(nameof(ShowSelectedState));
        }
    }

    public RecommendedWorkoutOption(Workout workout)
    {
        Workout = workout;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
