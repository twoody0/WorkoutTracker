using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class WorkoutScheduleService : IWorkoutScheduleService
{
    private readonly Dictionary<DayOfWeek, List<Workout>> _weeklySchedule = new();
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly WorkoutTrackerDatabase _database;
    private readonly string _legacyScheduleStateFilePath;
    private readonly Dictionary<string, string> _activePlanExerciseSubstitutions = new(StringComparer.OrdinalIgnoreCase);
    private int? _activePlanScheduleWeekNumber;
    private int _activePlanDayOffset;
    private int _scheduleVersion;

    public WorkoutPlan? ActivePlan { get; private set; }
    public DateTime? ActivePlanStartedOn { get; private set; }
    public DateTime? ActivePlanEndsOn { get; private set; }
    public bool HasCompletedActivePlan => ActivePlanEndsOn.HasValue && DateTime.Today > ActivePlanEndsOn.Value.Date;
    public int ScheduleVersion
    {
        get
        {
            EnsureActivePlanScheduleIsCurrent();
            return _scheduleVersion;
        }
    }

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

    public void AddPlanToWeeklySchedule(WorkoutPlan plan, bool alignFirstWorkoutDayToToday = false)
    {
        ActivePlan = plan;
        ActivePlanStartedOn = DateTime.Today;
        ActivePlanEndsOn = DateTime.Today.AddDays((plan.DurationInWeeks * 7) - 1);
        _activePlanExerciseSubstitutions.Clear();
        _activePlanDayOffset = alignFirstWorkoutDayToToday
            ? GetDayOffsetFromFirstWorkoutDay(plan, DateTime.Today.DayOfWeek)
            : 0;
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
            .Select(workout => ApplyActivePlanExerciseSubstitution(CloneWorkout(workout, _activePlanDayOffset)))
            .Where(workout => workout.Day == day)
            .ToList();
    }

    public IReadOnlyList<Workout> GetActivePlanExerciseOptions()
    {
        if (ActivePlan == null)
        {
            return [];
        }

        var uniqueExercises = new Dictionary<string, Workout>(StringComparer.OrdinalIgnoreCase);
        foreach (var workout in ActivePlan.Workouts)
        {
            var originalExerciseName = GetOriginalExerciseName(workout);
            if (uniqueExercises.ContainsKey(originalExerciseName))
            {
                continue;
            }

            uniqueExercises[originalExerciseName] = ApplyActivePlanExerciseSubstitution(CloneWorkout(workout));
        }

        return uniqueExercises.Values
            .OrderBy(workout => workout.Name)
            .ToList();
    }

    public IReadOnlyList<Workout> GetPlanWorkoutsForPreview(WorkoutPlan plan, int weekNumber)
    {
        if (plan == null)
        {
            return [];
        }

        var workouts = plan.GetWorkoutsForWeek(weekNumber)
            .Select(CloneWorkout)
            .ToList();

        if (ActivePlan != null &&
            string.Equals(ActivePlan.Name, plan.Name, StringComparison.OrdinalIgnoreCase))
        {
            return workouts
                .Select(ApplyActivePlanExerciseSubstitution)
                .ToList();
        }

        return workouts;
    }

    public void ReplaceActivePlanExercise(string originalExerciseName, string replacementExerciseName)
    {
        if (ActivePlan == null || string.IsNullOrWhiteSpace(originalExerciseName) || string.IsNullOrWhiteSpace(replacementExerciseName))
        {
            return;
        }

        var normalizedOriginal = originalExerciseName.Trim();
        var normalizedReplacement = replacementExerciseName.Trim();

        if (string.Equals(normalizedOriginal, normalizedReplacement, StringComparison.OrdinalIgnoreCase))
        {
            _activePlanExerciseSubstitutions.Remove(normalizedOriginal);
        }
        else if (_activePlanExerciseSubstitutions.Any(pair =>
                     !string.Equals(pair.Key, normalizedOriginal, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(pair.Value, normalizedReplacement, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        else
        {
            _activePlanExerciseSubstitutions[normalizedOriginal] = normalizedReplacement;
        }

        PopulateWeeklyScheduleForActivePlanWeek(_activePlanScheduleWeekNumber ?? GetPlanWeekNumberForDate(DateTime.Today));
    }

    public void RestartActivePlan()
    {
        if (ActivePlan == null)
        {
            return;
        }

        var currentSubstitutions = _activePlanExerciseSubstitutions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        AddPlanToWeeklySchedule(ActivePlan, _activePlanDayOffset != 0);

        foreach (var substitution in currentSubstitutions)
        {
            _activePlanExerciseSubstitutions[substitution.Key] = substitution.Value;
        }

        PopulateWeeklyScheduleForActivePlanWeek(1);
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
            ["At-Home Strength Builder"] = "Upper/Lower Strength Builder",
            ["Upper/Lower Strength Builder"] = "Push/Pull/Legs Hypertrophy",
            ["Push/Pull/Legs Hypertrophy"] = "Arnold Split Mass Builder",
            ["Arnold Split Mass Builder"] = "Classic Body Part Split",
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

    private static Workout CloneWorkout(Workout workout, int dayOffset = 0)
    {
        return new Workout(
            name: workout.Name,
            weight: workout.Weight,
            reps: workout.Reps,
            sets: workout.Sets,
            muscleGroup: workout.MuscleGroup,
            day: ShiftDayOfWeek(workout.Day, dayOffset),
            startTime: workout.StartTime,
            type: workout.Type,
            gymLocation: workout.GymLocation)
        {
            PlannedExerciseName = workout.PlannedExerciseName,
            MinReps = workout.MinReps,
            MaxReps = workout.MaxReps,
            TargetRpe = workout.TargetRpe,
            TargetRestRange = workout.TargetRestRange,
            EndTime = workout.EndTime,
            Steps = workout.Steps,
            DurationMinutes = workout.DurationMinutes,
            DurationSeconds = workout.DurationSeconds,
            DistanceMiles = workout.DistanceMiles,
            PlanWeekNumber = workout.PlanWeekNumber,
            IsWarmup = workout.IsWarmup
        };
    }

    private Workout ApplyActivePlanExerciseSubstitution(Workout workout)
    {
        var originalExerciseName = GetOriginalExerciseName(workout);
        if (_activePlanExerciseSubstitutions.TryGetValue(originalExerciseName, out var replacementExerciseName) &&
            !string.IsNullOrWhiteSpace(replacementExerciseName) &&
            !string.Equals(workout.Name, replacementExerciseName, StringComparison.OrdinalIgnoreCase))
        {
            workout.PlannedExerciseName = originalExerciseName;
            workout.Name = replacementExerciseName.Trim();
        }

        return workout;
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
            var scheduledWorkout = ApplyActivePlanExerciseSubstitution(CloneWorkout(workout, _activePlanDayOffset));
            _weeklySchedule[scheduledWorkout.Day].Add(scheduledWorkout);
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
            SELECT ActivePlanName, ActivePlanStartedOn, ActivePlanEndsOn, ActivePlanScheduleWeekNumber, ActivePlanDayOffset, ExerciseSubstitutionsJson
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
        _activePlanDayOffset = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
        _activePlanExerciseSubstitutions.Clear();

        var substitutionsJson = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
        if (!string.IsNullOrWhiteSpace(substitutionsJson))
        {
            var substitutions = JsonSerializer.Deserialize<Dictionary<string, string>>(substitutionsJson);
            if (substitutions != null)
            {
                foreach (var substitution in substitutions)
                {
                    if (!string.IsNullOrWhiteSpace(substitution.Key) && !string.IsNullOrWhiteSpace(substitution.Value))
                    {
                        _activePlanExerciseSubstitutions[substitution.Key.Trim()] = substitution.Value.Trim();
                    }
                }
            }
        }

        foreach (var day in _weeklySchedule.Keys.ToList())
        {
            _weeklySchedule[day].Clear();
        }

        using var workoutsCommand = connection.CreateCommand();
        workoutsCommand.CommandText =
            """
            SELECT Name, PlannedExerciseName, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup
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
            return;
        }

        _scheduleVersion++;
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
                INSERT INTO ActivePlanState (Id, ActivePlanName, ActivePlanStartedOn, ActivePlanEndsOn, ActivePlanScheduleWeekNumber, ActivePlanDayOffset, ExerciseSubstitutionsJson)
                VALUES (1, $activePlanName, $activePlanStartedOn, $activePlanEndsOn, $activePlanScheduleWeekNumber, $activePlanDayOffset, $exerciseSubstitutionsJson);
                """;
            stateCommand.Parameters.AddWithValue("$activePlanName", ActivePlan.Name);
            stateCommand.Parameters.AddWithValue("$activePlanStartedOn", ActivePlanStartedOn.Value.ToString("O"));
            stateCommand.Parameters.AddWithValue("$activePlanEndsOn", ActivePlanEndsOn.Value.ToString("O"));
            stateCommand.Parameters.AddWithValue("$activePlanScheduleWeekNumber", _activePlanScheduleWeekNumber ?? GetPlanWeekNumberForDate(DateTime.Today));
            stateCommand.Parameters.AddWithValue("$activePlanDayOffset", _activePlanDayOffset);
            stateCommand.Parameters.AddWithValue("$exerciseSubstitutionsJson", JsonSerializer.Serialize(_activePlanExerciseSubstitutions));
            stateCommand.ExecuteNonQuery();

            foreach (var workout in _weeklySchedule.SelectMany(entry => entry.Value))
            {
                InsertScheduledWorkout(connection, transaction, workout);
            }
        }

        transaction.Commit();
        _scheduleVersion++;
    }

    private void ClearSavedState()
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanScheduledWorkouts;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM ActivePlanState;");
        transaction.Commit();
        _scheduleVersion++;
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
            _activePlanDayOffset = 0;

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
            _activePlanDayOffset = 0;
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
            (Name, PlannedExerciseName, MuscleGroup, GymLocation, Weight, Reps, Sets, MinReps, MaxReps, TargetRpe, TargetRestRange, StartTime, EndTime, Steps, DurationMinutes, DurationSeconds, DistanceMiles, Type, Day, PlanWeekNumber, IsWarmup)
            VALUES ($name, $plannedExerciseName, $muscleGroup, $gymLocation, $weight, $reps, $sets, $minReps, $maxReps, $targetRpe, $targetRestRange, $startTime, $endTime, $steps, $durationMinutes, $durationSeconds, $distanceMiles, $type, $day, $planWeekNumber, $isWarmup);
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

    private static int GetDayOffsetFromFirstWorkoutDay(WorkoutPlan plan, DayOfWeek desiredStartDay)
    {
        var firstWorkoutDay = GetFirstWorkoutDay(plan);
        if (!firstWorkoutDay.HasValue)
        {
            return 0;
        }

        var desiredIndex = GetMondayFirstDayIndex(desiredStartDay);
        var firstWorkoutIndex = GetMondayFirstDayIndex(firstWorkoutDay.Value);
        return desiredIndex - firstWorkoutIndex;
    }

    private static DayOfWeek? GetFirstWorkoutDay(WorkoutPlan plan)
    {
        return plan.GetWorkoutsForWeek(1)
            .Select(workout => workout.Day)
            .Distinct()
            .OrderBy(GetMondayFirstDayIndex)
            .Cast<DayOfWeek?>()
            .FirstOrDefault();
    }

    private static int GetMondayFirstDayIndex(DayOfWeek day)
        => day == DayOfWeek.Sunday ? 6 : ((int)day - 1);

    private static DayOfWeek ShiftDayOfWeek(DayOfWeek day, int offset)
    {
        var shiftedIndex = (((int)day + offset) % 7 + 7) % 7;
        return (DayOfWeek)shiftedIndex;
    }

    private static string GetOriginalExerciseName(Workout workout)
    {
        return string.IsNullOrWhiteSpace(workout.PlannedExerciseName)
            ? workout.Name
            : workout.PlannedExerciseName.Trim();
    }
}

public interface IWorkoutScheduleService
{
    WorkoutPlan? ActivePlan { get; }
    DateTime? ActivePlanStartedOn { get; }
    DateTime? ActivePlanEndsOn { get; }
    bool HasCompletedActivePlan { get; }
    int ScheduleVersion { get; }
    void AddPlanToWeeklySchedule(WorkoutPlan plan, bool alignFirstWorkoutDayToToday = false);
    void AddWorkoutToDay(DayOfWeek day, Workout workout);
    void RemoveWorkoutFromDay(DayOfWeek day, Workout workout);
    IReadOnlyDictionary<DayOfWeek, List<Workout>> GetWeeklySchedule();
    IReadOnlyList<Workout> GetActivePlanWorkoutsForDay(DayOfWeek day);
    IReadOnlyList<Workout> GetActivePlanExerciseOptions();
    IReadOnlyList<Workout> GetPlanWorkoutsForPreview(WorkoutPlan plan, int weekNumber);
    void ReplaceActivePlanExercise(string originalExerciseName, string replacementExerciseName);
    void RestartActivePlan();
    WorkoutPlan? GetSuggestedNextPlan();
    string GetActivePlanTimelineSummary();
}
