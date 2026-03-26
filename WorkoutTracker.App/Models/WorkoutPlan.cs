using System.Diagnostics.CodeAnalysis;

namespace WorkoutTracker.Models;

public class WorkoutPlan
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int DurationInWeeks { get; set; } = 4;
    public List<Workout> Workouts { get; set; } = new();
    public bool IsCustom { get; set; }
    public string DurationDisplay => DurationInWeeks % 4 == 0 && DurationInWeeks >= 8
        ? $"{DurationInWeeks / 4} month{(DurationInWeeks == 4 ? string.Empty : "s")}"
        : $"{DurationInWeeks} week{(DurationInWeeks == 1 ? string.Empty : "s")}";

    public WorkoutPlan() { }

    [SetsRequiredMembers]
    public WorkoutPlan(string name, string description, bool isCustom = false, string category = "General", int durationInWeeks = 4)
    {
        Name = name;
        Description = description;
        IsCustom = isCustom;
        Category = category;
        DurationInWeeks = durationInWeeks;
    }
}
