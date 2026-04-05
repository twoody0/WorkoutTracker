using WorkoutTracker.Models;

namespace WorkoutTracker.Helpers;

public static class ExerciseAlternativeCatalog
{
    private static readonly Dictionary<string, string[]> ExactAlternatives = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Barbell Bench Press"] = ["Dumbbell Bench Press", "Machine Chest Press", "Push-Up"],
        ["Close-Grip Bench Press"] = ["Machine Chest Press", "Dip", "Push-Up"],
        ["Incline Bench Press"] = ["Incline Dumbbell Press", "Machine Chest Press", "Push-Up"],
        ["Incline Dumbbell Press"] = ["Machine Chest Press", "Dumbbell Bench Press", "Push-Up"],
        ["Machine Chest Press"] = ["Dumbbell Bench Press", "Push-Up", "Incline Dumbbell Press"],
        ["Cable Crossover"] = ["Dumbbell Fly", "Machine Chest Press", "Push-Up"],
        ["Dumbbell Fly"] = ["Cable Crossover", "Machine Chest Press", "Push-Up"],
        ["Push-Up"] = ["Incline Push-Up", "Machine Chest Press", "Dumbbell Bench Press"],
        ["Incline Push-Up"] = ["Push-Up", "Machine Chest Press", "Dumbbell Bench Press"],
        ["Wall Push-Up"] = ["Incline Push-Up", "Push-Up", "Machine Chest Press"],
        ["Pull-Up"] = ["Lat Pulldown", "Seated Cable Row", "Resistance Band Row"],
        ["Lat Pulldown"] = ["Pull-Up", "Seated Cable Row", "Resistance Band Row"],
        ["Seated Cable Row"] = ["Chest-Supported Row", "Single-Arm Dumbbell Row", "Resistance Band Row"],
        ["Chest-Supported Row"] = ["Seated Cable Row", "Single-Arm Dumbbell Row", "Resistance Band Row"],
        ["Bent-Over Row"] = ["Chest-Supported Row", "Seated Cable Row", "Single-Arm Dumbbell Row"],
        ["Single-Arm Dumbbell Row"] = ["Seated Cable Row", "Chest-Supported Row", "Resistance Band Row"],
        ["Resistance Band Row"] = ["Seated Cable Row", "Chest-Supported Row", "Single-Arm Dumbbell Row"],
        ["Back Squat"] = ["Goblet Squat", "Leg Press", "Box Squat"],
        ["Front Squat"] = ["Goblet Squat", "Leg Press", "Box Squat"],
        ["Pause Back Squat"] = ["Goblet Squat", "Leg Press", "Box Squat"],
        ["Leg Press"] = ["Goblet Squat", "Box Squat", "Walking Lunge"],
        ["Box Squat"] = ["Goblet Squat", "Leg Press", "Step-Up"],
        ["Trap Bar Deadlift"] = ["Romanian Deadlift", "Dumbbell Romanian Deadlift", "Hip Thrust"],
        ["Romanian Deadlift"] = ["Dumbbell Romanian Deadlift", "Hip Thrust", "Glute Bridge"],
        ["Dumbbell Romanian Deadlift"] = ["Romanian Deadlift", "Hip Thrust", "Glute Bridge"],
        ["Hip Thrust"] = ["Romanian Deadlift", "Dumbbell Romanian Deadlift", "Glute Bridge"],
        ["Walking Lunge"] = ["Reverse Lunge", "Step-Up", "Bulgarian Split Squat"],
        ["Reverse Lunge"] = ["Walking Lunge", "Step-Up", "Bulgarian Split Squat"],
        ["Bulgarian Split Squat"] = ["Reverse Lunge", "Step-Up", "Walking Lunge"],
        ["Step-Up"] = ["Walking Lunge", "Reverse Lunge", "Supported Split Squat"],
        ["Supported Split Squat"] = ["Step-Up", "Reverse Lunge", "Walking Lunge"],
        ["Bodyweight Squat"] = ["Goblet Squat", "Step-Up", "Sit-to-Stand"],
        ["Sit-to-Stand"] = ["Step-Up", "Bodyweight Squat", "Goblet Squat"],
        ["Overhead Press"] = ["Dumbbell Shoulder Press", "Seated Dumbbell Shoulder Press", "Landmine Press"],
        ["Push Press"] = ["Overhead Press", "Dumbbell Shoulder Press", "Landmine Press"],
        ["Dumbbell Shoulder Press"] = ["Seated Dumbbell Shoulder Press", "Landmine Press", "Pike Push-Up"],
        ["Seated Dumbbell Shoulder Press"] = ["Dumbbell Shoulder Press", "Landmine Press", "Pike Push-Up"],
        ["Half-Kneeling Shoulder Press"] = ["Dumbbell Shoulder Press", "Landmine Press", "Pike Push-Up"],
        ["Landmine Press"] = ["Dumbbell Shoulder Press", "Seated Dumbbell Shoulder Press", "Pike Push-Up"],
        ["Lateral Raise"] = ["Cable Lateral Raise", "Rear Delt Fly", "Face Pull"],
        ["Cable Lateral Raise"] = ["Lateral Raise", "Rear Delt Fly", "Face Pull"],
        ["Rear Delt Fly"] = ["Face Pull", "Band Pull-Apart", "Cable Lateral Raise"],
        ["Face Pull"] = ["Rear Delt Fly", "Band Pull-Apart", "Cable Lateral Raise"],
        ["Band Pull-Apart"] = ["Face Pull", "Rear Delt Fly", "Cable Lateral Raise"],
        ["Hammer Curl"] = ["EZ-Bar Curl", "Cable Curl", "Dumbbell Curl"],
        ["EZ-Bar Curl"] = ["Hammer Curl", "Cable Curl", "Dumbbell Curl"],
        ["Cable Curl"] = ["EZ-Bar Curl", "Hammer Curl", "Dumbbell Curl"],
        ["Cable Triceps Pushdown"] = ["Overhead Triceps Extension", "Dip", "Push-Up"],
        ["Overhead Triceps Extension"] = ["Cable Triceps Pushdown", "Dip", "Push-Up"],
        ["Dip"] = ["Machine Chest Press", "Push-Up", "Overhead Triceps Extension"],
        ["Calf Raise"] = ["Standing Calf Raise", "Seated Calf Raise", "Step-Up"],
        ["Standing Calf Raise"] = ["Seated Calf Raise", "Calf Raise", "Step-Up"],
        ["Seated Calf Raise"] = ["Standing Calf Raise", "Calf Raise", "Step-Up"],
        ["Plank"] = ["Dead Bug", "Pallof Press", "Bird Dog"],
        ["Dead Bug"] = ["Plank", "Bird Dog", "Pallof Press"],
        ["Bird Dog"] = ["Dead Bug", "Plank", "Pallof Press"],
        ["Pallof Press"] = ["Plank", "Dead Bug", "Bird Dog"],
        ["Farmer Carry"] = ["Suitcase Carry", "Plank", "Pallof Press"],
        ["Suitcase Carry"] = ["Farmer Carry", "Plank", "Pallof Press"],
        ["Single-Leg Balance Hold"] = ["Tandem Stance Hold", "Heel-to-Toe Walk", "Step-Up"],
        ["Tandem Stance Hold"] = ["Single-Leg Balance Hold", "Heel-to-Toe Walk", "Step-Up"],
        ["Heel-to-Toe Walk"] = ["Tandem Stance Hold", "Single-Leg Balance Hold", "Step-Up"],
        ["Glute Bridge"] = ["Hip Thrust", "Bodyweight Good Morning", "Dumbbell Romanian Deadlift"],
        ["Bodyweight Good Morning"] = ["Hip Hinge Drill", "Glute Bridge", "Dumbbell Romanian Deadlift"]
    };

    private static readonly Dictionary<string, string[]> MuscleGroupFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chest"] = ["Dumbbell Bench Press", "Machine Chest Press", "Push-Up"],
        ["Back"] = ["Seated Cable Row", "Lat Pulldown", "Chest-Supported Row"],
        ["Legs"] = ["Goblet Squat", "Leg Press", "Walking Lunge"],
        ["Shoulders"] = ["Dumbbell Shoulder Press", "Lateral Raise", "Rear Delt Fly"],
        ["Arms"] = ["Hammer Curl", "Cable Triceps Pushdown", "EZ-Bar Curl"],
        ["Biceps"] = ["Hammer Curl", "EZ-Bar Curl", "Cable Curl"],
        ["Triceps"] = ["Cable Triceps Pushdown", "Overhead Triceps Extension", "Dip"],
        ["Core"] = ["Plank", "Dead Bug", "Pallof Press"],
        ["Cardio"] = ["Bike Intervals", "Row", "Brisk Walk"]
    };

    public static IReadOnlyList<string> GetAlternatives(Workout workout, int maxCount = 3)
        => GetAlternatives(workout?.Name, workout?.MuscleGroup, workout?.Type ?? WorkoutType.WeightLifting, maxCount);

    public static IReadOnlyList<string> GetAlternatives(string? exerciseName, string? muscleGroup, WorkoutType workoutType, int maxCount = 3)
    {
        if (workoutType != WorkoutType.WeightLifting || string.IsNullOrWhiteSpace(exerciseName) || maxCount <= 0)
        {
            return [];
        }

        var normalizedName = exerciseName.Trim();
        var results = new List<string>();

        if (ExactAlternatives.TryGetValue(normalizedName, out var exactAlternatives))
        {
            AddDistinct(results, exactAlternatives, normalizedName);
        }

        AddDistinct(results, GetPatternAlternatives(normalizedName), normalizedName);

        if (!string.IsNullOrWhiteSpace(muscleGroup) &&
            MuscleGroupFallbacks.TryGetValue(muscleGroup.Trim(), out var muscleGroupAlternatives))
        {
            AddDistinct(results, muscleGroupAlternatives, normalizedName);
        }

        return results.Take(maxCount).ToArray();
    }

    private static IEnumerable<string> GetPatternAlternatives(string exerciseName)
    {
        var normalized = exerciseName.ToLowerInvariant();

        if (normalized.Contains("bench") || normalized.Contains("chest press"))
        {
            return ["Dumbbell Bench Press", "Machine Chest Press", "Push-Up"];
        }

        if (normalized.Contains("push-up") || normalized.Contains("push up"))
        {
            return ["Incline Push-Up", "Machine Chest Press", "Dumbbell Bench Press"];
        }

        if (normalized.Contains("row"))
        {
            return ["Seated Cable Row", "Chest-Supported Row", "Single-Arm Dumbbell Row"];
        }

        if (normalized.Contains("pull-up") || normalized.Contains("pull up") || normalized.Contains("pulldown"))
        {
            return ["Lat Pulldown", "Seated Cable Row", "Resistance Band Row"];
        }

        if (normalized.Contains("squat") || normalized.Contains("leg press"))
        {
            return ["Goblet Squat", "Leg Press", "Box Squat"];
        }

        if (normalized.Contains("deadlift") || normalized.Contains("hinge") || normalized.Contains("good morning"))
        {
            return ["Romanian Deadlift", "Dumbbell Romanian Deadlift", "Hip Thrust"];
        }

        if (normalized.Contains("lunge") || normalized.Contains("split squat") || normalized.Contains("step-up") || normalized.Contains("step up"))
        {
            return ["Walking Lunge", "Reverse Lunge", "Step-Up"];
        }

        if (normalized.Contains("press"))
        {
            return ["Dumbbell Shoulder Press", "Seated Dumbbell Shoulder Press", "Landmine Press"];
        }

        if (normalized.Contains("raise") || normalized.Contains("face pull") || normalized.Contains("rear delt"))
        {
            return ["Lateral Raise", "Rear Delt Fly", "Face Pull"];
        }

        if (normalized.Contains("curl"))
        {
            return ["Hammer Curl", "EZ-Bar Curl", "Cable Curl"];
        }

        if (normalized.Contains("triceps") || normalized.Contains("pushdown") || normalized.Contains("dip") || normalized.Contains("extension"))
        {
            return ["Cable Triceps Pushdown", "Overhead Triceps Extension", "Dip"];
        }

        if (normalized.Contains("plank") || normalized.Contains("dead bug") || normalized.Contains("bird dog") || normalized.Contains("pallof"))
        {
            return ["Plank", "Dead Bug", "Pallof Press"];
        }

        if (normalized.Contains("carry") || normalized.Contains("balance hold") || normalized.Contains("stance hold"))
        {
            return ["Farmer Carry", "Suitcase Carry", "Plank"];
        }

        return [];
    }

    private static void AddDistinct(List<string> results, IEnumerable<string> candidates, string currentExerciseName)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) ||
                string.Equals(candidate, currentExerciseName, StringComparison.OrdinalIgnoreCase) ||
                results.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(candidate.Trim());
        }
    }
}
