using System.Collections.ObjectModel;

namespace WorkoutTracker.Models;

public class WorkoutPlanMuscleGroupGroup
{
    public string MuscleGroup { get; }
    public string MuscleGroupLabel => string.IsNullOrWhiteSpace(MuscleGroup) ? "Other" : MuscleGroup;
    public ObservableCollection<WorkoutDisplay> Workouts { get; }

    public WorkoutPlanMuscleGroupGroup(string muscleGroup, IEnumerable<WorkoutDisplay> workouts)
    {
        MuscleGroup = muscleGroup;
        Workouts = new ObservableCollection<WorkoutDisplay>(workouts);
    }
}
