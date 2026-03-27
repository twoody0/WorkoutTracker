using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly WorkoutTrackerDatabase _database;
    private readonly string _legacyScheduleStateFilePath;
    private int? _activePlanScheduleWeekNumber;

    public WorkoutPlan? ActivePlan { get; private set; }
    public DateTime? ActivePlanStartedOn { get; private set; }
    public DateTime? ActivePlanEndsOn { get; private set; }
    public bool HasCompletedActivePlan => ActivePlanEndsOn.HasValue && DateTime.Today > ActivePlanEndsOn.Value.Date;

    public WorkoutScheduleService(IWorkoutPlanService workoutPlanService, string? databasePath = null)
    {
        _workoutPlanService = workoutPlanService;
        _database = new WorkoutTrackerDatabase(databasePath);
        _legacyScheduleStateFilePath = Path.Combine(
            Path.GetDirectoryName(_database.DatabasePath) ?? string.Empty,
            "active_workout_schedule.json");

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            _weeklySchedule[day] = new List<Workout>();
        }

        MigrateLegacyJsonIfNeeded();
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
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ActivePlanName, ActivePlanStartedOn, ActivePlanEndsOn, ActivePlanScheduleWeekNumber
            FROM ActivePlanState
            WHERE Id = 1;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return;
        }

        var activePlanName = reader.GetString(0);
        var matchingPlan = _workoutPlanService.GetWorkoutPlans()
            .FirstOrDefault(plan => string.Equals(plan.Name, activePlanName, StringComparison.OrdinalIgnoreCase));

        if (matchingPlan == null)
        {
            ClearSavedState();
            return;
        }

        ActivePlan = matchingPlan;
        ActivePlanStartedOn = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind);
        ActivePlanEndsOn = DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind);
        _activePlanScheduleWeekNumber = reader.GetInt32(3);

        foreach (var day in _weeklySchedule.Keys.ToList())
        {
            _weeklySchedule[day].Clear();
        }

        using var workoutsCommand = connection.CreateCommand();
        workoutsCommand.CommandText =
            """
            SELECT Name, MuscleGroup, GymLocation, Weight, Reps, Sets, StartTime, EndTime, Steps, DurationMinutes, DistanceMiles, Type, Day, PlanWeekNumber
            FROM ActivePlanScheduledWorkouts
            ORDER BY Id;
            """;

        using var workoutReader = workoutsCommand.ExecuteReader();
        var hasSavedWorkouts = false;
        while (workoutReader.Read())
        {
            var workout = WorkoutPlanService.ReadWorkout(workoutReader, 0);
            _weeklySchedule[workout.Day].Add(workout);
            hasSavedWorkouts = true;
        }

        if (!hasSavedWorkouts)
        {
            PopulateWeeklyScheduleForActivePlanWeek(GetPlanWeekNumberForDate(DateTime.Today));
        }
    }

    private void SaveState()
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanScheduledWorkouts;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanState;");

        if (ActivePlan != null && ActivePlanStartedOn.HasValue && ActivePlanEndsOn.HasValue)
        {
            using var stateCommand = connection.CreateCommand();
            stateCommand.Transaction = transaction;
            stateCommand.CommandText =
                """
                INSERT INTO ActivePlanState (Id, ActivePlanName, ActivePlanStartedOn, ActivePlanEndsOn, ActivePlanScheduleWeekNumber)
                VALUES (1, $activePlanName, $activePlanStartedOn, $activePlanEndsOn, $activePlanScheduleWeekNumber);
                """;
            stateCommand.Parameters.AddWithValue("$activePlanName", ActivePlan.Name);
            stateCommand.Parameters.AddWithValue("$activePlanStartedOn", ActivePlanStartedOn.Value.ToString("O"));
            stateCommand.Parameters.AddWithValue("$activePlanEndsOn", ActivePlanEndsOn.Value.ToString("O"));
            stateCommand.Parameters.AddWithValue("$activePlanScheduleWeekNumber", _activePlanScheduleWeekNumber ?? GetPlanWeekNumberForDate(DateTime.Today));
            stateCommand.ExecuteNonQuery();

            foreach (var workout in _weeklySchedule.SelectMany(entry => entry.Value))
            {
                InsertScheduledWorkout(connection, transaction, workout);
            }
        }

        transaction.Commit();
    }

    private void ClearSavedState()
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanScheduledWorkouts;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanState;");
        transaction.Commit();
    }

    private void MigrateLegacyJsonIfNeeded()
    {
        if (HasSavedState() || !File.Exists(_legacyScheduleStateFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_legacyScheduleStateFilePath);
            var state = JsonSerializer.Deserialize<LegacyWorkoutScheduleState>(json);
            if (state == null || string.IsNullOrWhiteSpace(state.ActivePlanName))
            {
                return;
            }

            var matchingPlan = _workoutPlanService.GetWorkoutPlans()
                .FirstOrDefault(plan => string.Equals(plan.Name, state.ActivePlanName, StringComparison.OrdinalIgnoreCase));

            if (matchingPlan == null)
            {
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

            foreach (var workout in state.ScheduledWorkouts)
            {
                _weeklySchedule[workout.Day].Add(CloneWorkout(workout));
            }

            SaveState();
            File.Delete(_legacyScheduleStateFilePath);

            ActivePlan = null;
            ActivePlanStartedOn = null;
            ActivePlanEndsOn = null;
            _activePlanScheduleWeekNumber = null;
            foreach (var day in _weeklySchedule.Keys.ToList())
            {
                _weeklySchedule[day].Clear();
            }
        }
        catch
        {
            // Leave the old file alone if migration fails.
        }
    }

    private bool HasSavedState()
    {
        using var connection = _database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM ActivePlanState;";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void ExecuteNonQuery(Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction transaction, string commandText)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static void InsertScheduledWorkout(Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction transaction, Workout workout)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO ActivePlanScheduledWorkouts
            (Name, MuscleGroup, GymLocation, Weight, Reps, Sets, StartTime, EndTime, Steps, DurationMinutes, DistanceMiles, Type, Day, PlanWeekNumber)
            VALUES ($name, $muscleGroup, $gymLocation, $weight, $reps, $sets, $startTime, $endTime, $steps, $durationMinutes, $distanceMiles, $type, $day, $planWeekNumber);
            """;
        WorkoutPlanService.AddWorkoutParameters(command, workout);
        command.ExecuteNonQuery();
    }

    private sealed class LegacyWorkoutScheduleState
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
