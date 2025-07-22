using System.Collections.Generic;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services
{
    public class WorkoutPlanService : IWorkoutPlanService
    {
        private readonly List<WorkoutPlan> _plans = new();

        public WorkoutPlanService()
        {
            // Seed with predefined plans
            _plans.Add(new WorkoutPlan
            {
                Name = "Beginner Full Body",
                Description = "A simple full-body routine for beginners. Perform 3 days per week.",
                Workouts = new List<Workout>
                {
                    new Workout("Squats", 0, 12, 3, "Legs", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Push Ups", 0, 10, 3, "Chest", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Bent Over Rows", 0, 10, 3, "Back", DayOfWeek.Wednesday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Plank", 0, 60, 3, "Core", DayOfWeek.Wednesday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"), // 60 sec hold
                    new Workout("Treadmill Walk", 0, 0, 1, "Cardio", DayOfWeek.Friday, DateTime.Now, WorkoutType.Cardio, "Cardio Area")
                    {
                        Steps = 3000
                    }
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Push/Pull/Legs Split",
                Description = "Intermediate split hitting each muscle group twice per week.",
                Workouts = new List<Workout>
                {
                    // Push Day
                    new Workout("Bench Press", 0, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Overhead Press", 0, 10, 3, "Shoulders", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Tricep Dips", 0, 12, 3, "Arms", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),

                    // Pull Day
                    new Workout("Deadlifts", 0, 5, 5, "Back", DayOfWeek.Wednesday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Pull Ups", 0, 8, 4, "Back", DayOfWeek.Wednesday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Barbell Curl", 0, 12, 3, "Arms", DayOfWeek.Wednesday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),

                    // Legs Day
                    new Workout("Squats", 0, 5, 5, "Legs", DayOfWeek.Friday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Leg Press", 0, 10, 4, "Legs", DayOfWeek.Friday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Calf Raises", 0, 15, 3, "Legs", DayOfWeek.Friday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Stationary Bike", 0, 0, 1, "Cardio", DayOfWeek.Friday, DateTime.Now, WorkoutType.Cardio, "Cardio Area")
                    {
                        Steps = 5000
                    }
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Upper/Lower Split (4x Weekly)",
                Description = "Advanced split alternating upper and lower body days.",
                Workouts = new List<Workout>
                {
                    // Upper Body
                    new Workout("Incline Bench Press", 0, 8, 4, "Chest", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Pull Ups", 0, 8, 4, "Back", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Dumbbell Shoulder Press", 0, 10, 3, "Shoulders", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Barbell Row", 0, 8, 4, "Back", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Cable Tricep Pushdown", 0, 12, 3, "Arms", DayOfWeek.Monday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),

                    // Lower Body
                    new Workout("Front Squats", 0, 8, 4, "Legs", DayOfWeek.Thursday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Romanian Deadlift", 0, 10, 3, "Legs", DayOfWeek.Thursday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Lunges", 0, 12, 3, "Legs", DayOfWeek.Thursday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("Seated Leg Curl", 0, 12, 3, "Legs", DayOfWeek.Thursday, DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                    new Workout("HIIT Treadmill Sprints", 0, 0, 1, "Cardio", DayOfWeek.Thursday, DateTime.Now, WorkoutType.Cardio, "Cardio Area")
                    {
                        Steps = 4000
                    }
                },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Advanced Cardio Blast",
                Description = "A high-intensity cardio workout plan.",
                Workouts = new List<Workout>
                {
                    new Workout("Treadmill Run", 0, 0, 0, "Legs", DayOfWeek.Tuesday, DateTime.Now, WorkoutType.Cardio, "Cardio Zone") { Steps = 5000 },
                    new Workout("Cycling", 0, 0, 0, "Legs", DayOfWeek.Thursday, DateTime.Now, WorkoutType.Cardio, "Cardio Zone") { Steps = 4000 }
                },
                IsCustom = false
            });
        }

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
