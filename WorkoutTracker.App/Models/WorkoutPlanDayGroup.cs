using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.Models;

public class WorkoutPlanDayGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DayOfWeek Day { get; }
    public string DayLabel => Day.ToString();
    public string ToggleGlyph => !HasWorkouts ? string.Empty : IsExpanded ? "v" : ">";
    public string ToggleLabel => IsExpanded ? "Hide" : "Show";
    public int WorkoutCount => Workouts.Count;
    public bool HasWorkouts => WorkoutCount > 0;
    public bool ShowRestDayMessage => IsExpanded && !HasWorkouts;
    public bool ShowToggle => HasWorkouts;
    public string WorkoutCountLabel => WorkoutCount switch
    {
        0 => "Rest day",
        1 => "1 workout",
        _ => $"{WorkoutCount} workouts"
    };
    public string RestDayMessage => "Rest day. No exercises scheduled.";
    public string MuscleGroupSummary => HasWorkouts
        ? string.Join(", ", MuscleGroups.Select(group => group.MuscleGroupLabel))
        : string.Empty;
    public ObservableCollection<WorkoutDisplay> Workouts { get; }
    public ObservableCollection<WorkoutPlanMuscleGroupGroup> MuscleGroups { get; }
    public IEnumerable<WorkoutPlanMuscleGroupGroup> VisibleMuscleGroups => IsExpanded ? MuscleGroups : Enumerable.Empty<WorkoutPlanMuscleGroupGroup>();

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
            OnPropertyChanged(nameof(ToggleLabel));
            OnPropertyChanged(nameof(ShowToggle));
            OnPropertyChanged(nameof(VisibleMuscleGroups));
            OnPropertyChanged(nameof(ShowRestDayMessage));
        }
    }

    public WorkoutPlanDayGroup(DayOfWeek day, IEnumerable<WorkoutDisplay> workouts, bool isExpanded = false)
    {
        Day = day;
        Workouts = new ObservableCollection<WorkoutDisplay>(workouts);
        MuscleGroups = new ObservableCollection<WorkoutPlanMuscleGroupGroup>(
            Workouts
                .GroupBy(workout => workout.Workout.MuscleGroup?.Trim() ?? string.Empty)
                .OrderBy(group => group.Key)
                .Select(group => new WorkoutPlanMuscleGroupGroup(group.Key, group)));
        _isExpanded = isExpanded;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
