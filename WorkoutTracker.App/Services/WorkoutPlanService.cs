using System.Collections.Generic;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services
{
    public class WorkoutPlanService : IWorkoutPlanService
    {
        private readonly List<WorkoutPlan> _plans = new();

        public WorkoutPlanService()
        {
            _plans.Add(new WorkoutPlan
            {
                Name = "Beginner Full Body Foundation",
                Description = "A 3-day full-body strength plan for new lifters, built around nonconsecutive training days and major muscle groups.",
                Category = "Beginner Strength",
                DurationInWeeks = 8,
                Workouts = new List<Workout>
                {
                    StrengthWorkout("Goblet Squat", 10, 3, "Legs", DayOfWeek.Monday),
                    StrengthWorkout("Incline Push-Up", 10, 3, "Chest", DayOfWeek.Monday),
                    StrengthWorkout("Seated Cable Row", 10, 3, "Back", DayOfWeek.Monday),
                    StrengthWorkout("Dumbbell Romanian Deadlift", 10, 3, "Legs", DayOfWeek.Wednesday),
                    StrengthWorkout("Dumbbell Shoulder Press", 10, 3, "Shoulders", DayOfWeek.Wednesday),
                    StrengthWorkout("Lat Pulldown", 10, 3, "Back", DayOfWeek.Wednesday),
                    StrengthWorkout("Leg Press", 12, 2, "Legs", DayOfWeek.Friday),
                    StrengthWorkout("Chest Press Machine", 10, 2, "Chest", DayOfWeek.Friday),
                    StrengthWorkout("Dead Bug", 12, 2, "Core", DayOfWeek.Friday)
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Upper/Lower Strength Builder",
                Description = "A 4-day upper/lower split for intermediates who want more training frequency while keeping recovery manageable.",
                Category = "Strength Progression",
                DurationInWeeks = 8,
                Workouts = new List<Workout>
                {
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
                    StrengthWorkout("Plank", 3, 3, "Core", DayOfWeek.Friday)
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Push/Pull/Legs Hypertrophy",
                Description = "A 6-day push/pull/legs split for experienced lifters who want higher weekly volume for muscle-building.",
                Category = "Muscle Building",
                DurationInWeeks = 16,
                Workouts = new List<Workout>
                {
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
                    StrengthWorkout("Standing Calf Raise", 15, 3, "Legs", DayOfWeek.Saturday)
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Brisk Walking Starter",
                Description = "A simple cardio base plan built around five brisk walks to help users reach the weekly moderate-activity target.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 6,
                Workouts = new List<Workout>
                {
                    CardioWorkout("Brisk Walk", 1, 3500, "Cardio", DayOfWeek.Monday),
                    CardioWorkout("Brisk Walk", 1, 3500, "Cardio", DayOfWeek.Tuesday),
                    CardioWorkout("Brisk Walk", 1, 3500, "Cardio", DayOfWeek.Wednesday),
                    CardioWorkout("Brisk Walk", 1, 3500, "Cardio", DayOfWeek.Thursday),
                    CardioWorkout("Brisk Walk", 1, 3500, "Cardio", DayOfWeek.Friday)
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Interval Conditioning Builder",
                Description = "A beginner-friendly interval plan that mixes moderate cardio with short higher-effort bursts instead of jumping straight into all-out HIIT.",
                Category = "Fat Loss & Conditioning",
                DurationInWeeks = 8,
                Workouts = new List<Workout>
                {
                    CardioWorkout("Bike Intervals", 6, 4500, "Cardio", DayOfWeek.Monday),
                    StrengthWorkout("Bodyweight Squat", 12, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Elevated Push-Up", 10, 3, "Chest", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Backward Lunge", 10, 3, "Legs", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Plank Knee Drive", 12, 3, "Core", DayOfWeek.Tuesday, "Studio"),
                    CardioWorkout("Treadmill Intervals", 6, 5000, "Cardio", DayOfWeek.Thursday),
                    CardioWorkout("Recovery Walk", 1, 3000, "Cardio", DayOfWeek.Saturday)
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Couch to 5K Starter",
                Description = "A 3-day run-walk starter inspired by beginner 5K plans, with rest days between sessions to build consistency safely.",
                Category = "Running",
                DurationInWeeks = 9,
                Workouts = new List<Workout>
                {
                    CardioWorkout("Run-Walk Session", 1, 3200, "Cardio", DayOfWeek.Monday, "Track"),
                    CardioWorkout("Run-Walk Session", 1, 3400, "Cardio", DayOfWeek.Wednesday, "Track"),
                    CardioWorkout("Run-Walk Session", 1, 3600, "Cardio", DayOfWeek.Friday, "Track"),
                    CardioWorkout("Optional Recovery Walk", 1, 2500, "Cardio", DayOfWeek.Sunday, "Outdoor Trail")
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Active Aging Strength & Balance",
                Description = "A lower-impact plan for older adults focused on walking, full-body strength, and balance-supportive lower-body work.",
                Category = "Active Aging",
                DurationInWeeks = 8,
                Workouts = new List<Workout>
                {
                    CardioWorkout("Brisk Walk", 1, 3000, "Cardio", DayOfWeek.Monday, "Outdoor Trail"),
                    StrengthWorkout("Sit-to-Stand", 10, 2, "Legs", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Wall Push-Up", 10, 2, "Chest", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Resistance Band Row", 12, 2, "Back", DayOfWeek.Tuesday, "Studio"),
                    StrengthWorkout("Step-Up", 8, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                    StrengthWorkout("Single-Leg Balance Hold", 3, 2, "Legs", DayOfWeek.Thursday, "Studio"),
                    StrengthWorkout("Farmer Carry", 8, 2, "Core", DayOfWeek.Thursday, "Studio"),
                    CardioWorkout("Brisk Walk", 1, 3000, "Cardio", DayOfWeek.Friday, "Outdoor Trail"),
                    CardioWorkout("Easy Bike Ride", 1, 2800, "Cardio", DayOfWeek.Sunday, "Cardio Area")
                },
                IsCustom = false
            });
        }

        private static Workout StrengthWorkout(string name, int reps, int sets, string muscleGroup, DayOfWeek day, string gymLocation = "Main Gym")
            => new(name, 0, reps, sets, muscleGroup, day, DateTime.Now, WorkoutType.WeightLifting, gymLocation);

        private static Workout CardioWorkout(string name, int sessions, int steps, string muscleGroup, DayOfWeek day, string gymLocation = "Cardio Area")
            => new(name, 0, 0, sessions, muscleGroup, day, DateTime.Now, WorkoutType.Cardio, gymLocation)
            {
                Steps = steps
            };

        public IEnumerable<WorkoutPlan> GetWorkoutPlans() => _plans;

        public void AddWorkoutPlan(WorkoutPlan plan)
        {
            _plans.Add(plan);
        }
    }

    public interface IWorkoutPlanService
    {
        IEnumerable<WorkoutPlan> GetWorkoutPlans();
        void AddWorkoutPlan(WorkoutPlan plan);
    }
}
