using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly string _scheduleStateFilePath;
    private int? _activePlanScheduleWeekNumber;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public WorkoutPlan? ActivePlan { get; private set; }
    public DateTime? ActivePlanStartedOn { get; private set; }
    public DateTime? ActivePlanEndsOn { get; private set; }
    public bool HasCompletedActivePlan => ActivePlanEndsOn.HasValue && DateTime.Today > ActivePlanEndsOn.Value.Date;

    public WorkoutScheduleService(IWorkoutPlanService workoutPlanService, string? scheduleStateFilePath = null)
    {
        _workoutPlanService = workoutPlanService;
        _scheduleStateFilePath = scheduleStateFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkoutTracker",
            "active_workout_schedule.json");

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _weeklySchedule[day] = new List<Workout>();
        }

        RestoreState();
    }

    public void AddPlanToWeeklySchedule(WorkoutPlan plan)
    {
        ActivePlan = plan;
        ActivePlanStartedOn = DateTime.Today;
        ActivePlanEndsOn = DateTime.Today.AddDays((plan.DurationInWeeks * 7) - 1);
        PopulateWeeklyScheduleForActivePlanWeek(1);
    }

    public void AddWorkoutToDay(DayOfWeek day, Workout workout)
    {
        if (!_weeklySchedule.ContainsKey(day))
        {
            _weeklySchedule[day] = new List<Workout>();
        }

        workout.Day = day;
        _weeklySchedule[day].Add(workout);
        SaveState();
    }

    public void RemoveWorkoutFromDay(DayOfWeek day, Workout workout)
    {
        if (_weeklySchedule.ContainsKey(day))
        {
            _weeklySchedule[day].Remove(workout);
            SaveState();
        }
    }

    public IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule()
    {
        EnsureActivePlanScheduleIsCurrent();
        return _weeklySchedule;
    }

    public IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day)
    {
        if (ActivePlan == null)
        {
            return [];
        }

        EnsureActivePlanScheduleIsCurrent();

        return ActivePlan.GetWorkoutsForWeek(GetPlanWeekNumberForDate(DateTime.Today))
            .Where(workout => workout.Day == day)
            .Select(CloneWorkout)
            .ToList();
    }

    public void RestartActivePlan()
    {
        if (ActivePlan == null)
        {
            return;
        }

        AddPlanToWeeklySchedule(ActivePlan);
    }

    public WorkoutPlan? GetSuggestedNextPlan()
    {
        if (ActivePlan == null)
        {
            return null;
        }

        var planProgressionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Beginner Full Body Foundation"] = "Upper/Lower Strength Builder",
            ["Upper/Lower Strength Builder"] = "Push/Pull/Legs Hypertrophy",
            ["Brisk Walking Starter"] = "Interval Conditioning Builder",
            ["Interval Conditioning Builder"] = "Couch to 5K Starter"
        };

        if (planProgressionMap.TryGetValue(ActivePlan.Name, out var nextPlanName))
        {
            return _workoutPlanService.GetWorkoutPlans()
                .FirstOrDefault(plan =>
                    !plan.IsCustom &&
                    string.Equals(plan.Name, nextPlanName, StringComparison.OrdinalIgnoreCase));
        }

        return _workoutPlanService.GetWorkoutPlans()
            .FirstOrDefault(plan =>
                !plan.IsCustom &&
                !string.Equals(plan.Name, ActivePlan.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(plan.Category, ActivePlan.Category, StringComparison.OrdinalIgnoreCase));
    }

    public string GetActivePlanTimelineSummary()
    {
        if (ActivePlan == null || !ActivePlanStartedOn.HasValue || !ActivePlanEndsOn.HasValue)
        {
            return "No active plan timeline.";
        }

        if (HasCompletedActivePlan)
        {
            return $"Completed on {ActivePlanEndsOn.Value:d}.";
        }

        var daysRemaining = (ActivePlanEndsOn.Value.Date - DateTime.Today).Days + 1;
        var weeksRemaining = Math.Max(1, (int)Math.Ceiling(daysRemaining / 7d));
        return $"{ActivePlan.DurationDisplay} plan. {weeksRemaining} week{(weeksRemaining == 1 ? string.Empty : "s")} remaining until {ActivePlanEndsOn.Value:d}.";
    }

    private static Workout CloneWorkout(Workout workout)
    {
        return new Workout(
            name: workout.Name,
            weight: workout.Weight,
            reps: workout.Reps,
            sets: workout.Sets,
            muscleGroup: workout.MuscleGroup,
            day: workout.Day,
            startTime: workout.StartTime,
            type: workout.Type,
            gymLocation: workout.GymLocation)
        {
            EndTime = workout.EndTime,
            Steps = workout.Steps,
            DurationMinutes = workout.DurationMinutes,
            DistanceMiles = workout.DistanceMiles,
            PlanWeekNumber = workout.PlanWeekNumber
        };
    }

    private void EnsureActivePlanScheduleIsCurrent()
    {
        if (ActivePlan == null)
        {
            return;
        }

        var currentWeekNumber = GetPlanWeekNumberForDate(DateTime.Today);
        if (_activePlanScheduleWeekNumber == currentWeekNumber)
        {
            return;
        }

        PopulateWeeklyScheduleForActivePlanWeek(currentWeekNumber);
    }

    private void PopulateWeeklyScheduleForActivePlanWeek(int weekNumber)
    {
        _activePlanScheduleWeekNumber = weekNumber;

        foreach (var day in _weeklySchedule.Keys.ToList())
        {
            _weeklySchedule[day].Clear();
        }

        if (ActivePlan == null)
        {
            SaveState();
            return;
        }

        foreach (var workout in ActivePlan.GetWorkoutsForWeek(weekNumber))
        {
            _weeklySchedule[workout.Day].Add(CloneWorkout(workout));
        }

        SaveState();
    }

    private int GetPlanWeekNumberForDate(DateTime date)
    {
        if (!ActivePlanStartedOn.HasValue)
        {
            return 1;
        }

        var dayOffset = Math.Max(0, (date.Date - ActivePlanStartedOn.Value.Date).Days);
        return (dayOffset / 7) + 1;
    }

    private void RestoreState()
    {
        if (!File.Exists(_scheduleStateFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_scheduleStateFilePath);
            var state = JsonSerializer.Deserialize<WorkoutScheduleState>(json, JsonSerializerOptions);
            if (state == null || string.IsNullOrWhiteSpace(state.ActivePlanName))
            {
                return;
            }

            var matchingPlan = _workoutPlanService.GetWorkoutPlans()
                .FirstOrDefault(plan => string.Equals(plan.Name, state.ActivePlanName, StringComparison.OrdinalIgnoreCase));

            if (matchingPlan == null)
            {
                ClearSavedState();
                return;
            }

            ActivePlan = matchingPlan;
            ActivePlanStartedOn = state.ActivePlanStartedOn;
            ActivePlanEndsOn = state.ActivePlanEndsOn;
            _activePlanScheduleWeekNumber = state.ActivePlanScheduleWeekNumber;

            foreach (var day in _weeklySchedule.Keys.ToList())
            {
                _weeklySchedule[day].Clear();
            }

            if (state.ScheduledWorkouts.Count > 0)
            {
                foreach (var workout in state.ScheduledWorkouts)
                {
                    _weeklySchedule[workout.Day].Add(CloneWorkout(workout));
                }
            }
            else
            {
                PopulateWeeklyScheduleForActivePlanWeek(GetPlanWeekNumberForDate(DateTime.Today));
            }
        }
        catch
        {
            ClearSavedState();
        }
    }

    private void SaveState()
    {
        if (ActivePlan == null || !ActivePlanStartedOn.HasValue || !ActivePlanEndsOn.HasValue)
        {
            ClearSavedState();
            return;
        }

        var directoryPath = Path.GetDirectoryName(_scheduleStateFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var state = new WorkoutScheduleState
        {
            ActivePlanName = ActivePlan.Name,
            ActivePlanStartedOn = ActivePlanStartedOn.Value,
            ActivePlanEndsOn = ActivePlanEndsOn.Value,
            ActivePlanScheduleWeekNumber = _activePlanScheduleWeekNumber ?? GetPlanWeekNumberForDate(DateTime.Today),
            ScheduledWorkouts = _weeklySchedule
                .SelectMany(entry => entry.Value)
                .Select(CloneWorkout)
                .ToList()
        };

        var json = JsonSerializer.Serialize(state, JsonSerializerOptions);
        File.WriteAllText(_scheduleStateFilePath, json);
    }

    private void ClearSavedState()
    {
        if (File.Exists(_scheduleStateFilePath))
        {
            File.Delete(_scheduleStateFilePath);
        }
    }

    private sealed class WorkoutScheduleState
    {
        public string ActivePlanName { get; set; } = string.Empty;
        public DateTime ActivePlanStartedOn { get; set; }
        public DateTime ActivePlanEndsOn { get; set; }
        public int ActivePlanScheduleWeekNumber { get; set; }
        public List<Workout> ScheduledWorkouts { get; set; } = [];
    }
}

public interface IWorkoutScheduleService
{
    WorkoutPlan? ActivePlan { get; }
    DateTime? ActivePlanStartedOn { get; }
    DateTime? ActivePlanEndsOn { get; }
    bool HasCompletedActivePlan { get; }
    void AddPlanToWeeklySchedule(WorkoutPlan plan);
    void AddWorkoutToDay(DayOfWeek day, Workout workout);
    void RemoveWorkoutFromDay(DayOfWeek day, Workout workout);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
    IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day);
    void RestartActivePlan();
    WorkoutPlan? GetSuggestedNextPlan();
    string GetActivePlanTimelineSummary();
}
