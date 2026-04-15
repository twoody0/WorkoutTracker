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

    private static readonly string[] ManualCardioExerciseOptions =
    [
        "Run",
        "Walk",
        "Bike",
        "Row",
        "Elliptical",
        "Stair Climber",
        "Hike",
        "Swim",
        "Jump Rope"
    ];

    #region Fields

    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutLibraryService _workoutLibraryService;
    private readonly IWorkoutScheduleService _workoutScheduleService;
    private readonly IBodyWeightService _bodyWeightService;
    private readonly IWorkoutReminderService _workoutReminderService;

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
    private bool _isManualCardio;
    private string _durationMinutesText = string.Empty;
    private string _timedStrengthSecondsText = string.Empty;
    private string _distanceMilesText = string.Empty;
    private string _stepsText = string.Empty;
    private string _activeSets = "1";
    private bool _isQuickAddMode;
    private bool _isAdvancedFieldsVisible = true;
    private bool _showManualWorkoutEntry;
    private bool _hadActivePlanOnLastRefresh;
    private bool _suppressSuggestionRefresh;
    private bool _isApplyingRecommendation;
    private List<Workout> _workoutHistory = new();
    private bool _hasScheduledPlanWorkoutsToday;
    private bool _hasCarryoverPlanWorkouts;
    private bool _showAutoCarryoverNotice;
    private WorkoutRecommendation? _selectedRecommendation;
    private bool _showRpeHelp;
    private bool _isWarmupSetSelected;
    private bool _shouldClearExerciseNameOnNextFocus;
    private CancellationTokenSource? _exerciseSuggestionDebounceCts;
    private CancellationTokenSource? _timedStrengthCountdownCancellation;
    private int _exerciseSuggestionRequestVersion;
    private long _lastLoadedWorkoutChangeVersion = -1;
    private string _lastPlanRecommendationSignature = string.Empty;
    private string _currentCarryoverDecisionKey = string.Empty;
    private string _handledCarryoverDecisionKey = string.Empty;
    private string _selectedRecommendationAppliedExerciseName = string.Empty;
    private string _selectedRecommendationPlanReferenceName = string.Empty;
    private int _timedStrengthCountdownRemainingSeconds;
    private bool _isTimedStrengthCountdownRunning;
    private bool _hasTimedStrengthCountdownCompleted;

    #endregion

    #region Constructor

    public WorkoutViewModel(
        IWorkoutService workoutService,
        IWorkoutLibraryService workoutLibraryService,
        IWorkoutScheduleService workoutScheduleService,
        IBodyWeightService bodyWeightService,
        IWorkoutReminderService workoutReminderService)
    {
        _workoutService = workoutService;
        _workoutLibraryService = workoutLibraryService;
        _workoutScheduleService = workoutScheduleService;
        _bodyWeightService = bodyWeightService;
        _workoutReminderService = workoutReminderService;

        MuscleGroups = new List<string> { "Back", "Arms", "Biceps", "Chest", "Core", "Legs", "Shoulders", "Triceps" };
        ExerciseOptions = new ObservableCollection<string>();
        ExerciseSuggestions = new ObservableCollection<WeightliftingExercise>();
        RecommendedPlanWorkouts = new ObservableCollection<WorkoutRecommendation>();

        Weight = string.Empty;
        Reps = "1";
        Sets = "1";

        _ = CheckForExistingWorkouts();

        // Preload template if one exists
        if (WorkoutTemplateCache.Template is Workout workout)
        {
            ApplyWorkoutTemplate(
                workout,
                historicalWeight: null,
                collapseForQuickAdd: false,
                appliedExerciseName: workout.Name,
                plannedExerciseName: GetPlanTrackingName(workout));
            WorkoutTemplateCache.Template = null;
        }

        RefreshPlanRecommendations();
    }

    #endregion

    #region Properties

    public List<string> MuscleGroups { get; }

    public ObservableCollection<string> ExerciseOptions { get; }
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; }
    public ObservableCollection<WorkoutRecommendation> RecommendedPlanWorkouts { get; }
    public string TodayLabel => DateTime.Today.DayOfWeek.ToString();
    public bool HasActivePlan => _workoutScheduleService.ActivePlan != null;
    public bool CanEditSelectedMuscleGroup => !(_selectedRecommendation != null && _workoutScheduleService.ActivePlan is { IsCustom: false });
    public bool HasRecommendedPlanWorkouts => RecommendedPlanWorkouts.Count > 0;
    public bool ShowPlanSection => HasActivePlan && (ShowPlanSuggestionsSection || ShowPlanCompletedSummary || ShowManualWorkoutPrompt || ShowRpeHelp);
    public bool ShowPlanSuggestionsSection => HasRecommendedPlanWorkouts && !ShowManualWorkoutEntry;
    public bool ShowCompactPlanSuggestions => RecommendedPlanWorkouts.Count > 0 && RecommendedPlanWorkouts.Count < 4;
    public bool ShowScrollablePlanSuggestions => RecommendedPlanWorkouts.Count >= 4;
    public bool ShowPlanCompletedState => HasActivePlan && !HasRecommendedPlanWorkouts;
    public bool ShowPlanCompletedSummary => ShowPlanCompletedState && !ShowManualWorkoutEntry;
    public bool ShowManualWorkoutPrompt => ShowPlanCompletedState && !ShowManualWorkoutEntry;
    public bool ShowTopManualWorkoutButton => HasRecommendedPlanWorkouts && !ShowManualWorkoutEntry;
    public bool ShowTrackCardioSessionButton => !HasActivePlan || ShowManualWorkoutEntry || ShowPlanCompletedState;
    public bool IsManualWorkoutEntryActive => ShowManualWorkoutEntry;
    public bool ShowBackToPlanButton => ShowManualWorkoutEntry && HasRecommendedPlanWorkouts;
    public bool HasRecommendedStrengthWorkouts => RecommendedPlanWorkouts.Any(workout => workout.IsWeightLifting);
    public bool HasRecommendedCardioWorkouts => RecommendedPlanWorkouts.Any(workout => workout.IsCardio);
    public bool ShowWorkoutEditor => !HasActivePlan || HasRecommendedStrengthWorkouts || ShowManualWorkoutEntry;
    public bool ShowQuickAddCard => ShowWorkoutEditor && IsQuickAddMode && !IsManualCardio;
    public bool ShowStandaloneWeightEditor => ShowQuickAddStandaloneWeightEditor || ShowManualStandaloneWeightEditor;
    public bool ShowQuickAddStandaloneWeightEditor => ShowWorkoutEditor && IsQuickAddMode && !IsAdvancedFieldsVisible && !IsManualCardio;
    public bool ShowManualStandaloneWeightEditor => ShowWorkoutEditor && !IsQuickAddMode && IsAdvancedFieldsVisible && !IsManualCardio;
    public bool ShowQuickAddWeightValueEditor => ShowQuickAddStandaloneWeightEditor && ShowStandardWeightInput;
    public bool ShowManualWeightValueEditor => ShowManualStandaloneWeightEditor && ShowStandardWeightInput;
    public bool ShowAdvancedEditorContent => ShowWorkoutEditor && ShowAdvancedEditorSection;
    public bool ShowStrengthFields => !IsManualCardio;
    public bool ShowCardioFields => IsManualCardio;
    public bool HasSelectedManualStrengthExercise => ShowStrengthFields &&
        !string.IsNullOrWhiteSpace(SelectedMuscleGroup) &&
        !string.IsNullOrWhiteSpace(Name);
    public bool ShowManualStrengthExerciseDetails => ShowStrengthFields && HasSelectedManualStrengthExercise;
    public bool ShowWarmupSetOption => IsQuickAddMode ? ShowStrengthFields : ShowManualStrengthExerciseDetails;
    public string ExercisePickerTitle => IsManualCardio ? "Select cardio workout" : "Select exercise";
    public bool HasSelectedStrengthMuscleGroup => !IsManualCardio && !string.IsNullOrWhiteSpace(SelectedMuscleGroup);
    public bool CanEditExerciseName => ShowStrengthFields && HasSelectedStrengthMuscleGroup;
    public string ExerciseNamePlaceholder => CanEditExerciseName
        ? "Type exercise name..."
        : "Select a muscle group first";
    public string ManualWorkoutButtonText => _hasScheduledPlanWorkoutsToday
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
                OnPropertyChanged(nameof(ShowQuickAddStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowQuickAddWeightValueEditor));
                OnPropertyChanged(nameof(ShowQuickAddResistanceAdjustmentLayout));
                OnPropertyChanged(nameof(ShowQuickAddStandardStrengthGrid));
                OnPropertyChanged(nameof(ShowManualStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowManualWeightValueEditor));
                OnPropertyChanged(nameof(ShowManualResistanceAdjustmentLayout));
                OnPropertyChanged(nameof(ShowManualStandardStrengthGrid));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
                OnPropertyChanged(nameof(ShowTimedQuickAddCountdown));
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
                OnPropertyChanged(nameof(ShowQuickAddStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowQuickAddWeightValueEditor));
                OnPropertyChanged(nameof(ShowQuickAddResistanceAdjustmentLayout));
                OnPropertyChanged(nameof(ShowQuickAddStandardStrengthGrid));
                OnPropertyChanged(nameof(ShowManualStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowManualWeightValueEditor));
                OnPropertyChanged(nameof(ShowManualResistanceAdjustmentLayout));
                OnPropertyChanged(nameof(ShowManualStandardStrengthGrid));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
                OnPropertyChanged(nameof(ShowTimedQuickAddCountdown));
            }
        }
    }

    public bool ShowStandaloneWeightField => !IsManualCardio && (!IsQuickAddMode || !IsAdvancedFieldsVisible);
    public bool ShowAdvancedEditorSection => !IsQuickAddMode && IsAdvancedFieldsVisible;
    public bool ShowQuickAddReadOnlySummary => IsQuickAddMode && !IsAdvancedFieldsVisible;
    public bool ShowQuickAddInlineEditors => IsQuickAddMode && IsAdvancedFieldsVisible;
    public bool ShowStandaloneWeightValueInput => ShowStandardWeightInput || ShowResistanceAdjustment;
    public bool ShowStrengthTargetEditorsWithOptionalLoad => ShowStrengthFields && !ShowNoWeightStrengthInput;
    public bool ShowQuickAddResistanceAdjustmentLayout => ShowQuickAddStandaloneWeightEditor && ShowResistanceAdjustment && ShowQuickAddRepTargetEditor;
    public bool ShowManualResistanceAdjustmentLayout => ShowManualStandaloneWeightEditor && ShowResistanceAdjustment && !UsesTimedStrengthTarget;
    public bool ShowQuickAddStandardStrengthGrid => ShowStrengthTargetEditorsWithOptionalLoad && !ShowQuickAddResistanceAdjustmentLayout;
    public bool ShowManualStandardStrengthGrid => ShowStrengthTargetEditorsWithOptionalLoad && !ShowManualResistanceAdjustmentLayout;
    public bool ShowTimedQuickAddCountdown => ShowQuickAddStandaloneWeightEditor && UsesTimedStrengthTarget;
    public bool ShowTimedManualCountdown => ShowManualStandaloneWeightEditor && UsesTimedStrengthTarget;
    public bool ShowQuickAddRepTargetEditor => !UsesTimedStrengthTarget;
    public string TimedStrengthCountdownDisplay => FormatCountdownDisplay(GetEffectiveTimedStrengthCountdownSeconds());
    public string TimedStrengthStartButtonText => _isTimedStrengthCountdownRunning
        ? "Restart"
        : _hasTimedStrengthCountdownCompleted
            ? "Start Again"
            : "Start";
    public bool CanStartTimedStrengthCountdown => UsesTimedStrengthTarget && GetConfiguredTimedStrengthSeconds() > 0;
    public bool HasPlannedRepRange => _plannedMinReps.HasValue && _plannedMaxReps.HasValue && _plannedMaxReps.Value >= _plannedMinReps.Value;
    public string PlannedRepRangeSummary => HasPlannedRepRange ? $"Reps: {_plannedMinReps}-{_plannedMaxReps}" : string.Empty;
    public bool UsesTimedStrengthTarget => !IsManualCardio && ((_selectedRecommendation?.Workout.HasTimedTarget ?? false) || Workout.PrefersTimedTarget(QuickEditExerciseName));
    public string CurrentRepsSummary => UsesTimedStrengthTarget
        ? (int.TryParse(TimedStrengthSecondsText, out var timedSeconds) && timedSeconds > 0
            ? $"Time: {timedSeconds} sec"
            : string.Empty)
        : HasPlannedRepRange
            ? $"Reps: {_plannedMinReps}-{_plannedMaxReps}"
            : $"Reps: {Reps}";
    public string RepsLabel => UsesTimedStrengthTarget ? "Time (sec)" : "Reps";
    public string StrengthTargetValue
    {
        get => UsesTimedStrengthTarget ? TimedStrengthSecondsText : Reps;
        set
        {
            if (UsesTimedStrengthTarget)
            {
                TimedStrengthSecondsText = value;
                return;
            }

            Reps = value;
        }
    }
    public bool CanEditPlannedTargets => _selectedRecommendation != null && _workoutScheduleService.ActivePlan is { IsCustom: true };
    public bool ShowReadOnlyPlannedTargets => !CanEditPlannedTargets;
    public bool HasPlannedTargetRpe => _plannedTargetRpe.HasValue && _plannedTargetRpe.Value > 0;
    public string PlannedTargetRpeSummary => HasPlannedTargetRpe ? $"RPE: {_plannedTargetRpe.GetValueOrDefault():0.#}" : string.Empty;
    public bool HasPlannedTargetRest => !string.IsNullOrWhiteSpace(_plannedTargetRestRange);
    public string PlannedTargetRestSummary => HasPlannedTargetRest ? $"Rest: {_plannedTargetRestRange}" : string.Empty;
    public bool ShowQuickEditTargetRpe => HasPlannedTargetRpe || CanEditPlannedTargets;
    public bool ShowQuickEditTargetRest => HasPlannedTargetRest || CanEditPlannedTargets;
    public bool ShowQuickEditTargetsColumn => ShowQuickEditTargetRpe || ShowQuickEditTargetRest;
    public string PlannedTargetRpeValue => HasPlannedTargetRpe ? _plannedTargetRpe.GetValueOrDefault().ToString("0.#") : string.Empty;
    public string PlannedTargetRestValue => _plannedTargetRestRange;
    public string PlannedTargetRpeInput
    {
        get => PlannedTargetRpeValue;
        set
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                ApplyPlannedTargetRpe(null);
                return;
            }

            if (!double.TryParse(trimmed, out var parsedRpe))
            {
                return;
            }

            ApplyPlannedTargetRpe(Math.Clamp(parsedRpe, 0, 10));
        }
    }

    public string PlannedTargetRestInput
    {
        get => PlannedTargetRestValue;
        set => ApplyPlannedTargetRest(value);
    }
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
    public bool ShowResistanceAdjustment => HasBodyWeight && SupportsBodyweightResistanceAdjustment(QuickEditExerciseName);
    public bool ShowNoWeightStrengthInput => ShowStrengthFields && IsZeroLoadOnlyExercise(QuickEditExerciseName);
    public bool ShowStandardWeightInput => !ShowResistanceAdjustment && !ShowNoWeightStrengthInput;
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
    public string QuickEditWeightPlaceholder => CurrentDumbbellLoadMode switch
    {
        DumbbellLoadMode.EachDumbbell => "One dumbbell",
        DumbbellLoadMode.EachSide => "One side",
        _ => "0"
    };
    public string WeightSummaryPrefix => "Weight";
    public string WeightSummaryText => double.TryParse(Weight, out var parsedWeight) && parsedWeight > 0
        ? $"{WeightSummaryPrefix}: {Weight}"
        : string.Empty;
    public string QuickEditExerciseName => !string.IsNullOrWhiteSpace(Name)
        ? Name
        : _selectedRecommendation?.Workout.Name ?? string.Empty;
    public bool HasSelectedExerciseInfo => ExerciseInfoCatalog.HasInfo(QuickEditExerciseName);
    public bool ShowQuickEditExerciseInfo => HasSelectedExerciseInfo;
    public bool HasSelectedExerciseImage => ExerciseImageCatalog.HasImage(QuickEditExerciseName);
    public string SelectedExerciseImageSource => ExerciseImageCatalog.GetImageSource(QuickEditExerciseName);
    public string QuickAddWeightSummaryText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(WeightSummaryText))
            {
                return WeightSummaryText;
            }

            if (_selectedRecommendation?.IsWeightLifting == true &&
                _selectedRecommendation.HasWeightDisplay)
            {
                return _selectedRecommendation.WeightDisplayText;
            }

            return string.Empty;
        }
    }
    public bool HasQuickAddWeightSummary => !string.IsNullOrWhiteSpace(QuickAddWeightSummaryText);
    public string WeightHelperText => CurrentDumbbellLoadMode switch
    {
        DumbbellLoadMode.EachDumbbell => "Enter one dumbbell. The app saves the total load.",
        DumbbellLoadMode.EachSide => "Enter one side. The app saves the total load.",
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

            double.TryParse(Weight, out var enteredWeight);

            var totalLoad = enteredWeight * GetDumbbellSideMultiplier(Name, ExerciseSearchQuery);
            return $"Total saved load: {totalLoad:0.#}";
        }
    }
    public bool CanDecreaseWeight
    {
        get
        {
            if (ShowResistanceAdjustment)
            {
                return double.TryParse(Weight, out var currentDisplayedWeight)
                    ? currentDisplayedWeight > 0
                    : (_bodyWeightService.GetBodyWeight() ?? 0) > 0;
            }

            return double.TryParse(Weight, out var parsedWeight) && parsedWeight > 0;
        }
    }
    public bool CanAddWorkout
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return false;
            }

            if (IsManualCardio)
            {
                var hasDuration = int.TryParse(DurationMinutesText, out var durationMinutes) && durationMinutes > 0;
                var hasDistance = double.TryParse(DistanceMilesText, out var distanceMiles) && distanceMiles > 0;
                var hasSteps = int.TryParse(StepsText, out var steps) && steps > 0;
                return hasDuration || hasDistance || hasSteps;
            }

            if (string.IsNullOrWhiteSpace(SelectedMuscleGroup) ||
                string.IsNullOrWhiteSpace(Sets))
            {
                return false;
            }

            if (UsesTimedStrengthTarget)
            {
                return int.TryParse(TimedStrengthSecondsText, out var timedSeconds) && timedSeconds > 0;
            }

            if (string.IsNullOrWhiteSpace(Reps))
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
    public bool CanSaveWarmupSet => !IsManualCardio && CanAddWorkout;
    public bool CanToggleWarmupSet => !IsManualCardio;
    public bool IsWarmupSetSelected
    {
        get => _isWarmupSetSelected;
        set
        {
            if (SetProperty(ref _isWarmupSetSelected, value))
            {
                if (value)
                {
                    ActiveSets = "1";
                }

                OnPropertyChanged(nameof(RemainingSetsAfterCurrentSave));
                OnPropertyChanged(nameof(IsCompletingSelectedPlanExercise));
                OnPropertyChanged(nameof(QuickSetProgressText));
                OnPropertyChanged(nameof(PlannedSetsInput));
                OnPropertyChanged(nameof(CanDecreaseSets));
                OnPropertyChanged(nameof(CanIncreaseSets));
                OnPropertyChanged(nameof(CanDecreaseActiveSets));
                OnPropertyChanged(nameof(CanIncreaseActiveSets));
            }
        }
    }
    public string ActiveSets
    {
        get => _activeSets;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, MaxSelectableSets);
            if (SetProperty(ref _activeSets, sanitized))
            {
                NotifyActiveSetProgressStateChanged();
            }
        }
    }
    public string PlannedSetsInput
    {
        get
        {
            if (IsWarmupSetSelected)
            {
                return "1";
            }

            return (_selectedRecommendation?.Workout.Sets ?? (int.TryParse(Sets, out var parsedSets) ? parsedSets : 1)).ToString();
        }
        set
        {
            if (IsWarmupSetSelected)
            {
                return;
            }

            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxSets);
            if (!int.TryParse(sanitized, out var parsedSets))
            {
                parsedSets = 1;
            }

            if (_selectedRecommendation?.IsWeightLifting == true)
            {
                var completedSets = GetCompletedPlanWorkoutSetCount(_selectedRecommendation.Workout);
                parsedSets = Math.Clamp(parsedSets, Math.Max(1, completedSets), InputSanitizer.MaxSets);

                if (_selectedRecommendation.Workout.Sets != parsedSets)
                {
                    _selectedRecommendation.Workout.Sets = parsedSets;
                    _selectedRecommendation.PlannedSets = parsedSets;
                    _selectedRecommendation.RemainingSets = Math.Max(0, parsedSets - completedSets);
                    if (int.TryParse(ActiveSets, out var activeSets) && activeSets > MaxSelectableSets)
                    {
                        ActiveSets = MaxSelectableSets.ToString();
                    }
                    NotifyTargetSetStateChanged();
                    NotifyActiveSetProgressStateChanged();
                }

                return;
            }

            Sets = parsedSets.ToString();
        }
    }
    public int MaxSelectableSets => GetMaxSelectableSets();
    public bool CanDecreaseSets => !IsWarmupSetSelected && int.TryParse(PlannedSetsInput, out var currentSets) && currentSets > 1;
    public bool CanIncreaseSets => !IsWarmupSetSelected && (!int.TryParse(PlannedSetsInput, out var currentSets) ? 0 : currentSets) < InputSanitizer.MaxSets;
    public bool CanDecreaseActiveSets => !IsWarmupSetSelected && int.TryParse(ActiveSets, out var currentSets) && currentSets > 1;
    public bool CanIncreaseActiveSets => !IsWarmupSetSelected && (!int.TryParse(ActiveSets, out var currentSets) ? 0 : currentSets) < MaxSelectableSets;
    public bool ShowQuickSetProgress => _selectedRecommendation?.IsWeightLifting == true;
    public int RemainingSetsAfterCurrentSave
    {
        get
        {
            if (_selectedRecommendation?.IsWeightLifting != true)
            {
                return 0;
            }

            var remainingSets = Math.Max(0, _selectedRecommendation.RemainingSets);
            if (IsWarmupSetSelected)
            {
                return remainingSets;
            }

            var selectedSets = int.TryParse(ActiveSets, out var parsedSets) ? parsedSets : 1;
            return Math.Max(0, remainingSets - Math.Clamp(selectedSets, 1, Math.Max(1, remainingSets)));
        }
    }
    public bool IsCompletingSelectedPlanExercise => ShowQuickSetProgress && !IsWarmupSetSelected && RemainingSetsAfterCurrentSave == 0;
    public string QuickSetProgressText
    {
        get
        {
            if (_selectedRecommendation?.IsWeightLifting != true)
            {
                return string.Empty;
            }

            var remainingSets = Math.Max(0, _selectedRecommendation.RemainingSets);
            if (IsWarmupSetSelected)
            {
                return $"Warmup set only. {remainingSets} sets will still be left after this save";
            }

            return RemainingSetsAfterCurrentSave == 0
                ? $"0 sets left after this save"
                : $"{RemainingSetsAfterCurrentSave} of {remainingSets} sets left after this save";
        }
    }
    public string CurrentSetSummary => $"Sets: {PlannedSetsInput}";
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

    public bool IsManualCardio
    {
        get => _isManualCardio;
        set
        {
            if (!SetProperty(ref _isManualCardio, value))
            {
                return;
            }

            if (value)
            {
                SelectedMuscleGroup = "Cardio";
                Weight = string.Empty;
                Reps = "1";
                Sets = "1";
                ResistanceAdjustment = string.Empty;
                ClearPlannedRepRange();
                ClearPlannedTargetRpe();
                ClearPlannedTargetRest();
            }
            else if (string.Equals(SelectedMuscleGroup, "Cardio", StringComparison.OrdinalIgnoreCase))
            {
                SelectedMuscleGroup = string.Empty;
            }

                Name = string.Empty;
                DurationMinutesText = string.Empty;
                TimedStrengthSecondsText = string.Empty;
                DistanceMilesText = string.Empty;
                StepsText = string.Empty;
            _ = RefreshExerciseOptionsAsync();
            NotifyManualWorkoutModeChanged();
            OnPropertyChanged(nameof(HasSelectedStrengthMuscleGroup));
            OnPropertyChanged(nameof(CanEditExerciseName));
            OnPropertyChanged(nameof(ExerciseNamePlaceholder));
        }
    }

    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            var sanitized = InputSanitizer.SanitizeMuscleGroup(value);
            if (SetProperty(ref _selectedMuscleGroup, sanitized))
            {
                Name = string.Empty;
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
                IsNameFieldFocused = !IsManualCardio && !string.IsNullOrWhiteSpace(sanitized);
                _shouldClearExerciseNameOnNextFocus = false;
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                SyncSelectedRecommendationState();
                _ = RefreshExerciseOptionsAsync();
                _ = UpdateExerciseSuggestionsAsync(showAllForCurrentGroup: IsNameFieldFocused);
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
                OnPropertyChanged(nameof(HasSelectedStrengthMuscleGroup));
                OnPropertyChanged(nameof(CanEditExerciseName));
                OnPropertyChanged(nameof(ExerciseNamePlaceholder));
                OnPropertyChanged(nameof(HasSelectedManualStrengthExercise));
                OnPropertyChanged(nameof(ShowManualStrengthExerciseDetails));
                OnPropertyChanged(nameof(ShowWarmupSetOption));
            }
        }
    }

    public string ExerciseSearchQuery
    {
        get => _exerciseSearchQuery;
        set
        {
            var sanitized = InputSanitizer.SanitizeName(value);
            if (SetProperty(ref _exerciseSearchQuery, sanitized))
            {
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                NotifyExerciseImageStateChanged();
                OnPropertyChanged(nameof(StrengthTargetValue));
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
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
            var sanitized = InputSanitizer.SanitizeName(value);
            if (SetProperty(ref _name, sanitized))
            {
                if (!string.Equals(_exerciseSearchQuery, sanitized, StringComparison.Ordinal))
                {
                    _exerciseSearchQuery = sanitized;
                    OnPropertyChanged(nameof(ExerciseSearchQuery));
                }

                OnPropertyChanged(nameof(QuickEditExerciseName));
                NotifyExerciseImageStateChanged();
                ApplyBodyweightDefaultsIfNeeded();
                NotifyBodyweightStateChanged();
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(StrengthTargetValue));
                OnPropertyChanged(nameof(HasSelectedManualStrengthExercise));
                OnPropertyChanged(nameof(ShowManualStrengthExerciseDetails));
                OnPropertyChanged(nameof(ShowWarmupSetOption));
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string DurationMinutesText
    {
        get => _durationMinutesText;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxDurationMinutes);
            if (SetProperty(ref _durationMinutesText, sanitized))
            {
                OnPropertyChanged(nameof(StrengthTargetValue));
                OnPropertyChanged(nameof(CurrentRepsSummary));
                OnPropertyChanged(nameof(CanDecreaseReps));
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string TimedStrengthSecondsText
    {
        get => _timedStrengthSecondsText;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxTimedStrengthSeconds);
            if (SetProperty(ref _timedStrengthSecondsText, sanitized))
            {
                SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: true);
                OnPropertyChanged(nameof(StrengthTargetValue));
                OnPropertyChanged(nameof(CurrentRepsSummary));
                OnPropertyChanged(nameof(CanDecreaseReps));
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string DistanceMilesText
    {
        get => _distanceMilesText;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveDecimalText(value, InputSanitizer.MaxDistanceMiles);
            if (SetProperty(ref _distanceMilesText, sanitized))
            {
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string StepsText
    {
        get => _stepsText;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxSteps);
            if (SetProperty(ref _stepsText, sanitized))
            {
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string Weight
    {
        get => _weight;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveDecimalText(
                value,
                ShowResistanceAdjustment ? InputSanitizer.MaxBodyWeight : InputSanitizer.MaxWorkoutWeight);

            if (SetProperty(ref _weight, sanitized))
            {
                OnPropertyChanged(nameof(WeightSummaryText));
                OnPropertyChanged(nameof(QuickAddWeightSummaryText));
                OnPropertyChanged(nameof(HasQuickAddWeightSummary));
                OnPropertyChanged(nameof(EffectiveLoadSummary));
                OnPropertyChanged(nameof(WeightTotalPreviewText));
                OnPropertyChanged(nameof(ShowWeightTotalPreview));
                OnPropertyChanged(nameof(CanDecreaseWeight));
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }

    public string Reps
    {
        get => _reps;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxReps);
            if (SetProperty(ref _reps, sanitized))
            {
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(StrengthTargetValue));
                OnPropertyChanged(nameof(CurrentRepsSummary));
                OnPropertyChanged(nameof(CanDecreaseReps));
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
            }
        }
    }
    public bool CanDecreaseReps
        => UsesTimedStrengthTarget
            ? int.TryParse(TimedStrengthSecondsText, out var timedSeconds) && timedSeconds > 5
            : int.TryParse(Reps, out var reps) && reps > 1;

    public string Sets
    {
        get => _sets;
        set
        {
            var sanitized = InputSanitizer.SanitizePositiveIntegerText(value, InputSanitizer.MaxSets);
            if (SetProperty(ref _sets, sanitized))
            {
                SyncSelectedRecommendationState();
                OnPropertyChanged(nameof(CanAddWorkout));
                OnPropertyChanged(nameof(CanSaveWarmupSet));
                NotifyTargetSetStateChanged();
            }
        }
    }

    public string ResistanceAdjustment
    {
        get => _resistanceAdjustment;
        set
        {
            var sanitized = InputSanitizer.SanitizeSignedDecimalText(
                value,
                -InputSanitizer.MaxResistanceAdjustment,
                InputSanitizer.MaxResistanceAdjustment);

            if (SetProperty(ref _resistanceAdjustment, sanitized))
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
    public ICommand StartManualCardioSessionCommand => new Command(async () => await StartManualCardioSessionAsync());
    public ICommand UseRecommendedWorkoutCommand => new Command<WorkoutRecommendation>(async recommendation =>
    {
        if (recommendation == null)
        {
            return;
        }

        if (recommendation.IsCardio)
        {
            await NavigateToCardioWorkoutAsync(recommendation.Workout);
            return;
        }

        ApplyWorkoutTemplate(recommendation, collapseForQuickAdd: true);
    });
    public ICommand ToggleAdvancedFieldsCommand => new Command(() =>
    {
        IsAdvancedFieldsVisible = !IsAdvancedFieldsVisible;
    });
    public ICommand ShowManualWorkoutEntryCommand => new Command(() =>
    {
        OpenManualWorkoutEntry();
    });
    public ICommand BackToPlanSuggestionsCommand => new Command(() =>
    {
        ReturnToPlanSuggestions();
    });

    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            ApplySelectedExercise(exercise.Name);
        }
    });

    public ICommand IncreaseSetsCommand => new Command(() => AdjustSets(1));
    public ICommand DecreaseSetsCommand => new Command(() => AdjustSets(-1));
    public ICommand IncreaseActiveSetsCommand => new Command(() => AdjustActiveSets(1));
    public ICommand DecreaseActiveSetsCommand => new Command(() => AdjustActiveSets(-1));
    public ICommand IncreaseRepsCommand => new Command(() => AdjustReps(1));
    public ICommand DecreaseRepsCommand => new Command(() => AdjustReps(-1));
    public ICommand StartTimedStrengthCountdownCommand => new Command(() => StartTimedStrengthCountdown());
    public ICommand ToggleRpeHelpCommand => new Command(() => ShowRpeHelp = !ShowRpeHelp);
    public ICommand ToggleWarmupSetCommand => new Command(() => IsWarmupSetSelected = !IsWarmupSetSelected);

    #endregion

    #region Private Methods

    public async Task EnsureWorkoutHistoryFreshAsync(bool force = false)
    {
        var currentChangeVersion = _workoutService.ChangeVersion;
        if (!force && _lastLoadedWorkoutChangeVersion == currentChangeVersion)
        {
            return;
        }

        await ReloadWorkoutHistoryAsync();
    }

    public async Task ReloadWorkoutHistoryAsync()
    {
        _workoutHistory = (await _workoutService.GetWorkouts()).ToList();
        HasWorkouts = _workoutHistory.Any();
        _lastLoadedWorkoutChangeVersion = _workoutService.ChangeVersion;
        RefreshPlanRecommendations();
    }

    public void EnsurePlanRecommendationsFresh()
    {
        var currentSignature = BuildPlanRecommendationSignature();
        if (string.Equals(_lastPlanRecommendationSignature, currentSignature, StringComparison.Ordinal))
        {
            return;
        }

        RefreshPlanRecommendations();
    }

    public bool NeedsMissedWorkoutCatchupChoice()
    {
        return _hasCarryoverPlanWorkouts &&
               _hasScheduledPlanWorkoutsToday &&
               !string.IsNullOrWhiteSpace(_currentCarryoverDecisionKey) &&
               !string.Equals(_handledCarryoverDecisionKey, _currentCarryoverDecisionKey, StringComparison.Ordinal);
    }

    public bool ConsumeAutoUsingMissedWorkoutCatchupNotice()
    {
        if (!_showAutoCarryoverNotice)
        {
            return false;
        }

        _showAutoCarryoverNotice = false;
        return true;
    }

    public string GetMissedWorkoutCatchupMessage()
    {
        var activePlanName = _workoutScheduleService.ActivePlan?.Name ?? "your active plan";
        return $"You missed yesterday's workout from '{activePlanName}'. If you do it today, today's planned workout and the following days will each shift forward until the next rest day absorbs the change.";
    }

    public async void UseMissedWorkoutCatchupToday()
    {
        if (string.IsNullOrWhiteSpace(_currentCarryoverDecisionKey))
        {
            return;
        }

        _handledCarryoverDecisionKey = _currentCarryoverDecisionKey;
        if (_workoutScheduleService.MoveMissedWorkoutToDate(DateTime.Today.AddDays(-1), DateTime.Today))
        {
            RefreshPlanRecommendations();
            await _workoutReminderService.RefreshWorkoutReminderAsync();
        }
    }

    public void KeepTodaysPlannedWorkout()
    {
        if (string.IsNullOrWhiteSpace(_currentCarryoverDecisionKey))
        {
            return;
        }

        _handledCarryoverDecisionKey = _currentCarryoverDecisionKey;
    }

    public async void AutoMoveMissedWorkoutToTodayIfNeeded()
    {
        if (!_hasCarryoverPlanWorkouts || _hasScheduledPlanWorkoutsToday)
        {
            return;
        }

        if (_workoutScheduleService.MoveMissedWorkoutToDate(DateTime.Today.AddDays(-1), DateTime.Today))
        {
            _showAutoCarryoverNotice = true;
            _handledCarryoverDecisionKey = _currentCarryoverDecisionKey;
            RefreshPlanRecommendations();
            await _workoutReminderService.RefreshWorkoutReminderAsync();
        }
    }

    public void SelectFirstRecommendedWorkout()
    {
        var defaultRecommendation = RecommendedPlanWorkouts.FirstOrDefault(recommendation => recommendation.IsWeightLifting);
        if (defaultRecommendation == null)
        {
            return;
        }

        ApplyWorkoutTemplate(defaultRecommendation, collapseForQuickAdd: true);
    }

    public void RestoreSelectedRecommendation(WorkoutRecommendation? recommendation)
    {
        if (recommendation == null || !RecommendedPlanWorkouts.Contains(recommendation))
        {
            return;
        }

        SetSelectedRecommendation(recommendation);
    }

    private async Task CheckForExistingWorkouts()
    {
        await EnsureWorkoutHistoryFreshAsync(force: true);
    }

    private async Task StartManualCardioSessionAsync()
    {
        WorkoutTemplateCache.Template = null;
        await Shell.Current.Navigation.PushAsync(App.Services.GetRequiredService<CardioSessionPage>());
    }

    private async Task AddWorkoutAsync()
    {
        var saved = await SaveCurrentWorkoutAsync(isWarmup: IsWarmupSetSelected);
        if (saved)
        {
            CancelTimedStrengthCountdown(resetToTarget: true);
            NotifyTimedStrengthCountdownStateChanged();
            IsWarmupSetSelected = false;
        }
    }

    private async Task<bool> SaveCurrentWorkoutAsync(bool isWarmup)
    {
        Name = Name;
        ExerciseSearchQuery = ExerciseSearchQuery;

        if (string.IsNullOrWhiteSpace(Name))
        {
            await ShowError(IsManualCardio ? "Please select a cardio workout." : "Please select an exercise.");
            return false;
        }

        if (IsManualCardio)
        {
            int.TryParse(DurationMinutesText, out var parsedDurationMinutes);
            double.TryParse(DistanceMilesText, out var parsedDistanceMiles);
            int.TryParse(StepsText, out var parsedSteps);

            if (parsedDurationMinutes <= 0 && parsedDistanceMiles <= 0 && parsedSteps <= 0)
            {
                await ShowError("Please enter time, distance, or steps for this cardio workout.");
                return false;
            }

            var cardioWorkout = new Workout(
                name: Name,
                weight: 0,
                reps: 0,
                sets: 0,
                muscleGroup: "Cardio",
                day: DateTime.Today.DayOfWeek,
                startTime: DateTime.Now,
                type: WorkoutType.Cardio,
                gymLocation: "Default Gym")
            {
                DurationMinutes = parsedDurationMinutes,
                DistanceMiles = parsedDistanceMiles,
                Steps = parsedSteps,
                EndTime = parsedDurationMinutes > 0 ? DateTime.Now.AddMinutes(parsedDurationMinutes) : DateTime.Now
            };

            await SaveWorkoutAsync(cardioWorkout);
            return true;
        }

        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            await ShowError("Please select a muscle group.");
            return false;
        }

        var isTimedStrengthTarget = UsesTimedStrengthTarget;

        if (!isTimedStrengthTarget && string.IsNullOrWhiteSpace(Reps))
        {
            await ShowError("Please enter the number of reps.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Sets))
        {
            await ShowError("Please enter the number of sets.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Weight))
        {
            Weight = "0";
        }

        double.TryParse(Weight, out double parsedWeight);
        int.TryParse(Reps, out int parsedReps);
        int.TryParse(TimedStrengthSecondsText, out var parsedStrengthDurationSeconds);
        var setsToSave = _selectedRecommendation != null && IsQuickAddMode
            ? ActiveSets
            : Sets;
        int.TryParse(setsToSave, out int parsedSets);

        if (!isTimedStrengthTarget && parsedReps <= 0)
        {
            await ShowError("Please enter reps greater than 0.");
            return false;
        }

        if (isTimedStrengthTarget && parsedStrengthDurationSeconds <= 0)
        {
            await ShowError("Please enter time greater than 0.");
            return false;
        }

        if (parsedSets <= 0)
        {
            await ShowError("Please enter sets greater than 0.");
            return false;
        }

        if (ShowResistanceAdjustment)
        {
            double.TryParse(ResistanceAdjustment, out var adjustment);
            parsedWeight = Math.Max(0, parsedWeight + adjustment);
        }
        else if (parsedWeight <= 0 && !IsZeroWeightAllowedExercise(Name) && !IsZeroWeightAllowedExercise(ExerciseSearchQuery))
        {
            await ShowError("Please enter a weight greater than 0 for this exercise.");
            return false;
        }

        if (ShowStandardWeightInput && CurrentDumbbellLoadMode != DumbbellLoadMode.None)
        {
            parsedWeight *= GetDumbbellSideMultiplier(Name, ExerciseSearchQuery);
        }

        var strengthWorkout = new Workout(
            name: Name,
            weight: parsedWeight,
            reps: isTimedStrengthTarget ? 0 : parsedReps,
            sets: parsedSets,
            muscleGroup: SelectedMuscleGroup,
            day: _selectedRecommendation?.Workout.Day ?? DateTime.Today.DayOfWeek,
            startTime: DateTime.Now,
            type: WorkoutType.WeightLifting,
            gymLocation: "Default Gym"
        )
        {
            PlannedExerciseName = GetSelectedRecommendationPlanReferenceForSave(),
            DurationSeconds = isTimedStrengthTarget ? parsedStrengthDurationSeconds : 0,
            MinReps = _plannedMinReps,
            MaxReps = _plannedMaxReps,
            TargetRpe = _plannedTargetRpe,
            TargetRestRange = _plannedTargetRestRange,
            PlanWeekNumber = _selectedRecommendation?.Workout.PlanWeekNumber,
            IsWarmup = isWarmup,
            EndTime = isTimedStrengthTarget ? DateTime.Now.AddSeconds(parsedStrengthDurationSeconds) : DateTime.Now
        };

        await SaveWorkoutAsync(strengthWorkout, restoreRecommendationAfterSave: isWarmup && _selectedRecommendation != null);
        return true;
    }

    private async Task SaveWorkoutAsync(Workout workout, bool restoreRecommendationAfterSave = false)
    {
        var selectedRecommendation = _selectedRecommendation;
        await _workoutService.AddWorkout(workout);
        await _workoutReminderService.RefreshWorkoutReminderAsync();
        _workoutHistory.Add(workout);
        HasWorkouts = true;

        if (workout.Type == WorkoutType.Cardio)
        {
            RefreshPlanRecommendations();
            Name = string.Empty;
            ExerciseSearchQuery = string.Empty;
            DurationMinutesText = string.Empty;
            TimedStrengthSecondsText = string.Empty;
            DistanceMilesText = string.Empty;
            StepsText = string.Empty;
            return;
        }

        var isPartialActivePlanSave =
            selectedRecommendation is { IsWeightLifting: true } &&
            !workout.IsWarmup &&
            IsQuickAddMode &&
            IsTemplateMatch(selectedRecommendation.Workout, workout) &&
            selectedRecommendation.RemainingSets > workout.Sets;

        if (isPartialActivePlanSave)
        {
            selectedRecommendation!.RemainingSets = Math.Max(0, selectedRecommendation.RemainingSets - workout.Sets);
            ActiveSets = "1";
            NotifyActiveSetProgressStateChanged();
            return;
        }

        RefreshPlanRecommendations();

        if (workout.IsWarmup && restoreRecommendationAfterSave && selectedRecommendation != null)
        {
            var restoredRecommendation = RecommendedPlanWorkouts.FirstOrDefault(recommendation =>
                IsTemplateMatch(recommendation.Workout, selectedRecommendation.Workout));

            if (restoredRecommendation != null)
            {
                ApplyWorkoutTemplate(restoredRecommendation, collapseForQuickAdd: true);
                return;
            }

            ApplyWorkoutTemplate(
                selectedRecommendation.Workout,
                GetLastUsedWeight(selectedRecommendation.Workout),
                collapseForQuickAdd: true,
                appliedExerciseName: selectedRecommendation.Workout.Name,
                plannedExerciseName: GetPlanTrackingName(selectedRecommendation.Workout));
            return;
        }

        var nextStrengthRecommendation = RecommendedPlanWorkouts.FirstOrDefault(recommendation => recommendation.IsWeightLifting);
        if (nextStrengthRecommendation != null)
        {
            ApplyWorkoutTemplate(nextStrengthRecommendation, collapseForQuickAdd: true);
            return;
        }

        if (HasActivePlan && !ShowManualWorkoutEntry)
        {
            Name = ExerciseSearchQuery = Weight = ResistanceAdjustment = string.Empty;
            Reps = "1";
            Sets = "1";
            DurationMinutesText = DistanceMilesText = StepsText = TimedStrengthSecondsText = string.Empty;
            ClearPlannedRepRange();
            ClearPlannedTargetRpe();
            ClearPlannedTargetRest();
            ExerciseSuggestions.Clear();
            IsQuickAddMode = false;
            IsAdvancedFieldsVisible = true;
            return;
        }

        ResistanceAdjustment = string.Empty;
        Weight = GetDefaultWeightForExerciseName(Name);
        Sets = "1";
        DurationMinutesText = DistanceMilesText = StepsText = TimedStrengthSecondsText = string.Empty;
        ClearPlannedRepRange();
        ClearPlannedTargetRpe();
        ClearPlannedTargetRest();
        ExerciseSuggestions.Clear();
        IsQuickAddMode = false;
        IsAdvancedFieldsVisible = true;
    }

    private static Workout CloneWorkoutTemplate(Workout workout)
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
            PlannedExerciseName = workout.PlannedExerciseName,
            MinReps = workout.MinReps,
            MaxReps = workout.MaxReps,
            TargetRpe = workout.TargetRpe,
            TargetRestRange = workout.TargetRestRange,
            EndTime = workout.EndTime,
            Steps = workout.Steps,
            DurationMinutes = workout.DurationMinutes,
            DistanceMiles = workout.DistanceMiles,
            DurationSeconds = workout.DurationSeconds,
            PlanWeekNumber = workout.PlanWeekNumber,
            IsWarmup = workout.IsWarmup
        };
    }

    private static bool IsTemplateMatch(Workout left, Workout right)
    {
        return string.Equals(GetPlanTrackingName(left), GetPlanTrackingName(right), StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.MuscleGroup, right.MuscleGroup, StringComparison.OrdinalIgnoreCase) &&
               left.Type == right.Type;
    }

    private async Task NavigateToCardioWorkoutAsync(Workout workout)
    {
        WorkoutTemplateCache.Template = CloneWorkoutTemplate(workout);
        await Shell.Current.Navigation.PushAsync(App.Services.GetRequiredService<CardioSessionPage>());
    }

    private void ApplyWorkoutTemplate(WorkoutRecommendation recommendation, bool collapseForQuickAdd)
    {
        SetSelectedRecommendation(recommendation);
        ApplyWorkoutTemplate(
            recommendation.Workout,
            recommendation.LastUsedWeight,
            collapseForQuickAdd,
            recommendation.RemainingSets,
            appliedExerciseName: recommendation.Workout.Name,
            plannedExerciseName: GetPlanTrackingName(recommendation.Workout));
    }

    private void ApplyWorkoutTemplate(
        Workout workout,
        double? historicalWeight,
        bool collapseForQuickAdd,
        int? remainingPlannedSets = null,
        string? appliedExerciseName = null,
        string? plannedExerciseName = null)
    {
        CancelTimedStrengthCountdown(resetToTarget: false);
        _isApplyingRecommendation = true;
        IsManualCardio = workout.Type == WorkoutType.Cardio;
        SelectedMuscleGroup = workout.MuscleGroup;
        Name = workout.Name;
        _suppressSuggestionRefresh = true;
        ExerciseSearchQuery = workout.Name;
        _suppressSuggestionRefresh = false;
        Weight = GetDefaultWeightForWorkout(workout, historicalWeight);
        ResistanceAdjustment = string.Empty;
        ApplyPlannedRepRange(workout.MinReps, workout.MaxReps);
        ApplyPlannedTargetRpe(workout.TargetRpe);
        ApplyPlannedTargetRest(workout.TargetRestRange);
        Reps = workout.HasRepTarget ? GetDefaultRepsForWorkout(workout).ToString() : string.Empty;
        TimedStrengthSecondsText = workout.HasTimedTarget ? workout.TimedTargetSeconds.ToString() : string.Empty;
        var defaultSets = workout.Sets;
        if (workout.Type == WorkoutType.WeightLifting && collapseForQuickAdd)
        {
            defaultSets = 1;
        }

        if (remainingPlannedSets.HasValue && remainingPlannedSets.Value > 0)
        {
            defaultSets = workout.Type == WorkoutType.WeightLifting && collapseForQuickAdd
                ? 1
                : remainingPlannedSets.Value;
        }

        Sets = Math.Clamp(defaultSets, 1, Math.Max(1, workout.Sets)).ToString();
        ActiveSets = "1";
        DurationMinutesText = workout.DurationMinutes > 0 ? workout.DurationMinutes.ToString() : string.Empty;
        DistanceMilesText = workout.DistanceMiles > 0 ? workout.DistanceMiles.ToString("0.##") : string.Empty;
        StepsText = workout.Steps > 0 ? workout.Steps.ToString() : string.Empty;
        UpdateSelectedRecommendationContext(appliedExerciseName ?? workout.Name, plannedExerciseName ?? workout.Name);
        ApplyBodyweightDefaultsIfNeeded();
        NotifyBodyweightStateChanged();
        OnPropertyChanged(nameof(QuickEditExerciseName));
        IsQuickAddMode = collapseForQuickAdd;
        IsAdvancedFieldsVisible = !collapseForQuickAdd;
        IsNameFieldFocused = false;
        ExerciseSuggestions.Clear();
        SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: false);
        _isApplyingRecommendation = false;
    }

    private string GetSelectedRecommendationPlanReferenceForSave()
    {
        if (_selectedRecommendation == null ||
            string.IsNullOrWhiteSpace(_selectedRecommendationPlanReferenceName) ||
            !string.Equals(Name, _selectedRecommendationAppliedExerciseName, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return _selectedRecommendationPlanReferenceName;
    }

    public void RefreshPlanRecommendations()
    {
        _lastPlanRecommendationSignature = BuildPlanRecommendationSignature();
        var hadActivePlanBeforeRefresh = _hadActivePlanOnLastRefresh;
        var hasActivePlanNow = _workoutScheduleService.ActivePlan != null;
        _hadActivePlanOnLastRefresh = hasActivePlanNow;

        SetSelectedRecommendation(null);
        RecommendedPlanWorkouts.Clear();

        var today = DateTime.Today;
        var todaysPlannedWorkouts = _workoutScheduleService.GetActivePlanWorkoutsForDate(today).ToList();
        var yesterdaysCarryoverWorkouts = GetCarryoverPlanWorkouts(today).ToList();
        var carryoverDecisionKey = BuildCarryoverDecisionKey(todaysPlannedWorkouts, yesterdaysCarryoverWorkouts);

        _hasScheduledPlanWorkoutsToday = todaysPlannedWorkouts.Count > 0;
        _hasCarryoverPlanWorkouts = yesterdaysCarryoverWorkouts.Count > 0;
        _currentCarryoverDecisionKey = carryoverDecisionKey;

        AddPlanRecommendations(todaysPlannedWorkouts, today, string.Empty);

        OnPropertyChanged(nameof(HasRecommendedPlanWorkouts));
        OnPropertyChanged(nameof(ShowCompactPlanSuggestions));
        OnPropertyChanged(nameof(ShowScrollablePlanSuggestions));
        OnPropertyChanged(nameof(HasRecommendedStrengthWorkouts));
        OnPropertyChanged(nameof(HasRecommendedCardioWorkouts));
        OnPropertyChanged(nameof(HasActivePlan));
        OnPropertyChanged(nameof(CanEditSelectedMuscleGroup));
        OnPropertyChanged(nameof(ShowPlanSection));
        OnPropertyChanged(nameof(ShowPlanSuggestionsSection));
                OnPropertyChanged(nameof(ShowPlanCompletedState));
                OnPropertyChanged(nameof(ShowPlanCompletedSummary));
                OnPropertyChanged(nameof(ShowManualWorkoutPrompt));
                OnPropertyChanged(nameof(ShowTopManualWorkoutButton));
                OnPropertyChanged(nameof(ShowTrackCardioSessionButton));
                OnPropertyChanged(nameof(IsManualWorkoutEntryActive));
                OnPropertyChanged(nameof(ShowBackToPlanButton));
                OnPropertyChanged(nameof(ShowWorkoutEditor));
                OnPropertyChanged(nameof(ShowQuickAddCard));
                OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowQuickAddStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowManualStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
        OnPropertyChanged(nameof(ShowPlanRpeInfo));
        OnPropertyChanged(nameof(ManualWorkoutButtonText));
        OnPropertyChanged(nameof(TodayLabel));
        OnPropertyChanged(nameof(QuickAddWeightSummaryText));
        UpdateActivePlanSummary();

        if (HasRecommendedPlanWorkouts)
        {
            if (!hadActivePlanBeforeRefresh && hasActivePlanNow)
            {
                ShowManualWorkoutEntry = false;
                IsQuickAddMode = true;
                IsAdvancedFieldsVisible = false;
            }

            SelectFirstRecommendedWorkout();
        }
    }

    private IEnumerable<Workout> GetCarryoverPlanWorkouts(DateTime referenceDate)
    {
        if (_workoutScheduleService.ActivePlan == null)
        {
            return [];
        }

        var carryoverDate = referenceDate.AddDays(-1);
        if (_workoutScheduleService.ActivePlanStartedOn.HasValue &&
            carryoverDate.Date < _workoutScheduleService.ActivePlanStartedOn.Value.Date)
        {
            return [];
        }

        return _workoutScheduleService.GetActivePlanWorkoutsForDate(carryoverDate)
            .Where(workout =>
            {
                var completedSets = GetCompletedPlanWorkoutSetCount(workout, carryoverDate, referenceDate);
                return workout.Type == WorkoutType.Cardio
                    ? completedSets <= 0
                    : completedSets < Math.Max(1, workout.Sets);
            });
    }

    private static string BuildCarryoverDecisionKey(IReadOnlyList<Workout> todaysPlannedWorkouts, IReadOnlyList<Workout> carryoverPlanWorkouts)
    {
        return string.Join("||",
            DateTime.Today.ToString("yyyy-MM-dd"),
            string.Join("::", todaysPlannedWorkouts.Select(GetWorkoutKey)),
            string.Join("::", carryoverPlanWorkouts.Select(GetWorkoutKey)));
    }

    private void AddPlanRecommendations(IEnumerable<Workout> plannedWorkouts, DateTime plannedDate, string scheduleContextText)
    {
        foreach (var workout in plannedWorkouts)
        {
            var completedSetsForWorkout = GetCompletedPlanWorkoutSetCount(workout, plannedDate, DateTime.Today);
            var remainingSets = workout.Type == WorkoutType.WeightLifting
                ? Math.Max(0, workout.Sets - completedSetsForWorkout)
                : 0;

            if (workout.Type == WorkoutType.WeightLifting && remainingSets <= 0)
            {
                continue;
            }

            if (workout.Type == WorkoutType.Cardio && completedSetsForWorkout > 0)
            {
                continue;
            }

            var lastUsedWeight = GetLastUsedWeight(workout);

            RecommendedPlanWorkouts.Add(new WorkoutRecommendation
            {
                Workout = workout,
                LastUsedWeight = lastUsedWeight,
                RepDisplayText = GetRecommendationRepText(workout),
                DurationDisplayText = GetRecommendationDurationText(workout),
                DistanceDisplayText = GetRecommendationDistanceText(workout),
                TargetRpeText = workout.HasTargetRpe ? $"RPE: {workout.TargetRpeDisplay}" : string.Empty,
                TargetRestText = workout.HasTargetRestRange ? $"Rest: {workout.TargetRestRange}" : string.Empty,
                WeightDisplayPrefix = GetRecommendationWeightPrefix(),
                WeightDisplayValue = GetRecommendationWeightValue(workout, lastUsedWeight),
                WeightHelperText = GetRecommendationWeightHelperText(workout.Name),
                PlannedSets = workout.Sets,
                RemainingSets = remainingSets,
                ScheduleContextText = scheduleContextText
            });
        }
    }

    private Dictionary<string, int> BuildCompletedPlanWorkoutSets(IEnumerable<Workout> todaysPlannedWorkouts)
    {
        var plannedWorkoutKeys = todaysPlannedWorkouts
            .Select(GetWorkoutKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _workoutHistory
            .Where(workout =>
                workout.StartTime.Date == DateTime.Today &&
                !workout.IsWarmup &&
                plannedWorkoutKeys.Contains(GetWorkoutKey(workout)))
            .GroupBy(GetWorkoutKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(workout => Math.Max(1, workout.Sets)), StringComparer.OrdinalIgnoreCase);
    }

    private int GetCompletedPlanWorkoutSetCount(Workout workout)
        => GetCompletedPlanWorkoutSetCount(workout, DateTime.Today, DateTime.Today);

    private int GetCompletedPlanWorkoutSetCount(Workout workout, DateTime plannedDate, DateTime completionWindowEndDate)
    {
        var workoutKey = GetWorkoutKey(workout);

        return _workoutHistory
            .Where(historyWorkout =>
                historyWorkout.StartTime.Date >= plannedDate.Date &&
                historyWorkout.StartTime.Date <= completionWindowEndDate.Date &&
                !historyWorkout.IsWarmup &&
                string.Equals(GetWorkoutKey(historyWorkout), workoutKey, StringComparison.OrdinalIgnoreCase))
            .Sum(historyWorkout => historyWorkout.Type == WorkoutType.Cardio ? 1 : Math.Max(1, historyWorkout.Sets));
    }

    private void SyncSelectedRecommendationState()
    {
        if (_isApplyingRecommendation || _selectedRecommendation == null)
        {
            return;
        }

        var stillMatches = string.Equals(Name, _selectedRecommendationAppliedExerciseName, StringComparison.Ordinal);

        if (stillMatches)
        {
            return;
        }

        SetSelectedRecommendation(null);
        ClearPlannedRepRange();
        ClearPlannedTargetRpe();
        ClearPlannedTargetRest();
        OnPropertyChanged(nameof(QuickEditExerciseName));
        NotifyExerciseImageStateChanged();
    }

    private void ApplySelectedExercise(string exerciseName)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            return;
        }

        var trimmedExerciseName = exerciseName.Trim();

        _suppressSuggestionRefresh = true;
        Name = trimmedExerciseName;
        ExerciseSearchQuery = trimmedExerciseName;
        _suppressSuggestionRefresh = false;

        if (!IsManualCardio)
        {
            Weight = GetDefaultWeightForExerciseName(trimmedExerciseName);
            ApplyTimedStrengthDefaultsIfNeeded(trimmedExerciseName);
            ApplyBodyweightDefaultsIfNeeded();
            NotifyBodyweightStateChanged();
        }

        ExerciseSuggestions.Clear();
        IsNameFieldFocused = false;
        _shouldClearExerciseNameOnNextFocus = true;
    }

    private void SetSelectedRecommendation(WorkoutRecommendation? recommendation)
    {
        if (!ReferenceEquals(_selectedRecommendation, recommendation))
        {
            CancelTimedStrengthCountdown(resetToTarget: false);
        }

        foreach (var item in RecommendedPlanWorkouts)
        {
            item.IsSelected = ReferenceEquals(item, recommendation);
        }

        _selectedRecommendation = recommendation;
        if (recommendation == null)
        {
            _selectedRecommendationAppliedExerciseName = string.Empty;
            _selectedRecommendationPlanReferenceName = string.Empty;
        }

        OnPropertyChanged(nameof(SelectedRecommendationItem));
        OnPropertyChanged(nameof(QuickAddWeightSummaryText));
        OnPropertyChanged(nameof(HasQuickAddWeightSummary));
        OnPropertyChanged(nameof(UsesTimedStrengthTarget));
        NotifyTimedStrengthCountdownStateChanged();
        OnPropertyChanged(nameof(ShowQuickAddResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowQuickAddStandardStrengthGrid));
        OnPropertyChanged(nameof(ShowManualResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowManualStandardStrengthGrid));
        OnPropertyChanged(nameof(StrengthTargetValue));
        OnPropertyChanged(nameof(CurrentRepsSummary));
        OnPropertyChanged(nameof(RepsLabel));
        OnPropertyChanged(nameof(CanDecreaseReps));
        OnPropertyChanged(nameof(CurrentSetSummary));
        OnPropertyChanged(nameof(PlannedSetsInput));
        OnPropertyChanged(nameof(QuickEditExerciseName));
        NotifyExerciseImageStateChanged();
        OnPropertyChanged(nameof(CanEditSelectedMuscleGroup));
        OnPropertyChanged(nameof(CanEditPlannedTargets));
        OnPropertyChanged(nameof(ShowReadOnlyPlannedTargets));
        OnPropertyChanged(nameof(ShowQuickEditTargetRpe));
        OnPropertyChanged(nameof(ShowQuickEditTargetRest));
        OnPropertyChanged(nameof(ShowQuickEditTargetsColumn));
        NotifyTimedStrengthCountdownStateChanged();
        NotifyTargetSetStateChanged();
        NotifyActiveSetProgressStateChanged();
    }

    private void OpenManualWorkoutEntry()
    {
        CancelTimedStrengthCountdown(resetToTarget: false);
        ShowManualWorkoutEntry = true;
        SetSelectedRecommendation(null);
        IsQuickAddMode = false;
        IsAdvancedFieldsVisible = true;
        ClearPlannedRepRange();
        ClearPlannedTargetRpe();
        ClearPlannedTargetRest();
        Name = string.Empty;
        ExerciseSearchQuery = string.Empty;
        Weight = "0";
        ResistanceAdjustment = string.Empty;
        Reps = "1";
        Sets = "1";
        ActiveSets = "1";
        DurationMinutesText = string.Empty;
        TimedStrengthSecondsText = string.Empty;
        DistanceMilesText = string.Empty;
        StepsText = string.Empty;
        ExerciseSuggestions.Clear();
        IsNameFieldFocused = false;
        _shouldClearExerciseNameOnNextFocus = false;
        SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: false);
    }

    private void ReturnToPlanSuggestions()
    {
        CancelTimedStrengthCountdown(resetToTarget: false);
        ShowManualWorkoutEntry = false;
        IsQuickAddMode = true;
        IsAdvancedFieldsVisible = false;
        SelectFirstRecommendedWorkout();
    }

    private void UpdateActivePlanSummary()
    {
        if (HasRecommendedPlanWorkouts)
        {
            var activePlanName = _workoutScheduleService.ActivePlan?.Name ?? "your active plan";
            ActivePlanSummary = $"Today is {TodayLabel}. Start from '{activePlanName}' so your workout matches today's plan.";
        }
        else if (_workoutScheduleService.ActivePlan != null)
        {
            ActivePlanSummary = _hasScheduledPlanWorkoutsToday
                ? $"You finished the workout plan exercises for {TodayLabel}. You can keep training if you want."
                : $"'{_workoutScheduleService.ActivePlan.Name}' has no scheduled workout for {TodayLabel}, but you can still train if you want.";
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
                OnPropertyChanged(nameof(ShowPlanSection));
        OnPropertyChanged(nameof(ShowPlanCompletedSummary));
        OnPropertyChanged(nameof(ShowManualWorkoutPrompt));
        OnPropertyChanged(nameof(ShowTopManualWorkoutButton));
        OnPropertyChanged(nameof(ShowTrackCardioSessionButton));
        OnPropertyChanged(nameof(ShowBackToPlanButton));
        OnPropertyChanged(nameof(ShowWorkoutEditor));
                OnPropertyChanged(nameof(ShowQuickAddCard));
                OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowQuickAddStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowManualStandaloneWeightEditor));
                OnPropertyChanged(nameof(ShowAdvancedEditorContent));
            }
        }
    }

    private static string GetWorkoutKey(Workout workout)
    {
        return string.Join("|",
            workout.Day,
            GetPlanTrackingName(workout),
            workout.MuscleGroup,
            workout.Type,
            workout.PlanWeekNumber);
    }

    private static string GetPlanTrackingName(Workout workout)
    {
        return string.IsNullOrWhiteSpace(workout.PlannedExerciseName)
            ? workout.Name
            : workout.PlannedExerciseName.Trim();
    }

    private void UpdateSelectedRecommendationContext(string? appliedExerciseName, string? plannedExerciseName)
    {
        if (_selectedRecommendation == null)
        {
            _selectedRecommendationAppliedExerciseName = string.Empty;
            _selectedRecommendationPlanReferenceName = string.Empty;
            return;
        }

        _selectedRecommendationAppliedExerciseName = appliedExerciseName?.Trim() ?? string.Empty;
        _selectedRecommendationPlanReferenceName = plannedExerciseName?.Trim() ?? string.Empty;
    }

    private double? GetLastUsedWeight(Workout workout)
    {
        if (workout.Type != WorkoutType.WeightLifting)
        {
            return null;
        }

        var lastWorkingSetWeight = _workoutHistory
            .Where(historyWorkout => !historyWorkout.IsWarmup)
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                historyWorkout.Weight > 0 &&
                string.Equals(historyWorkout.Name, workout.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(historyWorkout => historyWorkout.StartTime)
            .Select(historyWorkout => (double?)historyWorkout.Weight)
            .FirstOrDefault();

        if (lastWorkingSetWeight.HasValue)
        {
            return lastWorkingSetWeight;
        }

        return _workoutHistory
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                historyWorkout.Weight > 0 &&
                string.Equals(historyWorkout.Name, workout.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(historyWorkout => historyWorkout.StartTime)
            .Select(historyWorkout => (double?)historyWorkout.Weight)
            .FirstOrDefault();
    }

    private double? GetLastUsedWeight(string? exerciseName)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            return null;
        }

        var lastWorkingSetWeight = _workoutHistory
            .Where(historyWorkout => !historyWorkout.IsWarmup)
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                historyWorkout.Weight > 0 &&
                string.Equals(historyWorkout.Name, exerciseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(historyWorkout => historyWorkout.StartTime)
            .Select(historyWorkout => (double?)historyWorkout.Weight)
            .FirstOrDefault();

        if (lastWorkingSetWeight.HasValue)
        {
            return lastWorkingSetWeight;
        }

        return _workoutHistory
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                historyWorkout.Weight > 0 &&
                string.Equals(historyWorkout.Name, exerciseName, StringComparison.OrdinalIgnoreCase))
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

    public void CommitExerciseSelection()
    {
        if (IsManualCardio)
        {
            return;
        }

        var exerciseName = string.IsNullOrWhiteSpace(ExerciseSearchQuery)
            ? Name
            : ExerciseSearchQuery.Trim();

        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            return;
        }

        ApplySelectedExercise(exerciseName);
    }

    public bool BeginExerciseNameEdit()
    {
        if (!_shouldClearExerciseNameOnNextFocus ||
            string.IsNullOrWhiteSpace(Name) ||
            !string.Equals(ExerciseSearchQuery, Name, StringComparison.Ordinal))
        {
            return false;
        }

        _shouldClearExerciseNameOnNextFocus = false;
        Name = string.Empty;
        ExerciseSuggestions.Clear();
        return true;
    }

    #endregion

    #region Public Methods

    public async Task UpdateExerciseSuggestionsAsync(bool showAllForCurrentGroup = false)
    {
        if (IsManualCardio)
        {
            ExerciseSuggestions.Clear();
            return;
        }

        _exerciseSuggestionDebounceCts?.Cancel();

        if (!string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            var requestVersion = Interlocked.Increment(ref _exerciseSuggestionRequestVersion);
            var debounceCts = new CancellationTokenSource();
            _exerciseSuggestionDebounceCts = debounceCts;

            try
            {
                await Task.Delay(175, debounceCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var searchQuery = showAllForCurrentGroup ? string.Empty : ExerciseSearchQuery;
            var exercises = await _workoutLibraryService.SearchExercisesByName(
                SelectedMuscleGroup, searchQuery
            );

            if (debounceCts.IsCancellationRequested || requestVersion != _exerciseSuggestionRequestVersion)
            {
                return;
            }

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
        if (ShowNoWeightStrengthInput)
        {
            if (!string.IsNullOrWhiteSpace(Weight))
            {
                Weight = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(ResistanceAdjustment))
            {
                ResistanceAdjustment = string.Empty;
            }

            return;
        }

        if (!ShowResistanceAdjustment)
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
        SyncTimedStrengthCountdownWithTarget(resetRunningCountdown: true);
        OnPropertyChanged(nameof(HasPlannedRepRange));
        OnPropertyChanged(nameof(PlannedRepRangeSummary));
        OnPropertyChanged(nameof(UsesTimedStrengthTarget));
        NotifyTimedStrengthCountdownStateChanged();
        OnPropertyChanged(nameof(ShowQuickAddResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowQuickAddStandardStrengthGrid));
        OnPropertyChanged(nameof(ShowManualResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowManualStandardStrengthGrid));
        OnPropertyChanged(nameof(StrengthTargetValue));
        OnPropertyChanged(nameof(CurrentRepsSummary));
        OnPropertyChanged(nameof(RepsLabel));
        OnPropertyChanged(nameof(CanEditPlannedTargets));
        OnPropertyChanged(nameof(ShowReadOnlyPlannedTargets));
        OnPropertyChanged(nameof(HasPlannedTargetRpe));
        OnPropertyChanged(nameof(PlannedTargetRpeSummary));
        OnPropertyChanged(nameof(PlannedTargetRpeValue));
        OnPropertyChanged(nameof(PlannedTargetRpeInput));
        OnPropertyChanged(nameof(HasPlannedTargetRest));
        OnPropertyChanged(nameof(PlannedTargetRestSummary));
        OnPropertyChanged(nameof(PlannedTargetRestValue));
        OnPropertyChanged(nameof(PlannedTargetRestInput));
        OnPropertyChanged(nameof(ShowQuickEditTargetRpe));
        OnPropertyChanged(nameof(ShowQuickEditTargetRest));
        OnPropertyChanged(nameof(ShowQuickEditTargetsColumn));
        OnPropertyChanged(nameof(HasBodyWeight));
        OnPropertyChanged(nameof(IsBodyweightExercise));
        OnPropertyChanged(nameof(IsPerSideDumbbellExercise));
        OnPropertyChanged(nameof(ShowResistanceAdjustment));
        OnPropertyChanged(nameof(ShowNoWeightStrengthInput));
        OnPropertyChanged(nameof(ShowStandardWeightInput));
        OnPropertyChanged(nameof(ShowQuickAddWeightValueEditor));
        OnPropertyChanged(nameof(ShowManualWeightValueEditor));
        OnPropertyChanged(nameof(ShowStrengthTargetEditorsWithOptionalLoad));
        OnPropertyChanged(nameof(ShowStandaloneWeightValueInput));
        OnPropertyChanged(nameof(ShowDumbbellWeightHelper));
        OnPropertyChanged(nameof(HasWeightHelperText));
        OnPropertyChanged(nameof(ShowWeightTotalPreview));
        OnPropertyChanged(nameof(WeightLabel));
        OnPropertyChanged(nameof(WeightPlaceholder));
        OnPropertyChanged(nameof(WeightSummaryPrefix));
        OnPropertyChanged(nameof(WeightSummaryText));
        OnPropertyChanged(nameof(QuickAddWeightSummaryText));
        OnPropertyChanged(nameof(HasQuickAddWeightSummary));
        OnPropertyChanged(nameof(WeightHelperText));
        OnPropertyChanged(nameof(WeightTotalPreviewText));
        OnPropertyChanged(nameof(BaseBodyWeightSummary));
        OnPropertyChanged(nameof(EffectiveLoadSummary));
        OnPropertyChanged(nameof(ResistanceAdjustmentDisplay));
        OnPropertyChanged(nameof(CanAddWorkout));
        OnPropertyChanged(nameof(CanSaveWarmupSet));
    }

    public void RefreshBodyweightState()
    {
        NotifyBodyweightStateChanged();
    }

    private void NotifyManualWorkoutModeChanged()
    {
        OnPropertyChanged(nameof(ShowQuickAddCard));
        OnPropertyChanged(nameof(ShowStandaloneWeightField));
        OnPropertyChanged(nameof(ShowStandaloneWeightEditor));
        OnPropertyChanged(nameof(ShowQuickAddStandaloneWeightEditor));
        OnPropertyChanged(nameof(ShowQuickAddWeightValueEditor));
        OnPropertyChanged(nameof(ShowQuickAddResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowQuickAddStandardStrengthGrid));
        OnPropertyChanged(nameof(ShowManualStandaloneWeightEditor));
        OnPropertyChanged(nameof(ShowManualWeightValueEditor));
        OnPropertyChanged(nameof(ShowManualResistanceAdjustmentLayout));
        OnPropertyChanged(nameof(ShowManualStandardStrengthGrid));
        OnPropertyChanged(nameof(ShowNoWeightStrengthInput));
        OnPropertyChanged(nameof(ShowStrengthTargetEditorsWithOptionalLoad));
        OnPropertyChanged(nameof(ShowStrengthFields));
        OnPropertyChanged(nameof(ShowCardioFields));
        OnPropertyChanged(nameof(HasSelectedManualStrengthExercise));
        OnPropertyChanged(nameof(ShowManualStrengthExerciseDetails));
        OnPropertyChanged(nameof(ShowWarmupSetOption));
        OnPropertyChanged(nameof(ExercisePickerTitle));
        OnPropertyChanged(nameof(UsesTimedStrengthTarget));
        NotifyTimedStrengthCountdownStateChanged();
        OnPropertyChanged(nameof(StrengthTargetValue));
        OnPropertyChanged(nameof(CurrentRepsSummary));
        OnPropertyChanged(nameof(RepsLabel));
        OnPropertyChanged(nameof(CanDecreaseReps));
        OnPropertyChanged(nameof(CanAddWorkout));
        OnPropertyChanged(nameof(CanSaveWarmupSet));
    }

    private async Task RefreshExerciseOptionsAsync()
    {
        var options = IsManualCardio || string.Equals(SelectedMuscleGroup, "Cardio", StringComparison.OrdinalIgnoreCase)
            ? ManualCardioExerciseOptions
            : string.IsNullOrWhiteSpace(SelectedMuscleGroup)
                ? []
                : (await _workoutLibraryService.SearchExercisesByName(SelectedMuscleGroup, string.Empty))
                    .Select(exercise => exercise.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToArray();

        ExerciseOptions.Clear();
        foreach (var option in options)
        {
            ExerciseOptions.Add(option);
        }
    }

    public void AdjustResistanceAdjustment(double delta)
    {
        double.TryParse(ResistanceAdjustment, out var adjustment);
        adjustment = Math.Clamp(
            adjustment + delta,
            -InputSanitizer.MaxResistanceAdjustment,
            InputSanitizer.MaxResistanceAdjustment);
        ResistanceAdjustment = adjustment.ToString("0");
    }

    public void AdjustReps(int delta)
    {
        if (UsesTimedStrengthTarget)
        {
            var currentDuration = 0;
            int.TryParse(TimedStrengthSecondsText, out currentDuration);
            currentDuration = Math.Clamp(currentDuration + (delta * 5), 5, InputSanitizer.MaxTimedStrengthSeconds);
            TimedStrengthSecondsText = currentDuration.ToString();
            return;
        }

        var currentReps = 0;
        int.TryParse(Reps, out currentReps);
        currentReps = Math.Clamp(currentReps + delta, 1, InputSanitizer.MaxReps);

        if (HasPlannedRepRange)
        {
            currentReps = Math.Clamp(currentReps, _plannedMinReps!.Value, _plannedMaxReps!.Value);
        }

        Reps = currentReps.ToString();
    }

    public void AdjustSets(int delta)
    {
        var currentSets = 0;
        int.TryParse(PlannedSetsInput, out currentSets);
        var completedSets = _selectedRecommendation?.IsWeightLifting == true
            ? GetCompletedPlanWorkoutSetCount(_selectedRecommendation.Workout)
            : 0;
        currentSets = Math.Clamp(currentSets + delta, Math.Max(1, completedSets), InputSanitizer.MaxSets);
        PlannedSetsInput = currentSets.ToString();
    }

    public void AdjustActiveSets(int delta)
    {
        var currentSets = 0;
        int.TryParse(ActiveSets, out currentSets);
        currentSets = Math.Clamp(currentSets + delta, 1, GetMaxSelectableSets());
        ActiveSets = currentSets.ToString();
    }

    private int GetMaxSelectableSets()
    {
        if (_selectedRecommendation?.IsWeightLifting == true && _selectedRecommendation.RemainingSets > 0)
        {
            return _selectedRecommendation.RemainingSets;
        }

        return InputSanitizer.MaxSets;
    }

    private void NotifyTargetSetStateChanged()
    {
        OnPropertyChanged(nameof(PlannedSetsInput));
        OnPropertyChanged(nameof(CurrentSetSummary));
        OnPropertyChanged(nameof(CanDecreaseSets));
        OnPropertyChanged(nameof(CanIncreaseSets));
        OnPropertyChanged(nameof(MaxSelectableSets));
    }

    private void NotifyActiveSetProgressStateChanged()
    {
        OnPropertyChanged(nameof(MaxSelectableSets));
        OnPropertyChanged(nameof(CanDecreaseActiveSets));
        OnPropertyChanged(nameof(CanIncreaseActiveSets));
        OnPropertyChanged(nameof(ShowQuickSetProgress));
        OnPropertyChanged(nameof(RemainingSetsAfterCurrentSave));
        OnPropertyChanged(nameof(IsCompletingSelectedPlanExercise));
        OnPropertyChanged(nameof(QuickSetProgressText));
    }

    private void StartTimedStrengthCountdown()
    {
        if (!UsesTimedStrengthTarget)
        {
            return;
        }

        var targetSeconds = GetConfiguredTimedStrengthSeconds();
        if (targetSeconds <= 0)
        {
            return;
        }

        CancelTimedStrengthCountdown(resetToTarget: false);
        _timedStrengthCountdownRemainingSeconds = targetSeconds;
        _hasTimedStrengthCountdownCompleted = false;
        _isTimedStrengthCountdownRunning = true;
        NotifyTimedStrengthCountdownStateChanged();

        var countdownCancellation = new CancellationTokenSource();
        _timedStrengthCountdownCancellation = countdownCancellation;
        _ = RunTimedStrengthCountdownAsync(countdownCancellation.Token);
    }

    private async Task RunTimedStrengthCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_timedStrengthCountdownRemainingSeconds > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _timedStrengthCountdownRemainingSeconds = Math.Max(0, _timedStrengthCountdownRemainingSeconds - 1);
                NotifyTimedStrengthCountdownStateChanged();
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                _isTimedStrengthCountdownRunning = false;
                _hasTimedStrengthCountdownCompleted = true;
                NotifyTimedStrengthCountdownStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SyncTimedStrengthCountdownWithTarget(bool resetRunningCountdown)
    {
        if (resetRunningCountdown)
        {
            CancelTimedStrengthCountdown(resetToTarget: false);
        }

        if (!_isTimedStrengthCountdownRunning)
        {
            _timedStrengthCountdownRemainingSeconds = GetConfiguredTimedStrengthSeconds();
            _hasTimedStrengthCountdownCompleted = false;
        }

        NotifyTimedStrengthCountdownStateChanged();
    }

    private void CancelTimedStrengthCountdown(bool resetToTarget)
    {
        _timedStrengthCountdownCancellation?.Cancel();
        _timedStrengthCountdownCancellation?.Dispose();
        _timedStrengthCountdownCancellation = null;
        _isTimedStrengthCountdownRunning = false;

        if (resetToTarget)
        {
            _timedStrengthCountdownRemainingSeconds = GetConfiguredTimedStrengthSeconds();
            _hasTimedStrengthCountdownCompleted = false;
        }
    }

    private int GetConfiguredTimedStrengthSeconds()
        => int.TryParse(TimedStrengthSecondsText, out var timedSeconds) && timedSeconds > 0
            ? timedSeconds
            : 0;

    private int GetEffectiveTimedStrengthCountdownSeconds()
    {
        if (_timedStrengthCountdownRemainingSeconds > 0 || _isTimedStrengthCountdownRunning || _hasTimedStrengthCountdownCompleted)
        {
            return Math.Max(0, _timedStrengthCountdownRemainingSeconds);
        }

        return GetConfiguredTimedStrengthSeconds();
    }

    private void NotifyTimedStrengthCountdownStateChanged()
    {
        OnPropertyChanged(nameof(ShowTimedQuickAddCountdown));
        OnPropertyChanged(nameof(ShowTimedManualCountdown));
        OnPropertyChanged(nameof(ShowQuickAddRepTargetEditor));
        OnPropertyChanged(nameof(TimedStrengthCountdownDisplay));
        OnPropertyChanged(nameof(TimedStrengthStartButtonText));
        OnPropertyChanged(nameof(CanStartTimedStrengthCountdown));
    }

    private void ApplyTimedStrengthDefaultsIfNeeded(string? exerciseName)
    {
        if (!Workout.PrefersTimedTarget(exerciseName))
        {
            TimedStrengthSecondsText = string.Empty;
            return;
        }

        var resolvedSeconds = GetLastUsedTimedStrengthSeconds(exerciseName);
        if (resolvedSeconds <= 0)
        {
            resolvedSeconds = GetDefaultTimedStrengthSeconds(exerciseName);
        }

        TimedStrengthSecondsText = resolvedSeconds > 0
            ? resolvedSeconds.ToString()
            : string.Empty;
    }

    private int GetLastUsedTimedStrengthSeconds(string? exerciseName)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            return 0;
        }

        return _workoutHistory
            .Where(historyWorkout =>
                historyWorkout.Type == WorkoutType.WeightLifting &&
                string.Equals(historyWorkout.Name, exerciseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(historyWorkout => historyWorkout.StartTime)
            .Select(historyWorkout => historyWorkout.TimedTargetSeconds)
            .FirstOrDefault(seconds => seconds > 0);
    }

    private static int GetDefaultTimedStrengthSeconds(string? exerciseName)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            return 0;
        }

        var normalized = exerciseName.Trim().ToLowerInvariant();
        if (normalized.Equals("plank", StringComparison.Ordinal))
        {
            return 30;
        }

        if (normalized.Contains("suitcase carry", StringComparison.Ordinal))
        {
            return 35;
        }

        if (normalized.Contains("farmer carry", StringComparison.Ordinal))
        {
            return 40;
        }

        if (normalized.Contains("balance hold", StringComparison.Ordinal) ||
            normalized.Contains("stance hold", StringComparison.Ordinal))
        {
            return 30;
        }

        return normalized.Contains("carry", StringComparison.Ordinal) ? 40 : 30;
    }

    private static string FormatCountdownDisplay(int totalSeconds)
    {
        var safeSeconds = Math.Max(0, totalSeconds);
        var time = TimeSpan.FromSeconds(safeSeconds);
        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    public void AdjustBodyweightDisplayedWeight(double delta)
    {
        if (!ShowResistanceAdjustment)
        {
            return;
        }

        var baseWeight = _bodyWeightService.GetBodyWeight() ?? 0;
        var currentWeight = baseWeight;

        if (double.TryParse(Weight, out var parsedWeight) && parsedWeight > 0)
        {
            currentWeight = parsedWeight;
        }

        currentWeight = Math.Clamp(currentWeight + delta, 0, InputSanitizer.MaxBodyWeight);
        Weight = currentWeight.ToString("0");
        ResistanceAdjustment = (currentWeight - baseWeight).ToString("0");
    }

    public void AdjustDisplayedWeight(double delta)
    {
        if (ShowResistanceAdjustment)
        {
            return;
        }

        var currentWeight = 0.0;
        if (double.TryParse(Weight, out var parsedWeight) && parsedWeight > 0)
        {
            currentWeight = parsedWeight;
        }

        currentWeight = Math.Clamp(currentWeight + delta, 0, InputSanitizer.MaxWorkoutWeight);
        Weight = currentWeight.ToString("0.#");
    }

    public void CommitBodyweightWeightInput()
    {
        if (!ShowResistanceAdjustment || !HasBodyWeight)
        {
            return;
        }

        var baseWeight = _bodyWeightService.GetBodyWeight() ?? 0;
        if (!double.TryParse(Weight, out var enteredWeight) || enteredWeight <= 0)
        {
            Weight = baseWeight.ToString("0.#");
            return;
        }

        enteredWeight = Math.Clamp(enteredWeight, 0, InputSanitizer.MaxBodyWeight);
        var adjustment = enteredWeight - baseWeight;
        ResistanceAdjustment = adjustment.ToString("0");
        Weight = baseWeight.ToString("0.#");
    }

    public void BeginStandardWeightEdit()
    {
        if (!ShowStandardWeightInput)
        {
            return;
        }
    }

    public void CommitStandardWeightInput()
    {
        if (!ShowStandardWeightInput)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Weight) &&
            !IsZeroWeightAllowedExercise(Name) &&
            !IsZeroWeightAllowedExercise(ExerciseSearchQuery))
        {
            Weight = "0";
        }
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
        OnPropertyChanged(nameof(PlannedTargetRpeValue));
        OnPropertyChanged(nameof(PlannedTargetRpeInput));
        OnPropertyChanged(nameof(ShowQuickEditTargetRpe));
        OnPropertyChanged(nameof(ShowQuickEditTargetsColumn));
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
        OnPropertyChanged(nameof(PlannedTargetRestValue));
        OnPropertyChanged(nameof(PlannedTargetRestInput));
        OnPropertyChanged(nameof(ShowQuickEditTargetRest));
        OnPropertyChanged(nameof(ShowQuickEditTargetsColumn));
    }

    private void ClearPlannedTargetRest()
    {
        ApplyPlannedTargetRest(null);
    }

    private static int GetDefaultRepsForWorkout(Workout workout)
    {
        if (workout.MinReps.HasValue && workout.MaxReps.HasValue && workout.MaxReps.Value >= workout.MinReps.Value)
        {
            return workout.MaxReps.Value;
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
               normalized.Contains("band pull-apart") ||
               normalized.Contains("dip") ||
               normalized.Contains("bodyweight") ||
               normalized.Contains("wall push-up") ||
               normalized.Contains("incline push-up") ||
               normalized.Contains("elevated push-up") ||
               normalized.Contains("pike push-up") ||
               normalized.Contains("step-up") ||
               normalized.Contains("step up") ||
               normalized.Contains("glute bridge") ||
               normalized.Contains("dead bug") ||
               normalized.Contains("bird dog") ||
               normalized.Contains("sit-to-stand") ||
               normalized.Contains("sit to stand") ||
               normalized.Contains("single-leg balance hold") ||
               normalized.Contains("single leg balance hold") ||
               normalized.Contains("heel-to-toe walk") ||
               normalized.Contains("heel to toe walk") ||
               normalized.Contains("plank knee drive") ||
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
               normalized.Contains("bicycle crunch") ||
               normalized.Contains("sit up") ||
               normalized.Contains("sit-up") ||
               normalized.Contains("leg raise") ||
               normalized.Contains("hollow hold") ||
               normalized.Contains("mountain climber") ||
               normalized.Contains("pallof press") ||
               normalized.Contains("resistance band row") ||
               normalized.Contains("hip hinge drill") ||
               normalized.Contains("bodyweight squat") ||
               normalized.Contains("glute bridge") ||
               normalized.Contains("good morning") ||
               normalized.Contains("air squat") ||
               normalized.Contains("walking lunge") ||
               normalized.Contains("bodyweight lunge") ||
               normalized.Contains("jumping jack") ||
               normalized.Contains("burpee");
    }

    private static bool IsZeroLoadOnlyExercise(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToLowerInvariant();
        return normalized.Contains("plank") ||
               normalized.Contains("dead bug") ||
               normalized.Contains("bird dog") ||
               normalized.Contains("crunch") ||
               normalized.Contains("bicycle crunch") ||
               normalized.Contains("sit up") ||
               normalized.Contains("sit-up") ||
               normalized.Contains("leg raise") ||
               normalized.Contains("flutter kick") ||
               normalized.Contains("hollow hold") ||
               normalized.Contains("mountain climber") ||
               normalized.Contains("wall push-up") ||
               normalized.Contains("incline push-up") ||
               normalized.Contains("elevated push-up") ||
               normalized.Contains("band pull-apart") ||
               normalized.Contains("pallof press") ||
               normalized.Contains("resistance band row") ||
               normalized.Contains("hip hinge drill") ||
               normalized.Contains("bodyweight squat") ||
               normalized.Contains("air squat") ||
               normalized.Contains("reverse lunge") ||
               normalized.Contains("walking lunge") ||
               normalized.Contains("bodyweight lunge") ||
               normalized.Contains("supported split squat") ||
               normalized.Contains("step-up") ||
               normalized.Contains("step up") ||
               normalized.Contains("sit-to-stand") ||
               normalized.Contains("sit to stand") ||
               normalized.Contains("glute bridge") ||
               normalized.Contains("good morning") ||
               normalized.Contains("pike push-up") ||
               normalized.Contains("balance hold") ||
               normalized.Contains("stance hold") ||
               normalized.Contains("heel-to-toe walk") ||
               normalized.Contains("heel to toe walk") ||
               normalized.Contains("jumping jack") ||
               normalized.Contains("burpee");
    }

    private static bool SupportsBodyweightResistanceAdjustment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToLowerInvariant();
        return (normalized.Contains("pull-up") ||
                normalized.Contains("pull up") ||
                normalized.Contains("chin-up") ||
                normalized.Contains("chin up") ||
                normalized.Contains("dip"));
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
                 normalized.Contains("deadlift") ||
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

    private string GetDefaultWeightForExerciseName(string exerciseName)
    {
        var lastUsedWeight = GetLastUsedWeight(exerciseName);
        if (lastUsedWeight.HasValue && lastUsedWeight.Value > 0)
        {
            return GetDisplayWeightForExercise(lastUsedWeight.Value, exerciseName);
        }

        return IsZeroWeightAllowedExercise(exerciseName) ? string.Empty : "0";
    }

    private static string GetDefaultWeightForWorkout(Workout workout, double? historicalWeight)
    {
        if (workout.Type != WorkoutType.WeightLifting)
        {
            return string.Empty;
        }

        if (historicalWeight.HasValue && historicalWeight.Value > 0)
        {
            return GetDisplayWeightForExercise(historicalWeight.Value, workout.Name);
        }

        return workout.Weight > 0
            ? GetDisplayWeightForExercise(workout.Weight, workout.Name)
            : IsZeroWeightAllowedExercise(workout.Name)
                ? string.Empty
                : "0";
    }

    private static string GetRecommendationWeightPrefix() => "Weight";

    private static string GetRecommendationWeightHelperText(string? exerciseName)
    {
        return string.Empty;
    }

    private static string GetRecommendationWeightValue(Workout workout, double? lastUsedWeight)
    {
        if (workout.Type != WorkoutType.WeightLifting)
        {
            return string.Empty;
        }

        var weightToDisplay = lastUsedWeight ?? workout.Weight;
        if (weightToDisplay <= 0)
        {
            return IsZeroWeightAllowedExercise(workout.Name) ? string.Empty : "0";
        }

        return GetDisplayWeightForExercise(weightToDisplay, workout.Name);
    }

    private static string GetRecommendationRepText(Workout workout)
    {
        if (workout.Type != WorkoutType.WeightLifting)
        {
            return string.Empty;
        }

        if (!workout.HasRepTarget)
        {
            return string.Empty;
        }

        return workout.HasRepRange
            ? $"Reps: {workout.MinReps}-{workout.MaxReps}"
            : $"Reps: {workout.Reps}";
    }

    private static string GetRecommendationDurationText(Workout workout)
    {
        return workout.HasDuration && (workout.Type == WorkoutType.Cardio || workout.HasTimedTarget)
            ? $"Time: {workout.DurationValueDisplay}"
            : string.Empty;
    }

    private static string GetRecommendationDistanceText(Workout workout)
    {
        return workout.Type == WorkoutType.Cardio && workout.DistanceMiles > 0
            ? $"Distance: {workout.DistanceMiles:0.##} mi"
            : string.Empty;
    }

    private string BuildPlanRecommendationSignature()
    {
        var activePlanName = _workoutScheduleService.ActivePlan?.Name?.Trim() ?? string.Empty;
        var todaysPlannedWorkouts = _workoutScheduleService.GetActivePlanWorkoutsForDate(DateTime.Today);
        var carryoverPlanWorkouts = GetCarryoverPlanWorkouts(DateTime.Today);

        return string.Join("||",
            DateTime.Today.DayOfWeek,
            activePlanName,
            string.Join("::", todaysPlannedWorkouts.Select(workout => string.Join("|",
                workout.Day,
                workout.Type,
                workout.Name?.Trim(),
                workout.MuscleGroup?.Trim(),
                workout.Sets,
                workout.Reps,
                workout.DurationMinutes,
                workout.DurationSeconds,
                workout.DistanceMiles,
                workout.Steps,
                workout.PlanWeekNumber,
                workout.TargetRpe,
                workout.TargetRestRange?.Trim()))),
            string.Join("::", carryoverPlanWorkouts.Select(workout => string.Join("|",
                "carryover",
                workout.Day,
                workout.Type,
                workout.Name?.Trim(),
                workout.MuscleGroup?.Trim(),
                workout.Sets,
                workout.Reps,
                workout.DurationMinutes,
                workout.DurationSeconds,
                workout.DistanceMiles,
                workout.Steps,
                workout.PlanWeekNumber,
                workout.TargetRpe,
                workout.TargetRestRange?.Trim()))));
    }

    private void NotifyExerciseImageStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedExerciseInfo));
        OnPropertyChanged(nameof(ShowQuickEditExerciseInfo));
        OnPropertyChanged(nameof(HasSelectedExerciseImage));
        OnPropertyChanged(nameof(SelectedExerciseImageSource));
    }

    #endregion
}
