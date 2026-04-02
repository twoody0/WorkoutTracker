using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class WorkoutRecommendation : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _remainingSets;
    private int _plannedSets;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required Workout Workout { get; init; }
    public double? LastUsedWeight { get; init; }
    public bool IsWeightLifting => Workout.Type == WorkoutType.WeightLifting;
    public bool IsCardio => Workout.Type == WorkoutType.Cardio;
    public string WeightDisplayPrefix { get; init; } = "Weight";
    public string WeightDisplayValue { get; init; } = string.Empty;
    public bool HasWeightDisplay => !string.IsNullOrWhiteSpace(WeightDisplayValue);
    public string WeightDisplayText => HasWeightDisplay
        ? $"{WeightDisplayPrefix}: {WeightDisplayValue}"
        : string.Empty;
    public string WeightHelperText { get; init; } = string.Empty;
    public string RepDisplayText { get; init; } = string.Empty;
    public string DurationDisplayText { get; init; } = string.Empty;
    public string DistanceDisplayText { get; init; } = string.Empty;
    public string TargetRpeText { get; init; } = string.Empty;
    public string TargetRestText { get; init; } = string.Empty;
    public int RemainingSets
    {
        get => _remainingSets;
        set
        {
            if (_remainingSets == value)
            {
                return;
            }

            _remainingSets = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RemainingSetsText));
        }
    }

    public int PlannedSets
    {
        get => _plannedSets;
        set
        {
            if (_plannedSets == value)
            {
                return;
            }

            _plannedSets = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRemainingSets));
            OnPropertyChanged(nameof(RemainingSetsText));
        }
    }
    public bool HasRemainingSets => IsWeightLifting && PlannedSets > 0;
    public string RemainingSetsText => HasRemainingSets
        ? $"{RemainingSets}/{PlannedSets} sets left"
        : string.Empty;
    public bool HasLastUsedWeight => LastUsedWeight.HasValue;
    public bool HasWeightHelperText => !string.IsNullOrWhiteSpace(WeightHelperText);
    public bool HasRepDisplay => !string.IsNullOrWhiteSpace(RepDisplayText);
    public bool HasDuration => !string.IsNullOrWhiteSpace(DurationDisplayText);
    public bool HasDistance => !string.IsNullOrWhiteSpace(DistanceDisplayText);
    public bool HasTargetRpe => !string.IsNullOrWhiteSpace(TargetRpeText);
    public bool HasTargetRest => !string.IsNullOrWhiteSpace(TargetRestText);
    public string ActionButtonText => IsCardio ? "Track Workout" : "Use This Workout";
    public bool ShowUseButton => IsCardio || !IsSelected;
    public bool ShowDetails => IsCardio || !IsSelected;
    public bool ShowStrengthDetails => ShowDetails && IsWeightLifting;
    public bool ShowCardioDetails => ShowDetails && IsCardio;
    public bool ShowSelectedState => IsSelected && IsWeightLifting;
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
            OnPropertyChanged(nameof(ActionButtonText));
            OnPropertyChanged(nameof(ShowUseButton));
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowStrengthDetails));
            OnPropertyChanged(nameof(ShowCardioDetails));
            OnPropertyChanged(nameof(ShowSelectedState));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
