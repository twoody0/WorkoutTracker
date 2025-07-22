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
                Description = "A simple full-body plan for beginners.",
                Workouts = new List<Workout>
            {
                new Workout("Push Ups", 0, 12, 3, "Chest", DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Bodyweight Squats", 0, 15, 3, "Legs", DateTime.Now, WorkoutType.WeightLifting, "Main Gym"),
                new Workout("Plank", 0, 1, 3, "Core", DateTime.Now, WorkoutType.WeightLifting, "Main Gym")
            },
                IsCustom = false
            });

            _plans.Add(new WorkoutPlan
            {
                Name = "Advanced Cardio Blast",
                Description = "A high-intensity cardio workout plan.",
                Workouts = new List<Workout>
            {
                new Workout("Treadmill Run", 0, 0, 0, "Legs", DateTime.Now, WorkoutType.Cardio, "Cardio Zone") { Steps = 5000 },
                new Workout("Cycling", 0, 0, 0, "Legs", DateTime.Now, WorkoutType.Cardio, "Cardio Zone") { Steps = 4000 }
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
