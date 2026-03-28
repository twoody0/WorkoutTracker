using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class WeeklyScheduleDayGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DayOfWeek Day { get; }
    public string DayLabel => Day.ToString();
    public bool IsToday { get; }
    public bool HasWorkouts => WorkoutCount > 0;
    public bool CanToggle => HasWorkouts;
    public string ToggleGlyph => IsExpanded ? "^" : "v";
    public int WorkoutCount => Workouts.Count;
    public string WorkoutCountLabel => WorkoutCount == 0 ? "Rest day" : $"Planned sessions: {WorkoutCount}";
    public string RestDayMessage => "Rest day";
    public string MuscleGroupSummary => HasWorkouts
        ? string.Join(", ", Workouts
            .Select(workout => string.IsNullOrWhiteSpace(workout.MuscleGroup) ? "Other" : workout.MuscleGroup.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase))
        : string.Empty;
    public ObservableCollection<Workout> Workouts { get; }
    public IEnumerable<Workout> VisibleWorkouts => IsExpanded ? Workouts : Enumerable.Empty<Workout>();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleGlyph));
            OnPropertyChanged(nameof(VisibleWorkouts));
            OnPropertyChanged(nameof(ShowRestDayMessage));
        }
    }

    public bool ShowRestDayMessage => IsExpanded && !HasWorkouts;

    public WeeklyScheduleDayGroup(DayOfWeek day, IEnumerable<Workout> workouts, bool isToday, bool isExpanded)
    {
        Day = day;
        IsToday = isToday;
        Workouts = new ObservableCollection<Workout>(workouts);
        _isExpanded = isExpanded;
    }

    public void UpdateWorkouts(IEnumerable<Workout> workouts)
    {
        Workouts.Clear();

        foreach (var workout in workouts)
        {
            Workouts.Add(workout);
        }

        OnPropertyChanged(nameof(HasWorkouts));
        OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(WorkoutCount));
        OnPropertyChanged(nameof(WorkoutCountLabel));
        OnPropertyChanged(nameof(MuscleGroupSummary));
        OnPropertyChanged(nameof(VisibleWorkouts));
        OnPropertyChanged(nameof(ShowRestDayMessage));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
