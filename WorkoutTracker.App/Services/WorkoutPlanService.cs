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

        public WorkoutPlanService(string? databasePath = null)
        {
            _database = new WorkoutTrackerDatabase(databasePath);
            _legacyCustomPlansFilePath = Path.Combine(
                Path.GetDirectoryName(_database.DatabasePath) ?? string.Empty,
                "custom_workout_plans.json");

            _plans.Add(new WorkoutPlan
            {
                Name = "Beginner Full Body Foundation",
                Description = "An 8-week beginner strength plan with three nonconsecutive full-body days. Weeks rotate squat, hinge, push, pull, and core variations so new lifters build skill without repeating the exact same sessions every week.",
                Category = "Beginner Strength",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        StrengthWorkout("Goblet Squat", 10, 3, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Incline Push-Up", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Seated Cable Row", 10, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Hip Hinge Drill", 12, 2, "Legs", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Lat Pulldown", 10, 3, "Back", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Press", 12, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Chest Press Machine", 10, 2, "Chest", DayOfWeek.Friday),
                        StrengthWorkout("Dead Bug", 10, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(2,
                        StrengthWorkout("Box Squat", 8, 3, "Legs", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Dumbbell Floor Press", 10, 3, "Chest", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Half-Kneeling Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Wednesday, "Studio"),
                        StrengthWorkout("Assisted Pull-Up", 6, 3, "Back", DayOfWeek.Wednesday),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Push-Up", 8, 2, "Chest", DayOfWeek.Friday),
                        StrengthWorkout("Plank", 3, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(3,
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Machine Chest Press", 8, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 8, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 8, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 10, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Seated Cable Row", 8, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Farmer Carry", 6, 2, "Core", DayOfWeek.Friday, "Studio")),
                    Week(4,
                        StrengthWorkout("Goblet Squat", 12, 2, "Legs", DayOfWeek.Monday),
                        StrengthWorkout("Incline Push-Up", 12, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Lateral Raise", 12, 2, "Shoulders", DayOfWeek.Wednesday),
                        StrengthWorkout("Assisted Pull-Up", 5, 2, "Back", DayOfWeek.Wednesday),
                        StrengthWorkout("Step-Up", 10, 2, "Legs", DayOfWeek.Friday, "Studio"),
                        StrengthWorkout("Chest Press Machine", 10, 2, "Chest", DayOfWeek.Friday),
                        StrengthWorkout("Bird Dog", 8, 2, "Core", DayOfWeek.Friday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Upper/Lower Strength Builder",
                Description = "An 8-week upper/lower split for intermediates. The plan alternates heavy compounds, secondary variations, and lower-fatigue weeks so lifters can keep progressing while managing recovery.",
                Category = "Strength Progression",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        StrengthWorkout("Bench Press", 6, 4, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Barbell Row", 8, 4, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Overhead Press", 8, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Pull-Up", 6, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Back Squat", 5, 4, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Romanian Deadlift", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Walking Lunge", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Press", 8, 4, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Seated Cable Row", 10, 4, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Front Squat", 6, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hip Thrust", 8, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Leg Curl", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Plank", 3, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(2,
                        StrengthWorkout("Close-Grip Bench Press", 6, 4, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Chest-Supported Row", 8, 4, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 8, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Pause Back Squat", 4, 4, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Rear-Foot Elevated Split Squat", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Bench Press", 6, 4, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 4, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Trap Bar Deadlift", 5, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Dead Bug", 10, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(3,
                        StrengthWorkout("Bench Press", 5, 5, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Barbell Row", 6, 4, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Push Press", 5, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Weighted Pull-Up", 5, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Back Squat", 4, 5, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Romanian Deadlift", 6, 4, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Walking Lunge", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Calf Raise", 12, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Press", 8, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Seated Cable Row", 8, 4, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 10, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Front Squat", 5, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hip Thrust", 6, 4, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Leg Curl", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hanging Knee Raise", 10, 3, "Core", DayOfWeek.Friday, "Studio")),
                    Week(4,
                        StrengthWorkout("Dumbbell Bench Press", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Landmine Press", 10, 2, "Shoulders", DayOfWeek.Monday, "Studio"),
                        StrengthWorkout("Lat Pulldown", 10, 2, "Back", DayOfWeek.Monday),
                        StrengthWorkout("Front Squat", 6, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Hip Thrust", 8, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Reverse Lunge", 10, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Standing Calf Raise", 15, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 3, "Back", DayOfWeek.Thursday),
                        StrengthWorkout("Lateral Raise", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 2, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Hamstring Curl", 12, 2, "Legs", DayOfWeek.Friday),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Friday, "Studio"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Push/Pull/Legs Hypertrophy",
                Description = "A 16-week six-day hypertrophy split with rotating exercise variations and rep targets. Each four-week block changes angles, accessories, and fatigue demand instead of repeating the same exact push, pull, and leg days.",
                Category = "Muscle Building",
                DurationInWeeks = 16,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        StrengthWorkout("Barbell Bench Press", 8, 4, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Incline Dumbbell Press", 10, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Cable Chest Fly", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Lat Pulldown", 10, 4, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Chest-Supported Row", 10, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 12, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("EZ-Bar Curl", 12, 3, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Back Squat", 8, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Press", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 10, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Pull-Up", 8, 4, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 10, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 12, 3, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Front Squat", 8, 4, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Bulgarian Split Squat", 10, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Leg Curl", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Saturday)),
                    Week(2,
                        StrengthWorkout("Dumbbell Bench Press", 10, 4, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Incline Bench Press", 8, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Machine Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Pec Deck Fly", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Weighted Pull-Up", 6, 4, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Curl", 12, 3, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Hack Squat", 10, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Stiff-Leg Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 12, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Dips", 10, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 12, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Neutral-Grip Lat Pulldown", 10, 4, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Chest-Supported Row", 12, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Cable Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 10, 3, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Leg Press", 12, 4, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Rear-Foot Elevated Split Squat", 10, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hamstring Curl", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 4, "Legs", DayOfWeek.Saturday)),
                    Week(3,
                        StrengthWorkout("Barbell Bench Press", 6, 4, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Incline Dumbbell Press", 8, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Arnold Press", 10, 3, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Cable Chest Fly", 15, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Pull-Up", 6, 4, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("T-Bar Row", 8, 4, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 3, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("EZ-Bar Curl", 10, 3, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Back Squat", 6, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Romanian Deadlift", 8, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Leg Press", 10, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 12, 4, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 8, 4, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Seated Dumbbell Shoulder Press", 8, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 12, 3, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Overhead Triceps Extension", 10, 3, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Weighted Pull-Up", 5, 4, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Single-Arm Dumbbell Row", 8, 4, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Fly", 15, 3, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Cable Curl", 12, 3, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Front Squat", 6, 4, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Bulgarian Split Squat", 8, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Leg Curl", 10, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 12, 4, "Legs", DayOfWeek.Saturday)),
                    Week(4,
                        StrengthWorkout("Dumbbell Bench Press", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Machine Incline Press", 12, 3, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Machine Shoulder Press", 12, 2, "Shoulders", DayOfWeek.Monday),
                        StrengthWorkout("Cable Chest Fly", 15, 2, "Chest", DayOfWeek.Monday),
                        StrengthWorkout("Neutral-Grip Lat Pulldown", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Seated Cable Row", 12, 3, "Back", DayOfWeek.Tuesday),
                        StrengthWorkout("Face Pull", 15, 2, "Shoulders", DayOfWeek.Tuesday),
                        StrengthWorkout("Incline Dumbbell Curl", 12, 2, "Arms", DayOfWeek.Tuesday),
                        StrengthWorkout("Hack Squat", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Dumbbell Romanian Deadlift", 12, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Walking Lunge", 12, 2, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Seated Calf Raise", 15, 3, "Legs", DayOfWeek.Wednesday),
                        StrengthWorkout("Machine Chest Press", 12, 3, "Chest", DayOfWeek.Thursday),
                        StrengthWorkout("Arnold Press", 12, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Lateral Raise", 15, 2, "Shoulders", DayOfWeek.Thursday),
                        StrengthWorkout("Cable Triceps Pushdown", 15, 2, "Arms", DayOfWeek.Thursday),
                        StrengthWorkout("Lat Pulldown", 12, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Chest-Supported Row", 12, 3, "Back", DayOfWeek.Friday),
                        StrengthWorkout("Rear Delt Cable Fly", 15, 2, "Shoulders", DayOfWeek.Friday),
                        StrengthWorkout("Hammer Curl", 12, 2, "Arms", DayOfWeek.Friday),
                        StrengthWorkout("Leg Press", 12, 3, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Rear-Foot Elevated Split Squat", 10, 2, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Hamstring Curl", 12, 2, "Legs", DayOfWeek.Saturday),
                        StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Saturday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Brisk Walking Starter",
                Description = "A 6-week walking plan that gradually increases time, distance, and weekly frequency to help users build toward the recommended aerobic volume instead of repeating the same easy week.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 6,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Friday),
                        CardioWorkout("Optional Recovery Walk", 20, 1.0, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(2,
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Friday),
                        CardioWorkout("Brisk Walk", 24, 1.2, "Cardio", DayOfWeek.Saturday)),
                    Week(3,
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Tuesday),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Brisk Walk", 30, 1.5, "Cardio", DayOfWeek.Saturday)),
                    Week(4,
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 32, 1.6, "Cardio", DayOfWeek.Friday),
                        CardioWorkout("Recovery Walk", 22, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(5,
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Tuesday),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Brisk Walk", 35, 1.8, "Cardio", DayOfWeek.Saturday)),
                    Week(6,
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Monday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Tuesday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Wednesday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Friday),
                        CardioWorkout("Brisk Walk", 38, 2.0, "Cardio", DayOfWeek.Saturday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Interval Conditioning Builder",
                Description = "An 8-week conditioning plan that alternates interval days, low-impact recovery cardio, and basic strength support. The sessions progress from short repeats to longer intervals rather than staying the same each week.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Bike Intervals", 20, 6.0, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Bodyweight Squat", 12, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Elevated Push-Up", 10, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Backward Lunge", 10, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Plank Knee Drive", 10, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Treadmill Intervals", 22, 2.0, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Saturday)),
                    Week(2,
                        CardioWorkout("Bike Intervals", 24, 7.0, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Dumbbell Floor Press", 10, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Dead Bug", 10, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Rowing Intervals", 20, 2.5, "Cardio", DayOfWeek.Thursday, "Cardio Area"),
                        CardioWorkout("Brisk Walk", 28, 1.4, "Cardio", DayOfWeek.Saturday)),
                    Week(3,
                        CardioWorkout("Bike Intervals", 26, 7.5, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Bodyweight Squat", 15, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Elevated Push-Up", 12, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Walking Lunge", 10, 3, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Farmer Carry", 6, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Treadmill Intervals", 26, 2.4, "Cardio", DayOfWeek.Thursday),
                        CardioWorkout("Recovery Walk", 30, 1.5, "Cardio", DayOfWeek.Saturday)),
                    Week(4,
                        CardioWorkout("Bike Intervals", 18, 5.5, "Cardio", DayOfWeek.Monday),
                        StrengthWorkout("Goblet Squat", 10, 2, "Legs", DayOfWeek.Tuesday),
                        StrengthWorkout("Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday),
                        StrengthWorkout("Backward Lunge", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Pallof Press", 10, 2, "Core", DayOfWeek.Tuesday, "Studio"),
                        CardioWorkout("Elliptical Intervals", 22, 2.0, "Cardio", DayOfWeek.Thursday, "Cardio Area"),
                        CardioWorkout("Brisk Walk", 25, 1.3, "Cardio", DayOfWeek.Saturday))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Couch to 5K Starter",
                Description = "A 9-week run-walk progression for beginners. Each week changes the run and walk demands so users steadily build toward continuous running instead of repeating the same three sessions forever.",
                Category = "Running",
                DurationInWeeks = 9,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Run-Walk Session", 20, 1.5, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 20, 1.5, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 22, 1.7, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Optional Recovery Walk", 20, 1.0, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(2,
                        CardioWorkout("Run-Walk Session", 22, 1.7, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 24, 1.8, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 24, 1.8, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Optional Recovery Walk", 20, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(3,
                        CardioWorkout("Run-Walk Session", 25, 1.9, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 25, 1.9, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 27, 2.0, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 22, 1.1, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(4,
                        CardioWorkout("Run-Walk Session", 28, 2.1, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 28, 2.2, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 28, 2.2, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 22, 1.2, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(5,
                        CardioWorkout("Run-Walk Session", 30, 2.3, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 32, 2.5, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run-Walk Session", 32, 2.5, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 24, 1.2, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(6,
                        CardioWorkout("Run Session", 32, 2.6, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run-Walk Session", 30, 2.4, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 34, 2.8, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(7,
                        CardioWorkout("Run Session", 35, 2.9, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run Session", 35, 2.9, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 36, 3.0, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 25, 1.3, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(8,
                        CardioWorkout("Run Session", 38, 3.1, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Run Session", 38, 3.1, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Run Session", 40, 3.2, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Recovery Walk", 28, 1.4, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")),
                    Week(9,
                        CardioWorkout("Continuous Run", 40, 3.2, "Cardio", DayOfWeek.Monday, "Track"),
                        CardioWorkout("Continuous Run", 42, 3.3, "Cardio", DayOfWeek.Wednesday, "Track"),
                        CardioWorkout("Continuous Run", 45, 3.5, "Cardio", DayOfWeek.Friday, "Track"),
                        CardioWorkout("Optional Recovery Walk", 28, 1.4, "Cardio", DayOfWeek.Sunday, "Outdoor Trail"))),
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Active Aging Strength & Balance",
                Description = "An 8-week lower-impact plan that pairs walking, basic strength work, and balance practice. The weekly templates vary balance drills, sit-to-stand patterns, carries, and cardio options instead of locking older adults into one static week.",
                Category = "Active Aging",
                DurationInWeeks = 8,
                Workouts = CreatePlanWorkouts(
                    Week(1,
                        CardioWorkout("Brisk Walk", 25, 1.2, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Single-Leg Balance Hold", 3, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Farmer Carry", 6, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 25, 1.2, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 20, 4.0, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(2,
                        CardioWorkout("Brisk Walk", 28, 1.3, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 12, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Incline Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Supported Split Squat", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Tandem Stance Hold", 3, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Suitcase Carry", 6, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 28, 1.3, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 22, 4.5, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(3,
                        CardioWorkout("Brisk Walk", 30, 1.4, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 12, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 15, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Heel-to-Toe Walk", 4, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Farmer Carry", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 30, 1.4, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 25, 5.0, "Cardio", DayOfWeek.Sunday, "Cardio Area")),
                    Week(4,
                        CardioWorkout("Brisk Walk", 24, 1.1, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                        StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                        StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Tandem Stance Hold", 3, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                        StrengthWorkout("Pallof Press", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                        CardioWorkout("Brisk Walk", 24, 1.1, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                        CardioWorkout("Easy Bike Ride", 18, 3.8, "Cardio", DayOfWeek.Sunday, "Cardio Area"))),
                IsCustom = false
            });

            MigrateLegacyJsonIfNeeded();
            LoadCustomPlans();
        }

        private static List<Workout> CreatePlanWorkouts(params IEnumerable<Workout>[] weeklyTemplates)
            => weeklyTemplates.SelectMany(template => template).ToList();

        private static IEnumerable<Workout> Week(int weekNumber, params Workout[] workouts)
        {
            foreach (var workout in workouts)
            {
                workout.PlanWeekNumber = weekNumber;
                yield return workout;
            }
        }

        private static Workout StrengthWorkout(string name, int reps, int sets, string muscleGroup, DayOfWeek day, string gymLocation = "")
            => new(name, 0, reps, sets, muscleGroup, day, DateTime.Now, WorkoutType.WeightLifting, gymLocation);

        private static Workout CardioWorkout(string name, int durationMinutes, double distanceMiles, string muscleGroup, DayOfWeek day, string gymLocation = "", int steps = 0)
            => new(name, 0, 0, 0, muscleGroup, day, DateTime.Now, WorkoutType.Cardio, gymLocation)
            {
                DurationMinutes = durationMinutes,
                DistanceMiles = distanceMiles,
                Steps = steps
            };

        public IEnumerable<WorkoutPlan> GetWorkoutPlans() => _plans;

        public void AddWorkoutPlan(WorkoutPlan plan)
        {
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
                       w.Name, w.MuscleGroup, w.GymLocation, w.Weight, w.Reps, w.Sets,
                       w.StartTime, w.EndTime, w.Steps, w.DurationMinutes, w.DistanceMiles,
                       w.Type, w.Day, w.PlanWeekNumber
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
                   (PlanName, Name, MuscleGroup, GymLocation, Weight, Reps, Sets, StartTime, EndTime, Steps, DurationMinutes, DistanceMiles, Type, Day, PlanWeekNumber)
                   VALUES ($planName, $name, $muscleGroup, $gymLocation, $weight, $reps, $sets, $startTime, $endTime, $steps, $durationMinutes, $distanceMiles, $type, $day, $planWeekNumber);
                   """
                : $"""
                   INSERT INTO {tableName}
                   (Name, MuscleGroup, GymLocation, Weight, Reps, Sets, StartTime, EndTime, Steps, DurationMinutes, DistanceMiles, Type, Day, PlanWeekNumber)
                   VALUES ($name, $muscleGroup, $gymLocation, $weight, $reps, $sets, $startTime, $endTime, $steps, $durationMinutes, $distanceMiles, $type, $day, $planWeekNumber);
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
            command.Parameters.AddWithValue("$muscleGroup", workout.MuscleGroup);
            command.Parameters.AddWithValue("$gymLocation", workout.GymLocation);
            command.Parameters.AddWithValue("$weight", workout.Weight);
            command.Parameters.AddWithValue("$reps", workout.Reps);
            command.Parameters.AddWithValue("$sets", workout.Sets);
            command.Parameters.AddWithValue("$startTime", workout.StartTime.ToString("O"));
            command.Parameters.AddWithValue("$endTime", workout.EndTime.ToString("O"));
            command.Parameters.AddWithValue("$steps", workout.Steps);
            command.Parameters.AddWithValue("$durationMinutes", workout.DurationMinutes);
            command.Parameters.AddWithValue("$distanceMiles", workout.DistanceMiles);
            command.Parameters.AddWithValue("$type", (int)workout.Type);
            command.Parameters.AddWithValue("$day", (int)workout.Day);
            command.Parameters.AddWithValue("$planWeekNumber", workout.PlanWeekNumber.HasValue ? workout.PlanWeekNumber.Value : DBNull.Value);
        }

        internal static Workout ReadWorkout(SqliteDataReader reader, int offset)
        {
            return new Workout(
                name: reader.GetString(offset),
                weight: reader.GetDouble(offset + 3),
                reps: reader.GetInt32(offset + 4),
                sets: reader.GetInt32(offset + 5),
                muscleGroup: reader.GetString(offset + 1),
                day: (DayOfWeek)reader.GetInt32(offset + 12),
                startTime: DateTime.Parse(reader.GetString(offset + 6), null, DateTimeStyles.RoundtripKind),
                type: (WorkoutType)reader.GetInt32(offset + 11),
                gymLocation: reader.GetString(offset + 2))
            {
                EndTime = DateTime.Parse(reader.GetString(offset + 7), null, DateTimeStyles.RoundtripKind),
                Steps = reader.GetInt32(offset + 8),
                DurationMinutes = reader.GetInt32(offset + 9),
                DistanceMiles = reader.GetDouble(offset + 10),
                PlanWeekNumber = reader.IsDBNull(offset + 13) ? null : reader.GetInt32(offset + 13)
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
