using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class WorkoutRecommendation : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required Workout Workout { get; init; }
    public double? LastUsedWeight { get; init; }
    public bool HasLastUsedWeight => LastUsedWeight.HasValue;
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
