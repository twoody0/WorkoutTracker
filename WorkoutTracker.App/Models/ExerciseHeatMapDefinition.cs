namespace WorkoutTracker.Models;

public class ExerciseHeatMapDefinition
{
    public string Name { get; set; } = string.Empty;
    public double EffortMultiplier { get; set; } = 1.0;
    public Dictionary<string, double> Regions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
