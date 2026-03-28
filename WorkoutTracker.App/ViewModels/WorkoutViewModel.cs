using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutViewModel : BaseViewModel
{
    private enum DumbbellLoadMode
    {
        None,
        EachDumbbell,
        EachSide
    }

    private static readonly HashSet<string> EachDumbbellExercises = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dumbbell Curl",
        "Hammer Curl",
        "Incline Dumbbell Curl",
        "Incline Dumbbell Press",
        "Dumbbell Fly",
        "Dumbbell Shoulder Press",
        "Arnold Press",
        "Dumbbell Bench Press",
        "Dumbbell Floor Press",
        "Seated Dumbbell Shoulder Press",
        "Lateral Raise",
        "Front Raise",
        "Rear Delt Fly"
    };

    private static readonly HashSet<string> EachSideExercises = new(StringComparer.OrdinalIgnoreCase)
    {
        "Single-Arm Dumbbell Row",
        "Concentration Curl",
        "Dumbbell Kickback"
    };

    #region Fields

    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutLibraryService _workoutLibraryService;
    private readonly IWorkoutScheduleService _workoutScheduleService;
    private readonly IBodyWeightService _bodyWeightService;

    private string _selectedMuscleGroup = string.Empty;
    private string _exerciseSearchQuery = string.Empty;
    private bool _isNameFieldFocused;
    private bool _hasWorkouts;
    private string _name = string.Empty;
    private string _weight = string.Empty;
    private string _reps = string.Empty;
    private string _sets = string.Empty;
    private int? _plannedMinReps;
    private int? _plannedMaxReps;
    private double? _plannedTargetRpe;
    private string _plannedTargetRestRange = string.Empty;
    private string _activePlanSummary = "No active workout plan. Add any workout you want.";
    private string _resistanceAdjustment = string.Empty;
    private bool _hasLoadedTemplate;
    private bool _isQuickAddMode;
    private bool _isAdvancedFieldsVisible = true;
    private bool _showManualWorkoutEntry;
    private bool _suppressSuggestionRefresh;
    private bool _isApplyingRecommendation;
    private List<Workout> _workoutHistory = new();
    private bool _hasScheduledWeightliftingWorkoutsToday;
    private WorkoutRecommendation? _selectedRecommendation;
    private bool _showRpeHelp;

    #endregion

    #region Constructor

    public WorkoutViewModel(
        IWorkoutService workoutService,
        IWorkoutLibraryService workoutLibraryService,
        IWorkoutScheduleService workoutScheduleService,
        IBodyWeightService bodyWeightService)
    {
        _workoutService = workoutService;
        _workoutLibraryService = workoutLibraryService;
        _workoutScheduleService = workoutScheduleService;
        _bodyWeightService = bodyWeightService;

        MuscleGroups = new List<string> { "Back", "Arms", "Biceps", "Chest", "Core", "Abs", "Legs", "Shoulders", "Triceps" };
        ExerciseSuggestions = new ObservableCollection<WeightliftingExercise>();
        RecommendedPlanWorkouts = new ObservableCollection<WorkoutRecommendation>();

        Weight = string.Empty;
        Reps = string.Empty;
        Sets = string.Empty;

        _ = CheckForExistingWorkouts();

        // Preload template if one exists
        if (WorkoutTemplateCache.Template is Workout workout)
        {
            _hasLoadedTemplate = true;
            ApplyWorkoutTemplate(workout, historicalWeight: null, collapseForQuickAdd: false);
            WorkoutTemplateCache.Template = null;
        }

        RefreshPlanRecommendations();
    }

    #endregion

    #region Properties

    public List<string> MuscleGroups { get; }

    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; }
    public ObservableCollection<WorkoutRecommendation> RecommendedPlanWorkouts { get; }
    public string TodayLabel => DateTime.Today.DayOfWeek.ToString();
    public bool HasActivePlan => _workoutScheduleService.ActivePlan != null;
    public bool HasRecommendedPlanWorkouts => RecommendedPlanWorkouts.Count > 0;
    public bool ShowPlanSection => HasActivePlan;
    public bool ShowPlanSuggestionsSection => HasRecommendedPlanWorkouts;
    public bool ShowPlanCompletedState => HasActivePlan && !HasRecommendedPlanWorkouts;
    public bool ShowManualWorkoutPrompt => ShowPlanCompletedState && !ShowManualWorkoutEntry;
    public bool ShowWorkoutEditor => !HasActivePlan || HasRecommendedPlanWorkouts || ShowManualWorkoutEntry;
    public bool ShowQuickAddCard => ShowWorkoutEditor && IsQuickAddMode;
    public bool ShowStandaloneWeightEditor => ShowWorkoutEditor && ShowStandaloneWeightField;
    public bool ShowAdvancedEditorContent => ShowWorkoutEditor && ShowAdvancedEditorSection;
    public string ManualWorkoutButtonText => _hasScheduledWeightliftingWorkoutsToday
        ? "Add Extra Workout"
        : "Add Workout Anyway";

    public string ActivePlanSummary
    {
        get => _activePlanSummary;
        set => SetProperty(ref _activePlanSummary, value);
    }

    public bool IsQuickAddMode
    {
        get => _isQuickAddMode;
        set
        {
            if (SetProperty(ref _isQuickAddMode, value))
            {
                OnPropertyChanged(nameof(ShowStandaloneWeightField));
                OnPropertyChanged(nameof(ShowAdvancedEditorSection));
                OnPropertyChanged(nameof(ShowQuickAddReadOnlySummary));
                OnPropertyChanged(nameof(ShowQuickAddInlineEditors));
                OnPropertyChanged(nameof(ShowStandardWeightInput));
                OnPropertyChanged(nameof(ShowQuickAddCard));
                OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
            }
        }
    }

    public bool IsAdvancedFieldsVisible
    {
        get => _isAdvancedFieldsVisible;
        set
        {
            if (SetProperty(ref _isAdvancedFieldsVisible, value))
            {
                OnPropertyChanged(nameof(ShowStandaloneWeightField));
                OnPropertyChanged(nameof(ShowAdvancedEditorSection));
                OnPropertyChanged(nameof(ShowQuickAddReadOnlySummary));
                OnPropertyChanged(nameof(ShowQuickAddInlineEditors));
                OnPropertyChanged(nameof(ShowStandardWeightInput));
                OnPropertyChanged(nameof(ShowQuickAddCard));
                OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
            }
        }
    }

    public bool ShowStandaloneWeightField => !IsQuickAddMode || !IsAdvancedFieldsVisible;
    public bool ShowAdvancedEditorSection => !IsQuickAddMode && IsAdvancedFieldsVisible;
    public bool ShowQuickAddReadOnlySummary => IsQuickAddMode && !IsAdvancedFieldsVisible;
    public bool ShowQuickAddInlineEditors => IsQuickAddMode && IsAdvancedFieldsVisible;
    public bool HasPlannedRepRange => _plannedMinReps.HasValue && _plannedMaxReps.HasValue && _plannedMaxReps.Value >= _plannedMinReps.Value;
    public string PlannedRepRangeSummary => HasPlannedRepRange ? $"Reps: {_plannedMinReps}-{_plannedMaxReps}" : string.Empty;
    public string CurrentRepsSummary => HasPlannedRepRange ? $"Reps: {_plannedMinReps}-{_plannedMaxReps}" : $"Reps: {Reps}";
    public string RepsLabel => "Reps";
    public bool HasPlannedTargetRpe => _plannedTargetRpe.HasValue && _plannedTargetRpe.Value > 0;
    public string PlannedTargetRpeSummary => HasPlannedTargetRpe ? $"RPE: {_plannedTargetRpe.GetValueOrDefault():0.#}" : string.Empty;
    public bool HasPlannedTargetRest => !string.IsNullOrWhiteSpace(_plannedTargetRestRange);
    public string PlannedTargetRestSummary => HasPlannedTargetRest ? $"Rest: {_plannedTargetRestRange}" : string.Empty;
    public bool ShowPlanRpeInfo => HasRecommendedPlanWorkouts && RecommendedPlanWorkouts.Any(workout => workout.HasTargetRpe);
    public bool ShowRpeHelp
    {
        get => _showRpeHelp;
        set => SetProperty(ref _showRpeHelp, value);
    }
    public string RpeHelpText => "RPE means rate of perceived exertion on a 1-10 scale. Around 6 feels comfortable, 8 is hard with a couple reps left, and 9-10 is near-max effort.";
    public bool HasBodyWeight => _bodyWeightService.HasBodyWeight();
    public bool IsBodyweightExercise => IsBodyweightExerciseName(Name) || IsBodyweightExerciseName(ExerciseSearchQuery);
    public bool IsPerSideDumbbellExercise => CurrentDumbbellLoadMode != DumbbellLoadMode.None;
    public bool ShowResistanceAdjustment => IsBodyweightExercise && HasBodyWeight;
    public bool ShowStandardWeightInput => !ShowResistanceAdjustment;
    public bool ShowDumbbellWeightHelper => ShowStandardWeightInput && CurrentDumbbellLoadMode != DumbbellLoadMode.None;
    public bool HasWeightHelperText => !string.IsNullOrWhiteSpace(WeightHelperText);
    public bool ShowWeightTotalPreview => !string.IsNullOrWhiteSpace(WeightTotalPreviewText);
    public string WeightLabel => "Weight";
    public string WeightPlaceholder => CurrentDumbbellLoadMode switch
    {
        DumbbellLoadMode.EachDumbbell => "One dumbbell",
        DumbbellLoadMode.EachSide => "One side",
        _ => "Enter weight (lbs or kg)"
    };
    public string WeightSummaryPrefix => "Weight";
    public string WeightSummaryText => string.IsNullOrWhiteSpace(Weight)
        ? WeightSummaryPrefix
        : $"{WeightSummaryPrefix}: {Weight}";
    public string WeightHelperText => CurrentDumbbellLoadMode switch
    {
        DumbbellLoadMode.EachDumbbell => string.Empty,
        DumbbellLoadMode.EachSide => string.Empty,
        _ => string.Empty
    };
    public string WeightTotalPreviewText
    {
        get
        {
            if (!ShowDumbbellWeightHelper)
            {
                return string.Empty;
            }

            if (!double.TryParse(Weight, out var enteredWeight) || enteredWeight <= 0)
            {
                return string.Empty;
            }

            var totalLoad = enteredWeight * GetDumbbellSideMultiplier(Name, ExerciseSearchQuery);
            return $"Total saved load: {totalLoad:0.#}";
        }
    }
    public bool CanAddWorkout
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedMuscleGroup) ||
                string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Reps) ||
                string.IsNullOrWhiteSpace(Sets))
            {
                return false;
            }

            if (ShowResistanceAdjustment)
            {
                return true;
            }

            if (!double.TryParse(Weight, out var parsedWeight))
            {
                parsedWeight = 0;
            }

            return parsedWeight > 0 || IsZeroWeightAllowedExercise(Name) || IsZeroWeightAllowedExercise(ExerciseSearchQuery);
        }
    }
    public string BaseBodyWeightSummary => HasBodyWeight
        ? $"Base body weight: {_bodyWeightService.GetBodyWeight():N0} lb"
        : "Set your body weight in Profile to auto-fill bodyweight lifts.";
    public string EffectiveLoadSummary
    {
        get
        {
            if (!ShowResistanceAdjustment)
            {
                return string.Empty;
            }

            var baseWeight = _bodyWeightService.GetBodyWeight() ?? 0;
            double.TryParse(ResistanceAdjustment, out var adjustment);
            var effectiveLoad = Math.Max(0, baseWeight + adjustment);
            var adjustmentText = adjustment == 0
                ? "no adjustment"
                : adjustment > 0
                    ? $"+{adjustment:N0} lb resistance"
                    : $"{adjustment:N0} lb assistance";
            return $"Effective load: {effectiveLoad:N0} lb ({adjustmentText})";
        }
    }
    public string ResistanceAdjustmentDisplay
    {
        get
        {
            double.TryParse(ResistanceAdjustment, out var adjustment);
            return adjustment > 0 ? $"+{adjustment:0}" : adjustment.ToString("0");
        }
    }

    public bool HasWorkouts
    {
        get => _hasWorkouts;
        set => SetProperty(ref _hasWorkouts, value);
    }

    public bool IsNameFieldFocused
    {
        get => _isNameFieldFocused;
        set => SetProperty(ref _isNameFieldFocused, value);
    }

    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (SetProperty(ref _selectedMuscleGroup, value))
            {
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
            }
        }
    }

    public string ExerciseSearchQuery
    {
        get => _exerciseSearchQuery;
        set
        {
            if (SetProperty(ref _exerciseSearchQuery, value))
            {
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                OnPropertyChanged(nameof(CanAddWorkout));
                if (!_suppressSuggestionRefresh)
                {
                    _ = UpdateExerciseSuggestionsAsync();
                }
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
            }
        }
    }

    public string Weight
    {
        get => _weight;
        set
        {
            if (SetProperty(ref _weight, value))
            {
                OnPropertyChanged(nameof(WeightSummaryText));
                OnPropertyChanged(nameof(EffectiveLoadSummary));
                OnPropertyChanged(nameof(WeightTotalPreviewText));
                OnPropertyChanged(nameof(ShowWeightTotalPreview));
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
            }
        }
    }

    public string Reps
    {
        get => _reps;
        set
        {
            if (SetProperty(ref _reps, value))
            {
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CurrentRepsSummary));
                OnPropertyChanged(nameof(CanAddWorkout));
            }
        }
    }

    public string Sets
    {
        get => _sets;
        set
        {
            if (SetProperty(ref _sets, value))
            {
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
            }
        }
    }

    public string ResistanceAdjustment
    {
        get => _resistanceAdjustment;
        set
        {
            if (SetProperty(ref _resistanceAdjustment, value))
            {
                OnPropertyChanged(nameof(EffectiveLoadSummary));
                OnPropertyChanged(nameof(ResistanceAdjustmentDisplay));
            }
        }
    }

    public WorkoutRecommendation? SelectedRecommendationItem => _selectedRecommendation;

    #endregion

    #region Commands

    public ICommand AddWorkoutCommand => new Command(async () => await AddWorkoutAsync());
    public ICommand UseRecommendedWorkoutCommand => new Command<WorkoutRecommendation>(recommendation =>
    {
        if (recommendation != null)
        {
            ApplyWorkoutTemplate(recommendation, collapseForQuickAdd: true);
        }
    });
    public ICommand ToggleAdvancedFieldsCommand => new Command(() =>
    {
        IsAdvancedFieldsVisible = !IsAdvancedFieldsVisible;
    });
    public ICommand ShowManualWorkoutEntryCommand => new Command(() =>
    {
        ShowManualWorkoutEntry = true;
    });

    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            Name = exercise.Name;
            ExerciseSearchQuery = exercise.Name;
            ExerciseSuggestions.Clear();
        }
    });

    public ICommand NavigateToViewWorkoutsCommand => new Command(async () =>
    {
        await Shell.Current.Navigation.PushAsync(App.Services.GetRequiredService<ViewWorkoutPage>());
    });
    public ICommand IncreaseRepsCommand => new Command(() => AdjustReps(1));
    public ICommand DecreaseRepsCommand => new Command(() => AdjustReps(-1));
    public ICommand ToggleRpeHelpCommand => new Command(() => ShowRpeHelp = !ShowRpeHelp);

    #endregion

    #region Private Methods

    private async Task CheckForExistingWorkouts()
    {
        _workoutHistory = (await _workoutService.GetWorkouts()).ToList();
        HasWorkouts = _workoutHistory.Any();
        RefreshPlanRecommendations();
    }

    private async Task AddWorkoutAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            await ShowError("Please select a muscle group.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await ShowError("Please enter an exercise name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Reps))
        {
            await ShowError("Please enter the number of reps.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Sets))
        {
            await ShowError("Please enter the number of sets.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Weight))
            Weight = "0";

        double.TryParse(Weight, out double parsedWeight);
        int.TryParse(Reps, out int parsedReps);
        int.TryParse(Sets, out int parsedSets);
        if (ShowResistanceAdjustment)
        {
            double.TryParse(ResistanceAdjustment, out var adjustment);
            parsedWeight = Math.Max(0, parsedWeight + adjustment);
        }
        else if (parsedWeight <= 0 && !IsZeroWeightAllowedExercise(Name) && !IsZeroWeightAllowedExercise(ExerciseSearchQuery))
        {
            await ShowError("Please enter a weight greater than 0 for this exercise.");
            return;
        }

        if (ShowStandardWeightInput && CurrentDumbbellLoadMode != DumbbellLoadMode.None)
        {
            parsedWeight *= GetDumbbellSideMultiplier(Name, ExerciseSearchQuery);
        }

        var workout = new Workout(
            name: Name,
            weight: parsedWeight,
            reps: parsedReps,
            sets: parsedSets,
            muscleGroup: SelectedMuscleGroup,
            day: DateTime.Today.DayOfWeek,
            startTime: DateTime.Now,
            type: WorkoutType.WeightLifting,
            gymLocation: "Default Gym"
        )
        {
            MinReps = _plannedMinReps,
            MaxReps = _plannedMaxReps,
            TargetRpe = _plannedTargetRpe,
            TargetRestRange = _plannedTargetRestRange
        };

        await _workoutService.AddWorkout(workout);
        _workoutHistory.Add(workout);
        HasWorkouts = true;

        RefreshPlanRecommendations();

        if (RecommendedPlanWorkouts.Count > 0)
        {
            ApplyWorkoutTemplate(RecommendedPlanWorkouts[0], collapseForQuickAdd: true);
        }
        else
        {
            if (HasActivePlan && !ShowManualWorkoutEntry)
            {
                Name = ExerciseSearchQuery = Weight = Reps = Sets = ResistanceAdjustment = string.Empty;
                ClearPlannedRepRange();
                ClearPlannedTargetRpe();
                ClearPlannedTargetRest();
                ExerciseSuggestions.Clear();
                IsQuickAddMode = false;
                IsAdvancedFieldsVisible = true;
                return;
            }

            Name = ExerciseSearchQuery = Weight = Reps = Sets = ResistanceAdjustment = string.Empty;
            ClearPlannedRepRange();
            ClearPlannedTargetRpe();
            ClearPlannedTargetRest();
            ExerciseSuggestions.Clear();
            IsQuickAddMode = false;
            IsAdvancedFieldsVisible = true;
        }
    }

    private void ApplyWorkoutTemplate(WorkoutRecommendation recommendation, bool collapseForQuickAdd)
    {
        if (_selectedRecommendation != null)
        {
            _selectedRecommendation.IsSelected = false;
        }

        _selectedRecommendation = recommendation;
        _selectedRecommendation.IsSelected = true;
        OnPropertyChanged(nameof(SelectedRecommendationItem));
        ApplyWorkoutTemplate(recommendation.Workout, recommendation.LastUsedWeight, collapseForQuickAdd);
    }

    private void ApplyWorkoutTemplate(Workout workout, double? historicalWeight, bool collapseForQuickAdd)
    {
        _isApplyingRecommendation = true;
        SelectedMuscleGroup = workout.MuscleGroup;
        Name = workout.Name;
        _suppressSuggestionRefresh = true;
        ExerciseSearchQuery = workout.Name;
        _suppressSuggestionRefresh = false;
        Weight = historicalWeight.HasValue
            ? GetDisplayWeightForExercise(historicalWeight.Value, workout.Name)
            : workout.Weight > 0 ? GetDisplayWeightForExercise(workout.Weight, workout.Name) : string.Empty;
        ResistanceAdjustment = string.Empty;
        ApplyPlannedRepRange(workout.MinReps, workout.MaxReps);
        ApplyPlannedTargetRpe(workout.TargetRpe);
        ApplyPlannedTargetRest(workout.TargetRestRange);
        Reps = GetDefaultRepsForWorkout(workout).ToString();
        Sets = workout.Sets.ToString();
        ApplyBodyweightDefaultsIfNeeded();
        NotifyBodyweightStateChanged();
        IsQuickAddMode = collapseForQuickAdd;
        IsAdvancedFieldsVisible = !collapseForQuickAdd;
        IsNameFieldFocused = false;
        ExerciseSuggestions.Clear();
        _isApplyingRecommendation = false;
    }

    public void RefreshPlanRecommendations()
    {
        var selectedWorkoutKey = _selectedRecommendation != null
            ? GetWorkoutKey(_selectedRecommendation.Workout)
            : null;

        RecommendedPlanWorkouts.Clear();
        _selectedRecommendation = null;

        var todaysPlannedWorkouts = _workoutScheduleService.GetActivePlanWorkoutsForDay(DateTime.Today.DayOfWeek)
            .Where(workout => workout.Type == WorkoutType.WeightLifting)
            .ToList();
        var completedPlanWorkoutCounts = BuildCompletedPlanWorkoutCounts(todaysPlannedWorkouts);

        _hasScheduledWeightliftingWorkoutsToday = todaysPlannedWorkouts.Count > 0;

        foreach (var workout in todaysPlannedWorkouts)
        {
            var workoutKey = GetWorkoutKey(workout);
            if (completedPlanWorkoutCounts.TryGetValue(workoutKey, out var completedCount) && completedCount > 0)
            {
                completedPlanWorkoutCounts[workoutKey] = completedCount - 1;
                continue;
            }

            var lastUsedWeight = GetLastUsedWeight(workout);

            RecommendedPlanWorkouts.Add(new WorkoutRecommendation
            {
                Workout = workout,
                LastUsedWeight = lastUsedWeight,
                RepDisplayText = GetRecommendationRepText(workout),
                TargetRpeText = workout.HasTargetRpe ? $"RPE: {workout.TargetRpeDisplay}" : string.Empty,
                TargetRestText = workout.HasTargetRestRange ? $"Rest: {workout.TargetRestRange}" : string.Empty,
                WeightDisplayPrefix = GetRecommendationWeightPrefix(),
                WeightDisplayValue = GetRecommendationWeightValue(workout, lastUsedWeight),
                WeightHelperText = GetRecommendationWeightHelperText(workout.Name)
            });
        }

        OnPropertyChanged(nameof(HasRecommendedPlanWorkouts));
        OnPropertyChanged(nameof(HasActivePlan));
        OnPropertyChanged(nameof(ShowPlanSection));
        OnPropertyChanged(nameof(ShowPlanSuggestionsSection));
        OnPropertyChanged(nameof(ShowPlanCompletedState));
        OnPropertyChanged(nameof(ShowManualWorkoutPrompt));
        OnPropertyChanged(nameof(ShowWorkoutEditor));
        OnPropertyChanged(nameof(ShowQuickAddCard));
        OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
        OnPropertyChanged(nameof(ShowAdvancedEditorContent));
        OnPropertyChanged(nameof(ShowPlanRpeInfo));
        OnPropertyChanged(nameof(ManualWorkoutButtonText));
        OnPropertyChanged(nameof(TodayLabel));
        UpdateActivePlanSummary();

        if (HasRecommendedPlanWorkouts)
        {
            if (!_hasLoadedTemplate && string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(ExerciseSearchQuery))
            {
                ApplyWorkoutTemplate(RecommendedPlanWorkouts[0], collapseForQuickAdd: true);
            }
            else
            {
                var matchingRecommendation = !string.IsNullOrWhiteSpace(selectedWorkoutKey)
                    ? RecommendedPlanWorkouts.FirstOrDefault(recommendation =>
                        string.Equals(GetWorkoutKey(recommendation.Workout), selectedWorkoutKey, StringComparison.OrdinalIgnoreCase))
                    : null;

                matchingRecommendation ??= RecommendedPlanWorkouts.FirstOrDefault(recommendation =>
                    string.Equals(recommendation.Workout.Name, Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(recommendation.Workout.MuscleGroup, SelectedMuscleGroup, StringComparison.OrdinalIgnoreCase));

                if (matchingRecommendation != null)
                {
                    _selectedRecommendation = matchingRecommendation;
                    _selectedRecommendation.IsSelected = true;
                    OnPropertyChanged(nameof(SelectedRecommendationItem));
                }
            }
        }
    }

    private Dictionary<string, int> BuildCompletedPlanWorkoutCounts(IEnumerable<Workout> todaysPlannedWorkouts)
    {
        var plannedWorkoutKeys = todaysPlannedWorkouts
            .Select(GetWorkoutKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _workoutHistory
            .Where(workout =>
                workout.Type == WorkoutType.WeightLifting &&
                workout.StartTime.Date == DateTime.Today &&
                plannedWorkoutKeys.Contains(GetWorkoutKey(workout)))
            .GroupBy(GetWorkoutKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private void SyncSelectedRecommendationState()
    {
        if (_isApplyingRecommendation || _selectedRecommendation == null)
        {
            return;
        }

        var workout = _selectedRecommendation.Workout;
        var stillMatches = string.Equals(Name, workout.Name, StringComparison.Ordinal);

        if (stillMatches)
        {
            return;
        }

        _selectedRecommendation.IsSelected = false;
        _selectedRecommendation = null;
        ClearPlannedRepRange();
        ClearPlannedTargetRpe();
        ClearPlannedTargetRest();
        OnPropertyChanged(nameof(SelectedRecommendationItem));
    }

    private void UpdateActivePlanSummary()
    {
        if (HasRecommendedPlanWorkouts)
        {
            var activePlanName = _workoutScheduleService.ActivePlan?.Name ?? "your active plan";
            ActivePlanSummary = $"Today is {TodayLabel}. Start from '{activePlanName}' instead of entering everything manually.";
        }
        else if (_workoutScheduleService.ActivePlan != null)
        {
            ActivePlanSummary = _hasScheduledWeightliftingWorkoutsToday
                ? $"You finished the workout plan exercises for {TodayLabel}. You can keep training if you want."
                : $"'{_workoutScheduleService.ActivePlan.Name}' has no weightlifting workout for {TodayLabel}, but you can still train if you want.";
        }
        else
        {
            ActivePlanSummary = "No active workout plan. Add any workout you want.";
        }
    }

    private bool ShowManualWorkoutEntry
    {
        get => _showManualWorkoutEntry;
        set
        {
            if (SetProperty(ref _showManualWorkoutEntry, value))
            {
                OnPropertyChanged(nameof(ShowManualWorkoutPrompt));
                OnPropertyChanged(nameof(ShowWorkoutEditor));
                OnPropertyChanged(nameof(ShowQuickAddCard));
                OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
            }
        }
    }

    private static string GetWorkoutKey(Workout workout)
    {
        return string.Join("|",
            workout.Day,
            workout.Name,
            workout.MuscleGroup,
            workout.Type,
            workout.Sets,
            workout.Reps,
            workout.Steps);
    }

    private double? GetLastUsedWeight(Workout workout)
    {
        return _workoutHistory
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                historyWorkout.Weight > 0 &&
                string.Equals(historyWorkout.Name, workout.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(historyWorkout => historyWorkout.StartTime)
            .Select(historyWorkout => (double?)historyWorkout.Weight)
            .FirstOrDefault();
    }

    private async Task ShowError(string message)
    {
        var currentWindow = Application.Current?.Windows.FirstOrDefault();
        if (currentWindow?.Page is Page currentPage)
        {
            await currentPage.DisplayAlert("Error", message, "OK");
        }
    }

    #endregion

    #region Public Methods

    public async Task UpdateExerciseSuggestionsAsync()
    {
        if (!string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            var exercises = await _workoutLibraryService.SearchExercisesByName(
                SelectedMuscleGroup, ExerciseSearchQuery
            );

            var sorted = exercises.OrderBy(e => e.Name);
            ExerciseSuggestions.Clear();
            foreach (var ex in sorted)
            {
                ExerciseSuggestions.Add(ex);
            }
        }
        else
        {
            ExerciseSuggestions.Clear();
        }
    }

    private void ApplyBodyweightDefaultsIfNeeded()
    {
        if (!IsBodyweightExercise || !HasBodyWeight)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Weight) || Weight == "0")
        {
            Weight = (_bodyWeightService.GetBodyWeight() ?? 0).ToString("0.#");
        }

        if (string.IsNullOrWhiteSpace(ResistanceAdjustment))
        {
            ResistanceAdjustment = "0";
        }
    }

    private void NotifyBodyweightStateChanged()
    {
        OnPropertyChanged(nameof(HasPlannedRepRange));
        OnPropertyChanged(nameof(PlannedRepRangeSummary));
        OnPropertyChanged(nameof(CurrentRepsSummary));
        OnPropertyChanged(nameof(RepsLabel));
        OnPropertyChanged(nameof(HasPlannedTargetRpe));
        OnPropertyChanged(nameof(PlannedTargetRpeSummary));
        OnPropertyChanged(nameof(HasPlannedTargetRest));
        OnPropertyChanged(nameof(PlannedTargetRestSummary));
        OnPropertyChanged(nameof(HasBodyWeight));
        OnPropertyChanged(nameof(IsBodyweightExercise));
        OnPropertyChanged(nameof(IsPerSideDumbbellExercise));
        OnPropertyChanged(nameof(ShowResistanceAdjustment));
        OnPropertyChanged(nameof(ShowStandardWeightInput));
        OnPropertyChanged(nameof(ShowDumbbellWeightHelper));
        OnPropertyChanged(nameof(HasWeightHelperText));
        OnPropertyChanged(nameof(ShowWeightTotalPreview));
        OnPropertyChanged(nameof(WeightLabel));
        OnPropertyChanged(nameof(WeightPlaceholder));
        OnPropertyChanged(nameof(WeightSummaryPrefix));
        OnPropertyChanged(nameof(WeightSummaryText));
        OnPropertyChanged(nameof(WeightHelperText));
        OnPropertyChanged(nameof(WeightTotalPreviewText));
        OnPropertyChanged(nameof(BaseBodyWeightSummary));
        OnPropertyChanged(nameof(EffectiveLoadSummary));
        OnPropertyChanged(nameof(ResistanceAdjustmentDisplay));
        OnPropertyChanged(nameof(CanAddWorkout));
    }

    public void AdjustResistanceAdjustment(double delta)
    {
        double.TryParse(ResistanceAdjustment, out var adjustment);
        adjustment += delta;
        ResistanceAdjustment = adjustment.ToString("0");
    }

    public void AdjustReps(int delta)
    {
        var currentReps = 0;
        int.TryParse(Reps, out currentReps);
        currentReps = Math.Max(1, currentReps + delta);

        if (HasPlannedRepRange)
        {
            currentReps = Math.Clamp(currentReps, _plannedMinReps!.Value, _plannedMaxReps!.Value);
        }

        Reps = currentReps.ToString();
    }

    private void ApplyPlannedRepRange(int? minReps, int? maxReps)
    {
        if (minReps.HasValue && maxReps.HasValue && minReps.Value > 0 && maxReps.Value >= minReps.Value)
        {
            _plannedMinReps = minReps;
            _plannedMaxReps = maxReps;
        }
        else
        {
            _plannedMinReps = null;
            _plannedMaxReps = null;
        }

        OnPropertyChanged(nameof(HasPlannedRepRange));
        OnPropertyChanged(nameof(PlannedRepRangeSummary));
        OnPropertyChanged(nameof(CurrentRepsSummary));
        OnPropertyChanged(nameof(RepsLabel));
    }

    private void ClearPlannedRepRange()
    {
        ApplyPlannedRepRange(null, null);
    }

    private void ApplyPlannedTargetRpe(double? targetRpe)
    {
        _plannedTargetRpe = targetRpe.HasValue && targetRpe.Value > 0 ? targetRpe.Value : null;
        OnPropertyChanged(nameof(HasPlannedTargetRpe));
        OnPropertyChanged(nameof(PlannedTargetRpeSummary));
    }

    private void ClearPlannedTargetRpe()
    {
        ApplyPlannedTargetRpe(null);
    }

    private void ApplyPlannedTargetRest(string? targetRestRange)
    {
        _plannedTargetRestRange = targetRestRange?.Trim() ?? string.Empty;
        OnPropertyChanged(nameof(HasPlannedTargetRest));
        OnPropertyChanged(nameof(PlannedTargetRestSummary));
    }

    private void ClearPlannedTargetRest()
    {
        ApplyPlannedTargetRest(null);
    }

    private static int GetDefaultRepsForWorkout(Workout workout)
    {
        if (workout.MinReps.HasValue && workout.MaxReps.HasValue && workout.MaxReps.Value >= workout.MinReps.Value)
        {
            return workout.MaxReps.Value <= 5 ? workout.MinReps.Value : workout.MaxReps.Value;
        }

        return Math.Max(1, workout.Reps);
    }

    private static bool IsBodyweightExerciseName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToLowerInvariant();
        return normalized.Contains("pull-up") ||
               normalized.Contains("pull up") ||
               normalized.Contains("chin-up") ||
               normalized.Contains("chin up") ||
               normalized.Contains("push-up") ||
               normalized.Contains("push up") ||
               normalized.Contains("dip") ||
               normalized.Contains("bodyweight") ||
               normalized.Contains("wall push-up") ||
               normalized.Contains("incline push-up") ||
               normalized.Contains("elevated push-up") ||
               normalized.Contains("assisted pull-up") ||
               normalized.Contains("weighted pull-up");
    }

    private static bool IsZeroWeightAllowedExercise(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToLowerInvariant();
        return IsBodyweightExerciseName(normalized) ||
               normalized.Contains("plank") ||
               normalized.Contains("crunch") ||
               normalized.Contains("sit up") ||
               normalized.Contains("sit-up") ||
               normalized.Contains("leg raise") ||
               normalized.Contains("hollow hold") ||
               normalized.Contains("mountain climber") ||
               normalized.Contains("bodyweight squat") ||
               normalized.Contains("air squat") ||
               normalized.Contains("walking lunge") ||
               normalized.Contains("bodyweight lunge") ||
               normalized.Contains("jumping jack") ||
               normalized.Contains("burpee");
    }

    private DumbbellLoadMode CurrentDumbbellLoadMode => GetDumbbellLoadMode(Name, ExerciseSearchQuery);

    private static DumbbellLoadMode GetDumbbellLoadMode(params string?[] names)
    {
        var candidates = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToList();

        if (candidates.Count == 0)
        {
            return DumbbellLoadMode.None;
        }

        foreach (var candidate in candidates)
        {
            if (EachSideExercises.Contains(candidate))
            {
                return DumbbellLoadMode.EachSide;
            }

            if (EachDumbbellExercises.Contains(candidate))
            {
                return DumbbellLoadMode.EachDumbbell;
            }
        }

        foreach (var candidate in candidates)
        {
            var normalized = candidate.ToLowerInvariant();

            if (normalized.Contains("goblet") || normalized.Contains("pullover"))
            {
                return DumbbellLoadMode.None;
            }

            if ((normalized.Contains("single-arm") ||
                 normalized.Contains("single arm") ||
                 normalized.Contains("one-arm") ||
                 normalized.Contains("one arm") ||
                 normalized.Contains("alternating")) &&
                (normalized.Contains("dumbbell") || normalized.Contains("curl") || normalized.Contains("row")))
            {
                return DumbbellLoadMode.EachSide;
            }

            if (normalized.Contains("dumbbell") &&
                (normalized.Contains("curl") ||
                 normalized.Contains("press") ||
                 normalized.Contains("fly") ||
                 normalized.Contains("bench") ||
                 normalized.Contains("floor") ||
                 normalized.Contains("row") ||
                 normalized.Contains("raise") ||
                 normalized.Contains("shrug")))
            {
                return DumbbellLoadMode.EachDumbbell;
            }
        }

        return DumbbellLoadMode.None;
    }

    private static int GetDumbbellSideMultiplier(params string?[] names)
    {
        return GetDumbbellLoadMode(names) == DumbbellLoadMode.None ? 1 : 2;
    }

    private static string GetDisplayWeightForExercise(double storedWeight, string? exerciseName)
    {
        var displayWeight = GetDumbbellLoadMode(exerciseName) != DumbbellLoadMode.None
            ? storedWeight / GetDumbbellSideMultiplier(exerciseName)
            : storedWeight;

        return displayWeight.ToString("0.#");
    }

    private static string GetRecommendationWeightPrefix() => "Weight";

    private static string GetRecommendationWeightHelperText(string? exerciseName)
    {
        return string.Empty;
    }

    private static string GetRecommendationWeightValue(Workout workout, double? lastUsedWeight)
    {
        var weightToDisplay = lastUsedWeight ?? workout.Weight;
        if (weightToDisplay <= 0)
        {
            return "0";
        }

        return GetDisplayWeightForExercise(weightToDisplay, workout.Name);
    }

    private static string GetRecommendationRepText(Workout workout)
    {
        return workout.HasRepRange
            ? $"Reps: {workout.MinReps}-{workout.MaxReps}"
            : $"Reps: {workout.Reps}";
    }

    #endregion
}
