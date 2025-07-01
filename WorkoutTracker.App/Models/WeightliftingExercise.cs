namespace WorkoutTracker.Models;

public class WeightliftingExercise
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }

    // Required for data binding / deserialization
    public WeightliftingExercise() { }

    public WeightliftingExercise(string name, string muscleGroup)
    {
        Name = name;
        MuscleGroup = muscleGroup;
    }
}
