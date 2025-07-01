namespace WorkoutTracker.Models;

public class WeightliftingExercise
{
    public string Name { get; set; }
    public string MuscleGroup { get; set; }
    public WeightliftingExercise(string name, string muscleGroup)
    {
        Name = name;
        MuscleGroup = muscleGroup;
    }
}
