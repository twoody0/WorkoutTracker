using System.Diagnostics.CodeAnalysis;

namespace WorkoutTracker.Models;

public class WorkoutPlan
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<Workout> Workouts { get; set; } = new();
    public bool IsCustom { get; set; }

    public WorkoutPlan() { }

    [SetsRequiredMembers]
    public WorkoutPlan(string name, string description, bool isCustom = false)
    {
        Name = name;
        Description = description;
        IsCustom = isCustom;
    }
}
