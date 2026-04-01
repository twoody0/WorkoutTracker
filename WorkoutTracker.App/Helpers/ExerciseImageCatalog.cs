namespace WorkoutTracker.Helpers;

public static class ExerciseImageCatalog
{
    private static readonly Dictionary<string, string> ExerciseImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ab Rollout"] = "ab_rollout.webp",
        ["Barbell Bench Press"] = "barbell_bench_bress.webp",
        ["Dumbbell Bench Press"] = "dumbbell_bench_press.webp",
        ["Incline Bench Press"] = "incline_barbell_bench_press.webp"
    };

    public static bool HasImage(string? exerciseName)
        => TryGetImageSource(exerciseName, out _);

    public static string GetImageSource(string? exerciseName)
        => TryGetImageSource(exerciseName, out var imageSource)
            ? imageSource
            : string.Empty;

    public static bool TryGetImageSource(string? exerciseName, out string imageSource)
    {
        if (!string.IsNullOrWhiteSpace(exerciseName) &&
            ExerciseImageMap.TryGetValue(exerciseName.Trim(), out var mappedSource))
        {
            imageSource = mappedSource;
            return true;
        }

        imageSource = string.Empty;
        return false;
    }
}
