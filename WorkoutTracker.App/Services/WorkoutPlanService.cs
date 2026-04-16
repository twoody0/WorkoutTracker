using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services
{
    public class WorkoutPlanService : IWorkoutPlanService
    {
        private readonly List<WorkoutPlan> _plans = new();
        private readonly WorkoutTrackerDatabase _database;
        private readonly string _legacyCustomPlansFilePath;
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true
        };
        private static readonly Dictionary<string, string[]> ContextualMuscleGroupCandidates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dip"] = ["Chest", "Triceps"],
            ["Push-Up"] = ["Chest", "Triceps"],
            ["Incline Push-Up"] = ["Chest", "Triceps"],
            ["Wall Push-Up"] = ["Chest", "Triceps"],
            ["Diamond Push-Ups"] = ["Triceps", "Chest"],
            ["Close-Grip Bench Press"] = ["Triceps", "Chest"],
            ["Pull-Up"] = ["Back", "Biceps"],
            ["Face Pull"] = ["Back", "Shoulders"],
            ["Rear Delt Fly"] = ["Back", "Shoulders"],
            ["Reverse Pec Deck Fly"] = ["Back", "Shoulders"],
            ["Band Pull-Apart"] = ["Back", "Shoulders"],
            ["Pike Push-Up"] = ["Shoulders", "Triceps"]
        };

        public WorkoutPlanService(string? databasePath = null)
        {
            _database = new WorkoutTrackerDatabase(databasePath);
            _legacyCustomPlansFilePath = Path.Combine(
                Path.GetDirectoryName(_database.DatabasePath) ?? string.Empty,
                "custom_workout_plans.json");

            _plans.Add(new WorkoutPlan
            {
                Name = "Beginner Full Body Foundation",
                Description = "An 8-week beginner strength plan with three nonconsecutive full-body days. Weeks rotate squat, hinge, push, pull, arm, and core variations so new lifters build skill without repeating the exact same sessions every week. Sessions are fuller than a bare-minimum template but still beginner-friendly.",
                Category = "Beginner Strength",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        StrengthWorkout("Goblet Squat", 10, 3, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Incline Push-Up", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Seated Cable Row", 10, 3, "Back", DayOfWeek.Monday),
                        RangeStrengthWorkout("Lateral Raise", 12, 15, 2, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Hip Hinge Drill", 12, 2, "Legs", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Lat Pulldown", 10, 3, "Back", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 2, "Arms", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Press", 12, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Machine Chest Press", 10, 2, "Chest", DayOfWeek.Friday),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(2,
                        StrengthWorkout("Box Squat", 8, 3, "Legs", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Dumbbell Floor Press", 10, 3, "Chest", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 12, 15, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Half-Kneeling Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Pull-Up", 6, 3, "Back", DayOfWeek.Wednesday),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Push-Up", 8, 2, "Chest", DayOfWeek.Friday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 2, "Arms", DayOfWeek.Friday),
                        TimedStrengthWorkout("Plank", 30, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(3,
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Machine Chest Press", 8, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 8, 3, "Back", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Incline Dumbbell Curl", 10, 12, 2, "Arms", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 10, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Seated Cable Row", 8, 3, "Back", DayOfWeek.Friday),
                        RangeStrengthWorkout("Push-Up", 8, 10, 2, "Chest", DayOfWeek.Friday),
                        TimedStrengthWorkout("Farmer Carry", 40, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(4,
                        StrengthWorkout("Goblet Squat", 12, 2, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Incline Push-Up", 12, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Lateral Raise", 12, 2, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Pull-Up", 5, 2, "Back", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Hammer Curl", 12, 15, 2, "Arms", DayOfWeek.Wednesday),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Machine Chest Press", 10, 2, "Chest", DayOfWeek.Friday),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Friday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Upper/Lower Strength Builder",
                Description = "An 8-week upper/lower split for intermediates. The plan alternates heavy compounds, secondary variations, lower-fatigue weeks, and dedicated low-rep strength exposures. Several sessions use top sets plus backoff work and fuller 5-6 exercise days so progression feels more like a real coached strength block.",
                Category = "Strength Progression",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Barbell Bench Press", 4, 6, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Barbell Bench Press", 1, 3, 1, "Chest", DayOfWeek.Monday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Bent-Over Row", 8, 4, "Back", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Overhead Press", 8, 3, "Shoulders", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Pull-Up", 6, 3, "Back", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Hammer Curl", 8, 10, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Back Squat", 4, 6, 4, "Legs", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Back Squat", 1, 3, 1, "Legs", DayOfWeek.Tuesday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Romanian Deadlift", 8, 3, "Legs", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Walking Lunge", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Incline Dumbbell Press", 6, 8, 4, "Chest", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Seated Cable Row", 10, 4, "Back", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Face Pull", 12, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("EZ-Bar Curl", 10, 12, 2, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Front Squat", 4, 6, 4, "Legs", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Hip Thrust", 8, 4, "Legs", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Hamstring Curl", 10, 3, "Legs", DayOfWeek.Friday),
                        RangeStrengthWorkout("EZ-Bar Curl", 8, 10, 2, "Arms", DayOfWeek.Friday),
                        TimedStrengthWorkout("Plank", 45, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(2,
                        RangeStrengthWorkout("Close-Grip Bench Press", 4, 6, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Close-Grip Bench Press", 1, 3, 1, "Chest", DayOfWeek.Monday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Chest-Supported Row", 8, 4, "Back", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Lat Pulldown", 8, 3, "Back", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        RangeStrengthWorkout("Hammer Curl", 8, 10, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Pause Back Squat", 3, 4, 4, "Legs", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Tuesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Bulgarian Split Squat", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Incline Bench Press", 4, 6, 4, "Chest", DayOfWeek.Thursday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Incline Bench Press", 1, 3, 1, "Chest", DayOfWeek.Thursday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 4, "Back", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Rear Delt Fly", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Curl", 10, 12, 2, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Trap Bar Deadlift", 3, 5, 4, "Legs", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Trap Bar Deadlift", 1, 3, 1, "Legs", DayOfWeek.Friday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Dead Bug", 10, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(3,
                        RangeStrengthWorkout("Barbell Bench Press", 2, 4, 5, "Chest", DayOfWeek.Monday, targetRpe: 9, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Barbell Bench Press", 1, 2, 1, "Chest", DayOfWeek.Monday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Bent-Over Row", 5, 4, "Back", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Push Press", 2, 4, 4, "Shoulders", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Pull-Up", 1, 3, 4, "Back", DayOfWeek.Monday, targetRpe: 9, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Back Squat", 2, 4, 5, "Legs", DayOfWeek.Tuesday, targetRpe: 9, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Back Squat", 1, 2, 1, "Legs", DayOfWeek.Tuesday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Romanian Deadlift", 5, 4, "Legs", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Walking Lunge", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Calf Raise", 12, 3, "Legs", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Incline Dumbbell Press", 6, 8, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Seated Cable Row", 8, 4, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Face Pull", 12, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 10, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Front Squat", 2, 4, 4, "Legs", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Hip Thrust", 5, 4, "Legs", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Hamstring Curl", 10, 3, "Legs", DayOfWeek.Friday),
                        RangeStrengthWorkout("Hammer Curl", 8, 10, 2, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Hanging Knee Raise", 10, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(4,
                        RangeStrengthWorkout("Dumbbell Bench Press", 8, 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Landmine Press", 10, 2, "Shoulders", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Lat Pulldown", 10, 2, "Back", DayOfWeek.Monday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Front Squat", 6, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Hip Thrust", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Reverse Lunge", 10, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Standing Calf Raise", 15, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 3, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Crossover", 15, 2, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Lateral Raise", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Rear Delt Fly", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 2, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Curl", 12, 15, 2, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hamstring Curl", 12, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Friday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Push/Pull/Legs Hypertrophy",
                Description = "A 16-week six-day hypertrophy split with rotating exercise variations and rep targets. Each four-week block changes angles, accessories, fatigue demand, and occasional strength-emphasis weeks. Sessions can run 5-6 lifts and some key compounds use rep ranges or top-set/backoff pairings instead of one fixed prescription.",
                Category = "Muscle Building",
                DurationInWeeks = 16,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Barbell Bench Press", 6, 8, 4, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 8, 10, 3, "Chest", DayOfWeek.Monday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Cable Crossover", 12, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Lateral Raise", 12, 15, 2, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 10, 4, "Back", DayOfWeek.Tuesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Tuesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Face Pull", 12, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("EZ-Bar Curl", 12, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Back Squat", 8, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Leg Press", 12, 3, "Legs", DayOfWeek.Wednesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Leg Extension", 12, 15, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Dip", 8, 10, 2, "Chest", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Pull-Up", 8, 4, "Back", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 3, "Back", DayOfWeek.Friday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 12, 3, "Arms", DayOfWeek.Friday),
                        RangeStrengthWorkout("Face Pull", 12, 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Front Squat", 8, 4, "Legs", DayOfWeek.Saturday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Bulgarian Split Squat", 10, 3, "Legs", DayOfWeek.Saturday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Saturday),
                        RangeStrengthWorkout("Leg Extension", 12, 15, 2, "Legs", DayOfWeek.Saturday)),
                    Week(2,
                        StrengthWorkout("Dumbbell Bench Press", 10, 4, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Incline Bench Press", 8, 3, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Machine Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Pec Deck Fly", 12, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Lateral Raise", 12, 15, 2, "Shoulders", DayOfWeek.Monday),
                        RangeStrengthWorkout("Overhead Triceps Extension", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Pull-Up", 6, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Curl", 12, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Hack Squat", 10, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Stiff-Leg Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Walking Lunge", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 4, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Leg Extension", 12, 15, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 12, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Dip", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Neutral-Grip Lat Pulldown", 10, 4, "Back", DayOfWeek.Friday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Chest-Supported Row", 12, 3, "Back", DayOfWeek.Friday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Rear Delt Cable Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 10, 3, "Arms", DayOfWeek.Friday),
                        RangeStrengthWorkout("Cable Curl", 10, 12, 2, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Leg Press", 12, 4, "Legs", DayOfWeek.Saturday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Bulgarian Split Squat", 10, 3, "Legs", DayOfWeek.Saturday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 4, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hip Thrust", 10, 2, "Legs", DayOfWeek.Saturday)),
                    Week(3,
                        RangeStrengthWorkout("Barbell Bench Press", 3, 5, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Barbell Bench Press", 1, 3, 1, "Chest", DayOfWeek.Monday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 6, 8, 3, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Cable Crossover", 15, 2, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 10, 12, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Pull-Up", 3, 5, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("T-Bar Row", 5, 7, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Hammer Curl", 8, 10, 2, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Back Squat", 3, 5, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Back Squat", 1, 3, 1, "Legs", DayOfWeek.Wednesday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Romanian Deadlift", 5, 7, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 12, 4, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Leg Extension", 10, 12, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 8, 4, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 10, 3, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Dip", 6, 8, 2, "Chest", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Pull-Up", 1, 3, 4, "Back", DayOfWeek.Friday, targetRpe: 9, targetRestRange: "3-4 min"),
                        StrengthWorkout("Single-Arm Dumbbell Row", 8, 4, "Back", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Cable Curl", 12, 3, "Arms", DayOfWeek.Friday),
                        RangeStrengthWorkout("Face Pull", 12, 15, 2, "Shoulders", DayOfWeek.Friday),
                        RangeStrengthWorkout("Front Squat", 3, 5, 4, "Legs", DayOfWeek.Saturday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Front Squat", 1, 3, 1, "Legs", DayOfWeek.Saturday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Bulgarian Split Squat", 8, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hamstring Curl", 10, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 12, 4, "Legs", DayOfWeek.Saturday),
                        RangeStrengthWorkout("Leg Extension", 10, 12, 2, "Legs", DayOfWeek.Saturday)),
                    Week(4,
                        StrengthWorkout("Dumbbell Bench Press", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Machine Incline Press", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Machine Shoulder Press", 12, 2, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Cable Crossover", 15, 2, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Lateral Raise", 15, 20, 2, "Shoulders", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 12, 15, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Neutral-Grip Lat Pulldown", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Curl", 12, 2, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Hammer Curl", 12, 15, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Hack Squat", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 12, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Leg Extension", 15, 20, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 12, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Arnold Press", 12, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 15, 2, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Crossover", 15, 20, 2, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Lat Pulldown", 12, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Chest-Supported Row", 12, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Cable Fly", 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 12, 2, "Arms", DayOfWeek.Friday),
                        RangeStrengthWorkout("Cable Curl", 12, 15, 2, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Leg Press", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Bulgarian Split Squat", 10, 2, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hamstring Curl", 12, 2, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Saturday),
                        RangeStrengthWorkout("Leg Extension", 15, 20, 2, "Legs", DayOfWeek.Saturday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Arnold Split Mass Builder",
                Description = "An 8-week classic Arnold-style split with chest and back together, shoulders and arms together, and two leg sessions each week. Upper-body days are intentionally fuller, with multiple presses, rows, fly variations, laterals, and arm movements in the same session so the plan feels more like a real mass-building gym routine.",
                Category = "Muscle Building",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Barbell Bench Press", 6, 8, 4, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 8, 10, 3, "Chest", DayOfWeek.Monday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Pull-Up", 6, 8, 4, "Back", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Chest-Supported Row", 10, 4, "Back", DayOfWeek.Monday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Lat Pulldown", 10, 3, "Back", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 4, "Shoulders", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Cable Lateral Raise", 12, 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Reverse Pec Deck Fly", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Hammer Curl", 10, 12, 3, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Skull Crushers", 10, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Cable Triceps Pushdown", 12, 15, 2, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Back Squat", 6, 8, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Romanian Deadlift", 8, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Bulgarian Split Squat", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Hanging Knee Raise", 10, 3, "Core", DayOfWeek.Wednesday, "Studio"),
                        RangeStrengthWorkout("Incline Bench Press", 6, 8, 4, "Chest", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Dumbbell Bench Press", 10, 3, "Chest", DayOfWeek.Thursday, targetRpe: 7.5, targetRestRange: "1-2 min"),
                        StrengthWorkout("Dip", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Bent-Over Row", 8, 4, "Back", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("T-Bar Row", 10, 3, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Neutral-Grip Lat Pulldown", 10, 3, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Overhead Press", 6, 8, 4, "Shoulders", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Machine Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Friday),
                        RangeStrengthWorkout("Lateral Raise", 12, 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Barbell Curl", 10, 3, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Preacher Curl", 12, 2, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Overhead Triceps Extension", 10, 3, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Close-Grip Bench Press", 8, 3, "Arms", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Front Squat", 6, 8, 4, "Legs", DayOfWeek.Saturday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Hip Thrust", 10, 4, "Legs", DayOfWeek.Saturday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Walking Lunge", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Leg Extension", 15, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 4, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Pallof Press", 10, 3, "Core", DayOfWeek.Saturday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Classic Body Part Split",
                Description = "An 8-week five-day bodybuilding split with dedicated chest, back, shoulders, arms, and leg days. Each upper-body session intentionally stacks several exercises for the same muscle group so users who like classic bodybuilding structure have a higher-volume option beside the more balanced full-body and upper/lower plans.",
                Category = "Muscle Building",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Barbell Bench Press", 5, 7, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 8, 10, 4, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Dumbbell Fly", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Cable Crossover", 15, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Dip", 8, 10, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Push-Up", 12, 15, 2, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Deadlift", 4, 6, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        RangeStrengthWorkout("Pull-Up", 6, 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Bent-Over Row", 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Lat Pulldown", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Overhead Press", 5, 7, 4, "Shoulders", DayOfWeek.Wednesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Reverse Pec Deck Fly", 15, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Front Raise", 12, 2, "Shoulders", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Close-Grip Bench Press", 6, 8, 4, "Arms", DayOfWeek.Thursday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Skull Crushers", 10, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 2, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Preacher Curl", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Hammer Curl", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Concentration Curl", 12, 2, "Arms", DayOfWeek.Thursday),
                        RangeStrengthWorkout("Back Squat", 5, 7, 4, "Legs", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Romanian Deadlift", 8, 4, "Legs", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Bulgarian Split Squat", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Leg Extension", 12, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Standing Calf Raise", 15, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Friday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Chest/Triceps Back/Biceps Split Builder",
                Description = "An 8-week bodybuilding split built around classic paired muscle-group days: chest with triceps, back with biceps, a dedicated leg day, a shoulder day, and a fifth arm plus core day to round out the week. The first three weeks build volume and intensity, week four pulls fatigue down, and then the four-week wave repeats so the full block feels structured without becoming stale.",
                Category = "Muscle Building",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Barbell Bench Press", 5, 7, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Close-Grip Bench Press", 8, 3, "Arms", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 8, 10, 3, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Bent-Over Row", 6, 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Pull-Up", 6, 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Hammer Curl", 12, 3, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Chest-Supported Row", 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Lat Pulldown", 10, 3, "Back", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Back Squat", 5, 7, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Romanian Deadlift", 8, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 10, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Standing Calf Raise", 15, 4, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Overhead Press", 5, 7, 4, "Shoulders", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Cable Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Reverse Pec Deck Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Front Raise", 12, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Close-Grip Bench Press", 8, 3, "Arms", DayOfWeek.Saturday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Hammer Curl", 12, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 2, "Arms", DayOfWeek.Saturday),
                        TimedStrengthWorkout("Plank", 45, 3, "Core", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Hanging Knee Raise", 10, 3, "Core", DayOfWeek.Saturday, "Studio")),
                    Week(2,
                        RangeStrengthWorkout("Incline Bench Press", 4, 6, 4, "Chest", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Skull Crushers", 10, 3, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Dumbbell Bench Press", 8, 3, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Dip", 8, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Crossover", 12, 15, 2, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Bent-Over Row", 6, 8, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Preacher Curl", 10, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Neutral-Grip Lat Pulldown", 8, 10, 3, "Back", DayOfWeek.Tuesday, targetRpe: 8, targetRestRange: "1-2 min"),
                        RangeStrengthWorkout("Cable Curl", 10, 12, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 10, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 3, "Back", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Front Squat", 4, 6, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Hip Thrust", 8, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Bulgarian Split Squat", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Extension", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Hamstring Curl", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 4, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Machine Shoulder Press", 6, 8, 4, "Shoulders", DayOfWeek.Friday, targetRpe: 8, targetRestRange: "2-3 min"),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Skull Crushers", 10, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Preacher Curl", 10, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Saturday),
                        RangeStrengthWorkout("Cable Curl", 10, 12, 2, "Arms", DayOfWeek.Saturday),
                        TimedStrengthWorkout("Farmer Carry", 45, 3, "Core", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Cable Crunch", 12, 3, "Core", DayOfWeek.Saturday, "Studio")),
                    Week(3,
                        RangeStrengthWorkout("Barbell Bench Press", 3, 5, 5, "Chest", DayOfWeek.Monday, targetRpe: 9, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Barbell Bench Press", 1, 3, 1, "Chest", DayOfWeek.Monday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        StrengthWorkout("Close-Grip Bench Press", 6, 3, "Arms", DayOfWeek.Monday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        RangeStrengthWorkout("Incline Dumbbell Press", 6, 8, 3, "Chest", DayOfWeek.Monday, targetRpe: 8, targetRestRange: "1-2 min"),
                        StrengthWorkout("Overhead Triceps Extension", 10, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Machine Chest Press", 8, 3, "Chest", DayOfWeek.Monday),
                        RangeStrengthWorkout("Pull-Up", 3, 5, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("EZ-Bar Curl", 8, 3, "Arms", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("T-Bar Row", 5, 7, 4, "Back", DayOfWeek.Tuesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Hammer Curl", 10, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Chest-Supported Row", 8, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 12, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Lat Pulldown", 8, 3, "Back", DayOfWeek.Tuesday),
                        RangeStrengthWorkout("Back Squat", 3, 5, 5, "Legs", DayOfWeek.Wednesday, targetRpe: 9, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Back Squat", 1, 3, 1, "Legs", DayOfWeek.Wednesday, targetRpe: 9.5, targetRestRange: "3-5 min"),
                        RangeStrengthWorkout("Romanian Deadlift", 5, 7, 4, "Legs", DayOfWeek.Wednesday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("Leg Press", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 8, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Standing Calf Raise", 12, 4, "Legs", DayOfWeek.Wednesday),
                        RangeStrengthWorkout("Overhead Press", 3, 5, 5, "Shoulders", DayOfWeek.Friday, targetRpe: 8.5, targetRestRange: "2-4 min"),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Cable Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Reverse Pec Deck Fly", 12, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Face Pull", 12, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Close-Grip Bench Press", 6, 3, "Arms", DayOfWeek.Saturday, targetRpe: 8.5, targetRestRange: "2-3 min"),
                        StrengthWorkout("EZ-Bar Curl", 8, 3, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Overhead Triceps Extension", 10, 2, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Hammer Curl", 10, 2, "Arms", DayOfWeek.Saturday),
                        TimedStrengthWorkout("Suitcase Carry", 40, 3, "Core", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Hanging Leg Raise", 8, 3, "Core", DayOfWeek.Saturday, "Studio")),
                    Week(4,
                        StrengthWorkout("Dumbbell Bench Press", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Cable Triceps Pushdown", 15, 2, "Arms", DayOfWeek.Monday),
                        StrengthWorkout("Machine Chest Press", 12, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Overhead Triceps Extension", 15, 2, "Arms", DayOfWeek.Monday),
                        RangeStrengthWorkout("Cable Crossover", 15, 20, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Cable Curl", 15, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Hammer Curl", 12, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Chest-Supported Row", 12, 2, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Leg Press", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Reverse Lunge", 10, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Extension", 15, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Hamstring Curl", 15, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Arnold Press", 12, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Lateral Raise", 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Fly", 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Cable Triceps Pushdown", 15, 2, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Overhead Triceps Extension", 15, 2, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Cable Curl", 15, 2, "Arms", DayOfWeek.Saturday),
                        StrengthWorkout("Hammer Curl", 12, 2, "Arms", DayOfWeek.Saturday),
                        TimedStrengthWorkout("Plank", 35, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Russian Twists", 16, 2, "Core", DayOfWeek.Saturday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "At-Home Strength Builder",
                Description = "An 8-week no-gym-required strength plan built around bodyweight work, a sturdy chair or step, and an optional light resistance band. Four weekly sessions rotate pushing, rowing, squatting, lunging, hinging, glute, shoulder, and core patterns so home training still feels balanced and progressive.",
                Category = "At-Home Strength",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        RangeStrengthWorkout("Push-Up", 8, 12, 3, "Chest", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Resistance Band Row", 12, 3, "Back", DayOfWeek.Monday, "Home"),
                        RangeStrengthWorkout("Pike Push-Up", 6, 8, 3, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 15, 2, "Shoulders", DayOfWeek.Monday, "Home"),
                        RangeStrengthWorkout("Diamond Push-Ups", 8, 10, 2, "Arms", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Bodyweight Squat", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Reverse Lunge", 10, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Glute Bridge", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 12, 2, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        TimedStrengthWorkout("Plank", 30, 2, "Core", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Incline Push-Up", 12, 3, "Chest", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Resistance Band Row", 15, 3, "Back", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Pike Push-Up", 6, 2, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 15, 3, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Step-Up", 10, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Supported Split Squat", 8, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Glute Bridge", 12, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 15, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 15, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Plank Knee Drive", 10, 2, "Core", DayOfWeek.Saturday, "Home")),
                    Week(2,
                        StrengthWorkout("Push-Up", 10, 4, "Chest", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Resistance Band Row", 12, 4, "Back", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Pike Push-Up", 8, 3, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 15, 3, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Diamond Push-Ups", 10, 2, "Arms", DayOfWeek.Monday, "Home"),
                        TimedStrengthWorkout("Plank", 35, 2, "Core", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Bodyweight Squat", 12, 4, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Walking Lunge", 10, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Glute Bridge", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Step-Up", 8, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Incline Push-Up", 12, 3, "Chest", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Resistance Band Row", 15, 4, "Back", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 20, 3, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Diamond Push-Ups", 8, 2, "Arms", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Pallof Press", 10, 3, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Supported Split Squat", 10, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 15, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Reverse Lunge", 10, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Glute Bridge", 20, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 20, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Plank Knee Drive", 12, 2, "Core", DayOfWeek.Saturday, "Home")),
                    Week(3,
                        RangeStrengthWorkout("Push-Up", 6, 10, 4, "Chest", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Resistance Band Row", 10, 4, "Back", DayOfWeek.Monday, "Home"),
                        RangeStrengthWorkout("Pike Push-Up", 6, 8, 4, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 15, 3, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Diamond Push-Ups", 8, 3, "Arms", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Pallof Press", 12, 2, "Core", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Bodyweight Squat", 15, 4, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Step-Up", 10, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Reverse Lunge", 12, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Glute Bridge", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 15, 3, "Legs", DayOfWeek.Tuesday, "Home"),
                        TimedStrengthWorkout("Plank", 40, 3, "Core", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Push-Up", 8, 3, "Chest", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Resistance Band Row", 12, 4, "Back", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Pike Push-Up", 8, 3, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 20, 3, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Diamond Push-Ups", 10, 2, "Arms", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Dead Bug", 12, 2, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Supported Split Squat", 10, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Walking Lunge", 12, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Glute Bridge", 20, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 15, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 20, 3, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Bird Dog", 10, 2, "Core", DayOfWeek.Saturday, "Home")),
                    Week(4,
                        StrengthWorkout("Incline Push-Up", 10, 2, "Chest", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Band Pull-Apart", 15, 2, "Shoulders", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Monday, "Home"),
                        StrengthWorkout("Bodyweight Squat", 12, 2, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Glute Bridge", 12, 2, "Legs", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Standing Calf Raise", 15, 2, "Legs", DayOfWeek.Tuesday, "Home"),
                        TimedStrengthWorkout("Plank", 30, 2, "Core", DayOfWeek.Tuesday, "Home"),
                        StrengthWorkout("Push-Up", 8, 2, "Chest", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Pike Push-Up", 6, 2, "Shoulders", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Thursday, "Home"),
                        StrengthWorkout("Reverse Lunge", 8, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Bodyweight Good Morning", 12, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Supported Split Squat", 8, 2, "Legs", DayOfWeek.Saturday, "Home"),
                        StrengthWorkout("Plank Knee Drive", 10, 2, "Core", DayOfWeek.Saturday, "Home"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Brisk Walking Starter",
                Description = "A 6-week walking plan that gradually increases time, distance, and weekly frequency. Light strength and core support sessions are mixed in so the plan feels more complete without losing its walking-first focus.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 6,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Friday),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Optional Recovery Walk", 20, 1.0, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(2,
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Supported Split Squat", 8, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Friday),
                        StrengthWorkout("Sit-to-Stand", 12, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Brisk Walk", 24, 1.2, "Cardio", DayOfWeek.Saturday)),
                    Week(3,
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Tuesday),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 12, 2, "Chest", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Wednesday, "Studio"),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Saturday),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Saturday, "Studio")),
                    Week(4,
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Supported Split Squat", 8, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Friday),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 22, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(5,
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Tuesday),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 12, 2, "Chest", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 15, 2, "Back", DayOfWeek.Wednesday, "Studio"),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Saturday),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Saturday, "Studio")),
                    Week(6,
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Tuesday),
                        StrengthWorkout("Supported Split Squat", 10, 2, "Legs", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 15, 2, "Back", DayOfWeek.Wednesday, "Studio"),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Friday),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Saturday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Interval Conditioning Builder",
                Description = "An 8-week conditioning plan that alternates interval days, low-impact recovery cardio, and basic strength support. The sessions progress from short repeats to longer intervals, while support days train legs, pushing, pulling, and trunk stability.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Bike Intervals", 20, 6.0, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Bodyweight Squat", 12, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Incline Push-Up", 10, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Reverse Lunge", 10, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 3, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Plank Knee Drive", 10, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Treadmill Intervals", 22, 2.0, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Saturday),
                        CardioWorkout("Easy Bike Ride", 18, 3.8, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(2,
                        CardioWorkout("Bike Intervals", 24, 7.0, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Dumbbell Floor Press", 10, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 3, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Rowing Intervals", 20, 2.5, "Cardio", DayOfWeek.Thursday, "Cardio Area"),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Saturday),
                        CardioWorkout("Easy Bike Ride", 20, 4.2, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(3,
                        CardioWorkout("Bike Intervals", 26, 7.5, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Bodyweight Squat", 15, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Incline Push-Up", 12, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Walking Lunge", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Resistance Band Row", 15, 3, "Back", DayOfWeek.Tuesday, "Studio"),
                        TimedStrengthWorkout("Farmer Carry", 45, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Treadmill Intervals", 26, 2.4, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Recovery Walk", 30, 1.5, "Cardio", DayOfWeek.Saturday),
                        CardioWorkout("Easy Bike Ride", 22, 4.5, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(4,
                        CardioWorkout("Bike Intervals", 18, 5.5, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 10, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday),
                        StrengthWorkout("Reverse Lunge", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Elliptical Intervals", 22, 2.0, "Cardio", DayOfWeek.Thursday, "Cardio Area"),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Saturday),
                        CardioWorkout("Easy Bike Ride", 18, 3.6, "Cardio", DayOfWeek.Sunday, "Cardio Area"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Couch to 5K Starter",
                Description = "A 9-week run-walk progression for beginners. Each week changes the run and walk demands so users steadily build toward continuous running, and simple support strength work helps prepare the legs and trunk for more running volume.",
                Category = "Running",
                DurationInWeeks = 9,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Run-Walk Session", 20, 1.5, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 20, 1.5, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 22, 1.7, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Dead Bug", 8, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Optional Recovery Walk", 20, 1.0, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(2,
                        CardioWorkout("Run-Walk Session", 22, 1.7, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 24, 1.8, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 24, 1.8, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Optional Recovery Walk", 20, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(3,
                        CardioWorkout("Run-Walk Session", 25, 1.9, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 25, 1.9, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 27, 2.0, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 22, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(4,
                        CardioWorkout("Run-Walk Session", 28, 2.1, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 28, 2.2, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 28, 2.2, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Bodyweight Squat", 12, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 22, 1.2, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(5,
                        CardioWorkout("Run-Walk Session", 30, 2.3, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 32, 2.5, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 32, 2.5, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 24, 1.2, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(6,
                        CardioWorkout("Run Session", 32, 2.6, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 30, 2.4, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 34, 2.8, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Bodyweight Squat", 12, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(7,
                        CardioWorkout("Run Session", 35, 2.9, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run Session", 35, 2.9, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 36, 3.0, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(8,
                        CardioWorkout("Run Session", 38, 3.1, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run Session", 38, 3.1, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 40, 3.2, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Bodyweight Squat", 12, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Recovery Walk", 28, 1.4, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(9,
                        CardioWorkout("Continuous Run", 40, 3.2, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Continuous Run", 42, 3.3, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Continuous Run", 45, 3.5, "Cardio", DayOfWeek.Friday, "Track"),
                        StrengthWorkout("Step-Up", 12, 2, "Legs", DayOfWeek.Saturday, "Studio"),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Saturday, "Studio"),
                        CardioWorkout("Optional Recovery Walk", 28, 1.4, "Cardio", DayOfWeek.Sunday, "Outdoor Trail"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Active Aging Strength & Balance",
                Description = "An 8-week lower-impact plan that pairs walking, basic strength work, and balance practice. The weekly templates vary balance drills, sit-to-stand patterns, carries, calf work, and cardio options instead of locking older adults into one static week.",
                Category = "Active Aging",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Brisk Walk", 25, 1.2, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Standing Calf Raise", 12, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Single-Leg Balance Hold", 30, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Farmer Carry", 40, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 25, 1.2, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 20, 4.0, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(2,
                        CardioWorkout("Brisk Walk", 28, 1.3, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 12, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Incline Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Standing Calf Raise", 12, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Supported Split Squat", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Tandem Stance Hold", 30, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Suitcase Carry", 35, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 28, 1.3, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 22, 4.5, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(3,
                        CardioWorkout("Brisk Walk", 30, 1.4, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 12, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 15, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Standing Calf Raise", 15, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Heel-to-Toe Walk", 4, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Farmer Carry", 45, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 30, 1.4, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 25, 5.0, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(4,
                        CardioWorkout("Brisk Walk", 24, 1.1, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Standing Calf Raise", 12, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Tandem Stance Hold", 30, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        TimedStrengthWorkout("Suitcase Carry", 40, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 24, 1.1, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 18, 3.8, "Cardio", DayOfWeek.Sunday, "Cardio Area"))),
                IsCustom = false
            });

            MigrateLegacyJsonIfNeeded();
            LoadCustomPlans();
        }

        private static List<Workout> CreatePlanWorkouts(params IEnumerable<Workout>[] weeklyTemplates)
        {
            var workouts = weeklyTemplates.SelectMany(template => template).ToList();
            ApplySmartPlanMuscleGroups(workouts);
            return workouts;
        }

        private static IEnumerable<Workout> Week(int weekNumber, params Workout[] workouts)
        {
            foreach (var workout in workouts)
            {
                workout.PlanWeekNumber = weekNumber;
                if (!workout.TargetRpe.HasValue)
                {
                    workout.TargetRpe = GetSuggestedTargetRpe(workout);
                }

                if (string.IsNullOrWhiteSpace(workout.TargetRestRange))
                {
                    workout.TargetRestRange = GetSuggestedTargetRestRange(workout);
                }
                yield return workout;
            }
        }

        private static Workout StrengthWorkout(string name, int reps, int sets, string muscleGroup, DayOfWeek day, string gymLocation = "", double? targetRpe = null, string targetRestRange = "")
            => new(name, 0, reps, sets, NormalizePlanMuscleGroup(name, muscleGroup, WorkoutType.WeightLifting), day, DateTime.Now, WorkoutType.WeightLifting, gymLocation)
            {
                TargetRpe = targetRpe,
                TargetRestRange = targetRestRange
            };

        private static Workout RangeStrengthWorkout(string name, int minReps, int maxReps, int sets, string muscleGroup, DayOfWeek day, string gymLocation = "", double? targetRpe = null, string targetRestRange = "")
            => new(name, 0, maxReps <= 5 ? minReps : maxReps, sets, NormalizePlanMuscleGroup(name, muscleGroup, WorkoutType.WeightLifting), day, DateTime.Now, WorkoutType.WeightLifting, gymLocation)
            {
                MinReps = minReps,
                MaxReps = maxReps,
                TargetRpe = targetRpe,
                TargetRestRange = targetRestRange
            };

        private static Workout TimedStrengthWorkout(string name, int durationSeconds, int sets, string muscleGroup, DayOfWeek day, string gymLocation = "", double? targetRpe = null, string targetRestRange = "")
            => new(name, 0, 0, sets, NormalizePlanMuscleGroup(name, muscleGroup, WorkoutType.WeightLifting), day, DateTime.Now, WorkoutType.WeightLifting, gymLocation)
            {
                DurationSeconds = durationSeconds,
                TargetRpe = targetRpe,
                TargetRestRange = targetRestRange
            };

        private static Workout CardioWorkout(string name, int durationMinutes, double distanceMiles, string muscleGroup, DayOfWeek day, string gymLocation = "", int steps = 0)
            => new(name, 0, 0, 0, muscleGroup, day, DateTime.Now, WorkoutType.Cardio, gymLocation)
            {
                DurationMinutes = durationMinutes,
                DistanceMiles = distanceMiles,
                Steps = steps
            };

        private static string NormalizePlanMuscleGroup(string exerciseName, string muscleGroup, WorkoutType type)
        {
            if (type != WorkoutType.WeightLifting || !string.Equals(muscleGroup, "Arms", StringComparison.OrdinalIgnoreCase))
            {
                return muscleGroup;
            }

            var normalizedName = exerciseName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return muscleGroup;
            }

            if (normalizedName.Contains("curl", StringComparison.OrdinalIgnoreCase))
            {
                return "Biceps";
            }

            if (normalizedName.Contains("triceps", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Contains("skull crusher", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Contains("close-grip", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Contains("diamond push-up", StringComparison.OrdinalIgnoreCase))
            {
                return "Triceps";
            }

            return muscleGroup;
        }

        private static void ApplySmartPlanMuscleGroups(IEnumerable<Workout> workouts)
        {
            var strengthWorkouts = workouts
                .Where(workout => workout.Type == WorkoutType.WeightLifting)
                .ToList();

            foreach (var workout in strengthWorkouts)
            {
                workout.MuscleGroup = NormalizePlanMuscleGroup(workout.Name, workout.MuscleGroup, workout.Type);
            }

            foreach (var dayGroup in strengthWorkouts.GroupBy(workout => new { workout.PlanWeekNumber, workout.Day }))
            {
                var dayWorkouts = dayGroup.ToList();
                foreach (var workout in dayWorkouts)
                {
                    workout.MuscleGroup = ResolveContextualMuscleGroup(workout, dayWorkouts);
                }
            }
        }

        private static string ResolveContextualMuscleGroup(Workout workout, IReadOnlyCollection<Workout> dayWorkouts)
        {
            var workoutName = workout.Name.Trim();
            if (!ContextualMuscleGroupCandidates.TryGetValue(workoutName, out var candidateGroups) ||
                candidateGroups.Length == 0)
            {
                return workout.MuscleGroup;
            }

            var scores = candidateGroups.ToDictionary(group => group, _ => 0, StringComparer.OrdinalIgnoreCase);

            foreach (var otherWorkout in dayWorkouts)
            {
                if (ReferenceEquals(otherWorkout, workout))
                {
                    continue;
                }

                foreach (var candidateGroup in candidateGroups)
                {
                    if (string.Equals(otherWorkout.MuscleGroup, candidateGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        scores[candidateGroup]++;
                    }
                }
            }

            var maxScore = scores.Values.DefaultIfEmpty(0).Max();
            if (maxScore <= 0)
            {
                return workout.MuscleGroup;
            }

            var topCandidates = scores
                .Where(pair => pair.Value == maxScore)
                .Select(pair => pair.Key)
                .ToList();

            return topCandidates.Count == 1
                ? topCandidates[0]
                : workout.MuscleGroup;
        }

        private static double? GetSuggestedTargetRpe(Workout workout)
        {
            if (workout.Type == WorkoutType.Cardio)
            {
                var cardioName = workout.Name.ToLowerInvariant();
                if (cardioName.Contains("optional") || cardioName.Contains("recovery") || cardioName.Contains("easy"))
                {
                    return 4.5;
                }

                if (cardioName.Contains("interval"))
                {
                    return 8;
                }

                if (cardioName.Contains("continuous run") || cardioName.Contains("run session"))
                {
                    return 7.5;
                }

                if (cardioName.Contains("run-walk"))
                {
                    return 6.5;
                }

                if (cardioName.Contains("brisk walk"))
                {
                    return 5.5;
                }

                return 6;
            }

            var lowerName = workout.Name.ToLowerInvariant();
            if (workout.HasTimedTarget)
            {
                return ApplyWeekPhaseAdjustment(lowerName.Contains("carry") ? 7 : 6.5, workout.PlanWeekNumber);
            }

            if (IsLowIntensitySupportLift(lowerName))
            {
                return ApplyWeekPhaseAdjustment(6.5, workout.PlanWeekNumber);
            }

            var targetReps = workout.HasRepRange
                ? workout.MaxReps!.Value
                : workout.Reps;

            double baseRpe;
            if (IsPrimaryCompoundLift(lowerName))
            {
                baseRpe = targetReps switch
                {
                    <= 3 => 9,
                    <= 5 => 8.5,
                    <= 8 => 8,
                    <= 12 => 7.5,
                    _ => 7
                };
            }
            else if (IsAccessoryLift(lowerName))
            {
                baseRpe = targetReps switch
                {
                    <= 6 => 8,
                    <= 10 => 7.5,
                    <= 15 => 7,
                    _ => 6.5
                };
            }
            else
            {
                baseRpe = targetReps switch
                {
                    <= 3 => 8.5,
                    <= 5 => 8,
                    <= 8 => 7.5,
                    <= 12 => 7.5,
                    _ => 7
                };
            }

            return ApplyWeekPhaseAdjustment(baseRpe, workout.PlanWeekNumber);
        }

        private static string GetSuggestedTargetRestRange(Workout workout)
        {
            if (workout.Type == WorkoutType.Cardio)
            {
                return string.Empty;
            }

            var lowerName = workout.Name.ToLowerInvariant();
            if (workout.HasTimedTarget)
            {
                return lowerName.Contains("carry") ? "1-2 min" : "45-60 sec";
            }

            if (IsLowIntensitySupportLift(lowerName))
            {
                return "1 min";
            }

            if (lowerName.Contains("carry"))
            {
                return "1 min";
            }

            var targetReps = workout.HasRepRange
                ? workout.MaxReps!.Value
                : workout.Reps;

            if (IsPrimaryCompoundLift(lowerName))
            {
                if (targetReps <= 3)
                {
                    return "3-5 min";
                }

                if (targetReps <= 5)
                {
                    return "2-4 min";
                }

                if (targetReps <= 8)
                {
                    return "2-3 min";
                }

                if (targetReps <= 12)
                {
                    return "1-2 min";
                }

                return "1 min";
            }

            if (IsAccessoryLift(lowerName))
            {
                return targetReps <= 8 ? "1-2 min" : "1 min";
            }

            if (targetReps <= 5)
            {
                return "2 min";
            }

            if (targetReps <= 10)
            {
                return "1-2 min";
            }

            return "1 min";
        }

        private static bool IsPrimaryCompoundLift(string lowerName)
            => lowerName.Contains("bench") ||
               lowerName.Contains("squat") ||
               lowerName.Contains("deadlift") ||
               lowerName.Contains("pull-up") ||
               lowerName.Contains("pull up") ||
               lowerName.Contains("chin-up") ||
               lowerName.Contains("chin up") ||
               lowerName.Contains("row") ||
               lowerName.Contains("shoulder press") ||
               lowerName.Contains("overhead press") ||
               lowerName.Contains("push press") ||
               lowerName.Contains("machine chest press") ||
               lowerName.Contains("chest press") ||
               lowerName.Contains("lat pulldown") ||
               lowerName.Contains("leg press") ||
               lowerName.Contains("hack squat") ||
               lowerName.Contains("split squat") ||
               lowerName.Contains("lunge") ||
               lowerName.Contains("hip thrust") ||
               lowerName.Contains("dip");

        private static bool IsAccessoryLift(string lowerName)
            => lowerName.Contains("curl") ||
               lowerName.Contains("raise") ||
               lowerName.Contains("fly") ||
               lowerName.Contains("pushdown") ||
               lowerName.Contains("extension") ||
               lowerName.Contains("calf") ||
               lowerName.Contains("leg extension") ||
               lowerName.Contains("leg curl") ||
               lowerName.Contains("hamstring curl") ||
               lowerName.Contains("face pull");

        private static bool IsLowIntensitySupportLift(string lowerName)
            => lowerName.Contains("balance hold") ||
               lowerName.Contains("tandem stance") ||
               lowerName.Contains("heel-to-toe") ||
               lowerName.Contains("plank") ||
               lowerName.Contains("bird dog") ||
               lowerName.Contains("dead bug") ||
               lowerName.Contains("pallof") ||
               lowerName.Contains("sit-to-stand") ||
               lowerName.Contains("wall push-up") ||
               lowerName.Contains("incline push-up") ||
               lowerName.Contains("elevated push-up") ||
               lowerName.Contains("step-up") ||
               lowerName.Contains("bodyweight squat") ||
               lowerName.Contains("supported split squat");

        private static double ApplyWeekPhaseAdjustment(double baseRpe, int? planWeekNumber)
        {
            if (planWeekNumber == 4)
            {
                return Math.Max(5.5, baseRpe - 0.5);
            }

            return baseRpe;
        }

        public IEnumerable<WorkoutPlan> GetWorkoutPlans() => _plans;

        public void AddWorkoutPlan(WorkoutPlan plan)
        {
            ApplySmartPlanMuscleGroups(plan.Workouts);
            _plans.Add(plan);
            SavePlans();
        }

        public void SavePlans()
        {
            using var connection = _database.CreateConnection();
            using var transaction = connection.BeginTransaction();

            ExecuteNonQuery(connection, transaction, "DELETE FROM CustomWorkoutPlanWorkouts;");
            ExecuteNonQuery(connection, transaction, "DELETE FROM CustomWorkoutPlans;");

            foreach (var plan in _plans.Where(plan => plan.IsCustom))
            {
                using var planCommand = connection.CreateCommand();
                planCommand.Transaction = transaction;
                planCommand.CommandText =
                    """
                    INSERT INTO CustomWorkoutPlans (Name, Description, Category, DurationInWeeks, IsCustom)
                    VALUES ($name, $description, $category, $durationInWeeks, $isCustom);
                    """;
                planCommand.Parameters.AddWithValue("$name", plan.Name);
                planCommand.Parameters.AddWithValue("$description", plan.Description ?? string.Empty);
                planCommand.Parameters.AddWithValue("$category", plan.Category ?? "General");
                planCommand.Parameters.AddWithValue("$durationInWeeks", plan.DurationInWeeks);
                planCommand.Parameters.AddWithValue("$isCustom", 1);
                planCommand.ExecuteNonQuery();

                foreach (var workout in plan.Workouts)
                {
                    InsertWorkout(connection, transaction, "CustomWorkoutPlanWorkouts", workout, plan.Name);
                }
            }

            transaction.Commit();
        }

        private void LoadCustomPlans()
        {
            using var connection = _database.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT p.Name, p.Description, p.Category, p.DurationInWeeks, p.IsCustom,
                       w.Name, w.PlannedExerciseName, w.MuscleGroup, w.GymLocation, w.Weight, w.Reps, w.Sets, w.MinReps, w.MaxReps, w.TargetRpe, w.TargetRestRange,
                       w.StartTime, w.EndTime, w.Steps, w.DurationMinutes, w.DurationSeconds, w.DistanceMiles,
                       w.Type, w.Day, w.PlanWeekNumber, w.IsWarmup
                FROM CustomWorkoutPlans p
                LEFT JOIN CustomWorkoutPlanWorkouts w ON w.PlanName = p.Name
                ORDER BY p.Name, w.Id;
                """;

            using var reader = command.ExecuteReader();
            var plansByName = new Dictionary<string, WorkoutPlan>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var planName = reader.GetString(0);
                if (!plansByName.TryGetValue(planName, out var plan))
                {
                    plan = new WorkoutPlan
                    {
                        Name = planName,
                        Description = reader.GetString(1),
                        Category = reader.GetString(2),
                        DurationInWeeks = reader.GetInt32(3),
                        IsCustom = reader.GetInt64(4) == 1
                    };
                    plansByName.Add(planName, plan);
                    _plans.Add(plan);
                }

                if (!reader.IsDBNull(5))
                {
                    plan.Workouts.Add(ReadWorkout(reader, 5));
                }
            }

            foreach (var plan in plansByName.Values)
            {
                ApplySmartPlanMuscleGroups(plan.Workouts);
            }
        }

        private void MigrateLegacyJsonIfNeeded()
        {
            if (GetCustomPlanCount() > 0 || !File.Exists(_legacyCustomPlansFilePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_legacyCustomPlansFilePath);
                var savedPlans = JsonSerializer.Deserialize<List<WorkoutPlan>>(json, JsonSerializerOptions) ?? [];

                foreach (var plan in savedPlans.Where(plan => plan.IsCustom && !string.IsNullOrWhiteSpace(plan.Name)))
                {
                    ApplySmartPlanMuscleGroups(plan.Workouts);
                    _plans.Add(plan);
                }

                SavePlans();
                File.Delete(_legacyCustomPlansFilePath);
                _plans.RemoveAll(plan => plan.IsCustom);
            }
            catch
            {
                // Keep built-in plans available even if old JSON cannot be migrated.
            }
        }

        private int GetCustomPlanCount()
        {
            using var connection = _database.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CustomWorkoutPlans;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction transaction, string commandText)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }

        private static void InsertWorkout(SqliteConnection connection, SqliteTransaction transaction, string tableName, Workout workout, string? planName = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = tableName == "CustomWorkoutPlanWorkouts"
                ? $"""
                   INSERT INTO {tableName}
                   (PlanName, Name, PlannedExerciseName, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup)
                   VALUES ($planName, $name, $plannedExerciseName, $muscleGroup, $gymLocation, $weight, $reps, $sets, $minReps, $maxReps, $targetRpe, $targetRestRange, $startTime, $endTime, $steps, $durationMinutes, $durationSeconds, $distanceMiles, $type, $day, $planWeekNumber, $isWarmup);
                   """
                : $"""
                   INSERT INTO {tableName}
                   (Name, PlannedExerciseName, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup)
                   VALUES ($name, $plannedExerciseName, $muscleGroup, $gymLocation, $weight, $reps, $sets, $minReps, $maxReps, $targetRpe, $targetRestRange, $startTime, $endTime, $steps, $durationMinutes, $durationSeconds, $distanceMiles, $type, $day, $planWeekNumber, $isWarmup);
                   """;

            if (planName != null)
            {
                command.Parameters.AddWithValue("$planName", planName);
            }

            AddWorkoutParameters(command, workout);
            command.ExecuteNonQuery();
        }

        internal static void AddWorkoutParameters(SqliteCommand command, Workout workout)
        {
            command.Parameters.AddWithValue("$name", workout.Name);
            command.Parameters.AddWithValue("$plannedExerciseName", workout.PlannedExerciseName ?? string.Empty);
            command.Parameters.AddWithValue("$muscleGroup", workout.MuscleGroup);
            command.Parameters.AddWithValue("$gymLocation", workout.GymLocation);
            command.Parameters.AddWithValue("$weight", workout.Weight);
            command.Parameters.AddWithValue("$reps", workout.Reps);
            command.Parameters.AddWithValue("$sets", workout.Sets);
            command.Parameters.AddWithValue("$minReps", workout.MinReps.HasValue ? workout.MinReps.Value : DBNull.Value);
            command.Parameters.AddWithValue("$maxReps", workout.MaxReps.HasValue ? workout.MaxReps.Value : DBNull.Value);
            command.Parameters.AddWithValue("$targetRpe", workout.TargetRpe.HasValue ? workout.TargetRpe.Value : DBNull.Value);
            command.Parameters.AddWithValue("$targetRestRange", workout.TargetRestRange ?? string.Empty);
            command.Parameters.AddWithValue("$startTime", workout.StartTime.ToString("O"));
            command.Parameters.AddWithValue("$endTime", workout.EndTime.ToString("O"));
            command.Parameters.AddWithValue("$steps", workout.Steps);
            command.Parameters.AddWithValue("$durationMinutes", workout.DurationMinutes);
            command.Parameters.AddWithValue("$durationSeconds", workout.DurationSeconds);
            command.Parameters.AddWithValue("$distanceMiles", workout.DistanceMiles);
            command.Parameters.AddWithValue("$type", (int)workout.Type);
            command.Parameters.AddWithValue("$day", (int)workout.Day);
            command.Parameters.AddWithValue("$planWeekNumber", workout.PlanWeekNumber.HasValue ? workout.PlanWeekNumber.Value : DBNull.Value);
            command.Parameters.AddWithValue("$isWarmup", workout.IsWarmup ? 1 : 0);
        }

        internal static Workout ReadWorkout(SqliteDataReader reader, int offset)
        {
            var name = reader.GetString(offset);
            var type = (WorkoutType)reader.GetInt32(offset + 17);
            var muscleGroup = NormalizePlanMuscleGroup(name, reader.GetString(offset + 2), type);

            return new Workout(
                name: name,
                weight: reader.GetDouble(offset + 4),
                reps: reader.GetInt32(offset + 5),
                sets: reader.GetInt32(offset + 6),
                muscleGroup: muscleGroup,
                day: (DayOfWeek)reader.GetInt32(offset + 18),
                startTime: DateTime.Parse(reader.GetString(offset + 11), null, DateTimeStyles.RoundtripKind),
                type: type,
                gymLocation: reader.GetString(offset + 3))
            {
                PlannedExerciseName = reader.IsDBNull(offset + 1) ? string.Empty : reader.GetString(offset + 1),
                MinReps = reader.IsDBNull(offset + 7) ? null : reader.GetInt32(offset + 7),
                MaxReps = reader.IsDBNull(offset + 8) ? null : reader.GetInt32(offset + 8),
                TargetRpe = reader.IsDBNull(offset + 9) ? null : reader.GetDouble(offset + 9),
                TargetRestRange = reader.IsDBNull(offset + 10) ? string.Empty : reader.GetString(offset + 10),
                EndTime = DateTime.Parse(reader.GetString(offset + 12), null, DateTimeStyles.RoundtripKind),
                Steps = reader.GetInt32(offset + 13),
                DurationMinutes = reader.GetInt32(offset + 14),
                DurationSeconds = reader.IsDBNull(offset + 15) ? 0 : reader.GetInt32(offset + 15),
                DistanceMiles = reader.GetDouble(offset + 16),
                PlanWeekNumber = reader.IsDBNull(offset + 19) ? null : reader.GetInt32(offset + 19),
                IsWarmup = !reader.IsDBNull(offset + 20) && reader.GetInt32(offset + 20) != 0
            };
        }
    }

    public interface IWorkoutPlanService
    {
        IEnumerable<WorkoutPlan> GetWorkoutPlans();
        void AddWorkoutPlan(WorkoutPlan plan);
        void SavePlans();
    }
}
