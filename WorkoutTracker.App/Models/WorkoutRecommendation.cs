using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class WorkoutRecommendation : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required Workout Workout { get; init; }
    public double? LastUsedWeight { get; init; }
    public string WeightDisplayPrefix { get; init; } = "Weight";
    public string WeightDisplayValue { get; init; } = string.Empty;
    public string WeightDisplayText => string.IsNullOrWhiteSpace(WeightDisplayValue)
        ? WeightDisplayPrefix
        : $"{WeightDisplayPrefix}: {WeightDisplayValue}";
    public string WeightHelperText { get; init; } = string.Empty;
    public string RepDisplayText { get; init; } = string.Empty;
    public string TargetRpeText { get; init; } = string.Empty;
    public string TargetRestText { get; init; } = string.Empty;
    public bool HasLastUsedWeight => LastUsedWeight.HasValue;
    public bool HasWeightHelperText => !string.IsNullOrWhiteSpace(WeightHelperText);
    public bool HasTargetRpe => !string.IsNullOrWhiteSpace(TargetRpeText);
    public bool HasTargetRest => !string.IsNullOrWhiteSpace(TargetRestText);
    public bool ShowUseButton => !IsSelected;
    public bool ShowDetails => !IsSelected;
    public bool ShowSelectedState => IsSelected;
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
            OnPropertyChanged(nameof(ShowUseButton));
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowSelectedState));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
