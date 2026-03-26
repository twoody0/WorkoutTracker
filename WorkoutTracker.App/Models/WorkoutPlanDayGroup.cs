using System.Collections.ObjectModel;

namespace WorkoutTracker.Models;

public class WorkoutPlanDayGroup
{
    public DayOfWeek Day { get; }
    public ObservableCollection<WorkoutDisplay> Workouts { get; }

    public WorkoutPlanDayGroup(DayOfWeek day, IEnumerable<WorkoutDisplay> workouts)
    {
        Day = day;
        Workouts = new ObservableCollection<WorkoutDisplay>(workouts);
    }
}
