using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.PlatformPermissions;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class CardioWorkoutViewModel : BaseViewModel
{
    private static readonly string[] CommonCardioNames =
    [
        "Run",
        "Walk",
        "Bike Ride",
        "Row",
        "Elliptical",
        "Stair Climber",
        "Hike",
        "Swim",
        "Jump Rope"
    ];

    private const string IsTrackingPreferenceKey = "cardio_session.is_tracking";
    private const string StartedAtUtcPreferenceKey = "cardio_session.started_at_utc";
    private const string NamePreferenceKey = "cardio_session.name";
    private const string DistancePreferenceKey = "cardio_session.distance";
    private const string UseStepTrackingPreferenceKey = "cardio_session.use_step_tracking";

    private readonly IWorkoutService _workoutService;
    private readonly IStepCounterService _stepCounterService;
    private CancellationTokenSource? _sessionLoopCancellation;
    private DateTimeOffset? _sessionStartedAtUtc;
    private int _sessionSteps;
    private TimeSpan _elapsedTime = TimeSpan.Zero;
    private string _distanceMilesText = string.Empty;
    private string _sessionName = string.Empty;
    private bool _isTracking;
    private bool _useStepTracking;
    private bool _isSessionNameFocused;
    private int _plannedDurationMinutes;
    private double _plannedDistanceMiles;
    private double? _plannedTargetRpe;

    public CardioWorkoutViewModel(IWorkoutService workoutService, IStepCounterService stepCounterService)
    {
        _workoutService = workoutService;
        _stepCounterService = stepCounterService;
        _stepCounterService.StepsUpdated += OnStepsUpdated;
        _useStepTracking = true;
        CardioNameSuggestions = new ObservableCollection<string>(CommonCardioNames);

        ApplyWorkoutTemplateIfAvailable();
        RestoreTrackingState();
    }

    public int SessionSteps
    {
        get => _sessionSteps;
        set
        {
            if (SetProperty(ref _sessionSteps, value))
            {
                OnPropertyChanged(nameof(HasEstimatedDistanceFromSteps));
                OnPropertyChanged(nameof(EstimatedDistanceFromStepsText));
            }
        }
    }

    public TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            if (SetProperty(ref _elapsedTime, value))
            {
                OnPropertyChanged(nameof(ElapsedTimeDisplay));
                OnPropertyChanged(nameof(ElapsedMinutes));
            }
        }
    }

    public int ElapsedMinutes => Math.Max(0, (int)Math.Ceiling(ElapsedTime.TotalMinutes));

    public string ElapsedTimeDisplay => ElapsedTime.ToString(@"hh\:mm\:ss");

    public string DistanceMilesText
    {
        get => _distanceMilesText;
        set
        {
            if (SetProperty(ref _distanceMilesText, value) && IsTracking)
            {
                Preferences.Set(DistancePreferenceKey, value ?? string.Empty);
            }
        }
    }

    public string SessionName
    {
        get => _sessionName;
        set
        {
            if (SetProperty(ref _sessionName, value))
            {
                UpdateCardioNameSuggestions();
                OnPropertyChanged(nameof(SupportsStepTrackingForSelectedActivity));
                OnPropertyChanged(nameof(StepTrackingStatusText));
                OnPropertyChanged(nameof(HasEstimatedDistanceFromSteps));
                OnPropertyChanged(nameof(EstimatedDistanceFromStepsText));

                if (IsTracking)
                {
                    Preferences.Set(NamePreferenceKey, value ?? string.Empty);
                }
            }
        }
    }

    public ObservableCollection<string> CardioNameSuggestions { get; }

    public bool IsSessionNameFocused
    {
        get => _isSessionNameFocused;
        set => SetProperty(ref _isSessionNameFocused, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetProperty(ref _isTracking, value);
    }

    public bool UseStepTracking
    {
        get => _useStepTracking;
        set
        {
            if (SetProperty(ref _useStepTracking, value) && IsTracking)
            {
                Preferences.Set(UseStepTrackingPreferenceKey, value);
            }

            OnPropertyChanged(nameof(StepTrackingStatusText));
        }
    }

    public int PlannedDurationMinutes
    {
        get => _plannedDurationMinutes;
        set
        {
            if (SetProperty(ref _plannedDurationMinutes, value))
            {
                OnPropertyChanged(nameof(HasPlannedDuration));
                OnPropertyChanged(nameof(ShowPlannedTargets));
                OnPropertyChanged(nameof(PlannedDurationText));
            }
        }
    }

    public double PlannedDistanceMiles
    {
        get => _plannedDistanceMiles;
        set
        {
            if (SetProperty(ref _plannedDistanceMiles, value))
            {
                OnPropertyChanged(nameof(HasPlannedDistance));
                OnPropertyChanged(nameof(ShowPlannedTargets));
                OnPropertyChanged(nameof(PlannedDistanceText));
            }
        }
    }

    public double? PlannedTargetRpe
    {
        get => _plannedTargetRpe;
        set
        {
            if (SetProperty(ref _plannedTargetRpe, value))
            {
                OnPropertyChanged(nameof(HasPlannedTargetRpe));
                OnPropertyChanged(nameof(ShowPlannedTargets));
                OnPropertyChanged(nameof(PlannedTargetRpeText));
            }
        }
    }

    public bool HasPlannedDuration => PlannedDurationMinutes > 0;
    public bool HasPlannedDistance => PlannedDistanceMiles > 0;
    public bool HasPlannedTargetRpe => PlannedTargetRpe.HasValue && PlannedTargetRpe.Value > 0;
    public bool ShowPlannedTargets => HasPlannedDuration || HasPlannedDistance || HasPlannedTargetRpe;
    public string PlannedDurationText => $"Time: {PlannedDurationMinutes} min";
    public string PlannedDistanceText => $"Distance: {PlannedDistanceMiles:0.#} mi";
    public string PlannedTargetRpeText => $"RPE: {PlannedTargetRpe.GetValueOrDefault():0.#}";
    public bool SupportsStepTrackingForSelectedActivity => SupportsStepTracking(SessionName);
    public string StepTrackingStatusText => !SupportsStepTrackingForSelectedActivity
        ? "This activity does not use step tracking."
        : UseStepTracking
            ? "Step tracking will be captured automatically when supported."
            : "Step tracking is off for this session.";
    public bool HasEstimatedDistanceFromSteps => SupportsStepTrackingForSelectedActivity && GetEstimatedDistanceMiles() > 0;
    public string EstimatedDistanceFromStepsText => HasEstimatedDistanceFromSteps
        ? $"Estimated distance from steps: {GetEstimatedDistanceMiles():0.##} mi"
        : SupportsStepTrackingForSelectedActivity
            ? "Estimated distance will appear here when enough steps are tracked."
            : "Distance will need to be entered or confirmed for this activity.";

    public ICommand StartSessionCommand => new Command(async () => await StartSessionAsync());
    public ICommand StopSessionCommand => new Command(async () => await StopSessionAsync());

    private void ApplyWorkoutTemplateIfAvailable()
    {
        if (WorkoutTemplateCache.Template is not Workout template || template.Type != WorkoutType.Cardio)
        {
            return;
        }

        SessionName = string.IsNullOrWhiteSpace(template.Name) ? string.Empty : template.Name;
        DistanceMilesText = template.DistanceMiles > 0 ? template.DistanceMiles.ToString("0.#") : string.Empty;
        PlannedDurationMinutes = template.DurationMinutes;
        PlannedDistanceMiles = template.DistanceMiles;
        PlannedTargetRpe = template.TargetRpe;
        UseStepTracking = true;
        WorkoutTemplateCache.Template = null;
    }

    private void RestoreTrackingState()
    {
        if (!Preferences.Get(IsTrackingPreferenceKey, false))
        {
            return;
        }

        var startedAtText = Preferences.Get(StartedAtUtcPreferenceKey, string.Empty);
        if (!DateTimeOffset.TryParse(startedAtText, out var startedAtUtc))
        {
            ClearTrackingState();
            return;
        }

        SessionName = Preferences.Get(NamePreferenceKey, SessionName);
        DistanceMilesText = Preferences.Get(DistancePreferenceKey, DistanceMilesText);
        UseStepTracking = Preferences.Get(UseStepTrackingPreferenceKey, true);
        _sessionStartedAtUtc = startedAtUtc;
        IsTracking = true;
        RefreshElapsedTime();

        if (UseStepTracking && SupportsStepTrackingForSelectedActivity)
        {
            _stepCounterService.StartTracking();
        }

        StartSessionLoop();
    }

    private async Task StartSessionAsync()
    {
        if (IsTracking)
        {
            return;
        }

        if (!SupportsStepTrackingForSelectedActivity)
        {
            UseStepTracking = false;
        }
        else if (UseStepTracking && !await EnsureActivityRecognitionPermissionAsync())
        {
            UseStepTracking = false;
            OnPropertyChanged(nameof(StepTrackingStatusText));
        }

        SessionSteps = 0;
        _sessionStartedAtUtc = DateTimeOffset.UtcNow;
        ElapsedTime = TimeSpan.Zero;
        IsTracking = true;
        SaveTrackingState();

        if (UseStepTracking && SupportsStepTrackingForSelectedActivity)
        {
            _stepCounterService.StartTracking();
        }

        StartSessionLoop();
    }

    private async Task StopSessionAsync()
    {
        if (!IsTracking || !_sessionStartedAtUtc.HasValue)
        {
            return;
        }

        _sessionLoopCancellation?.Cancel();

        if (UseStepTracking && SupportsStepTrackingForSelectedActivity)
        {
            _stepCounterService.StopTracking();
        }

        RefreshElapsedTime();
        IsTracking = false;
        ClearTrackingState();

        var parsedDistanceMiles = await ResolveDistanceMilesAsync();
        var sessionEndedAt = DateTimeOffset.UtcNow;
        var sessionStartedAt = _sessionStartedAtUtc.Value;

        var workout = new Workout(
            name: string.IsNullOrWhiteSpace(SessionName) ? "Cardio Session" : SessionName.Trim(),
            weight: 0,
            reps: 0,
            sets: 0,
            muscleGroup: "Cardio",
            startTime: sessionStartedAt.LocalDateTime,
            type: WorkoutType.Cardio,
            gymLocation: "Outdoor")
        {
            Steps = UseStepTracking && SupportsStepTrackingForSelectedActivity ? SessionSteps : 0,
            DurationMinutes = Math.Max(1, (int)Math.Ceiling((sessionEndedAt - sessionStartedAt).TotalMinutes)),
            DistanceMiles = parsedDistanceMiles,
            EndTime = sessionEndedAt.LocalDateTime,
            TargetRpe = PlannedTargetRpe
        };

        await _workoutService.AddWorkout(workout);

        _sessionStartedAtUtc = null;
        SessionSteps = 0;
        ElapsedTime = TimeSpan.Zero;
        DistanceMilesText = HasPlannedDistance ? PlannedDistanceMiles.ToString("0.#") : string.Empty;
        SessionName = string.Empty;
        UseStepTracking = true;
        OnPropertyChanged(nameof(StepTrackingStatusText));
    }

    private void StartSessionLoop()
    {
        _sessionLoopCancellation?.Cancel();
        _sessionLoopCancellation = new CancellationTokenSource();
        _ = RunSessionClockAsync(_sessionLoopCancellation.Token);
    }

    private void RefreshElapsedTime()
    {
        if (!_sessionStartedAtUtc.HasValue)
        {
            ElapsedTime = TimeSpan.Zero;
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - _sessionStartedAtUtc.Value;
        ElapsedTime = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
    }

    private void SaveTrackingState()
    {
        Preferences.Set(IsTrackingPreferenceKey, true);
        Preferences.Set(StartedAtUtcPreferenceKey, _sessionStartedAtUtc?.ToString("O") ?? string.Empty);
        Preferences.Set(NamePreferenceKey, SessionName ?? string.Empty);
        Preferences.Set(DistancePreferenceKey, DistanceMilesText ?? string.Empty);
        Preferences.Set(UseStepTrackingPreferenceKey, UseStepTracking);
    }

    private void ClearTrackingState()
    {
        Preferences.Remove(IsTrackingPreferenceKey);
        Preferences.Remove(StartedAtUtcPreferenceKey);
        Preferences.Remove(NamePreferenceKey);
        Preferences.Remove(DistancePreferenceKey);
        Preferences.Remove(UseStepTrackingPreferenceKey);
    }

    private async Task RunSessionClockAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                RefreshElapsedTime();
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnStepsUpdated(object? sender, int steps)
    {
        if (UseStepTracking && SupportsStepTrackingForSelectedActivity)
        {
            SessionSteps = steps;
        }
    }

    private async Task<double> ResolveDistanceMilesAsync()
    {
        if (double.TryParse(DistanceMilesText, out var parsedDistanceMiles) && parsedDistanceMiles > 0)
        {
            return parsedDistanceMiles;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return 0;
        }

        var promptResult = await page.DisplayPromptAsync(
            "Distance",
            GetDistancePromptMessage(),
            accept: "Save",
            cancel: "Skip",
            placeholder: "Miles",
            initialValue: GetInitialDistanceValue(),
            keyboard: Keyboard.Numeric);

        if (double.TryParse(promptResult, out parsedDistanceMiles) && parsedDistanceMiles > 0)
        {
            DistanceMilesText = parsedDistanceMiles.ToString("0.#");
            return parsedDistanceMiles;
        }

        return 0;
    }

    private double GetEstimatedDistanceMiles()
    {
        if (SessionSteps <= 0)
        {
            return 0;
        }

        return Math.Round(SessionSteps / 2000d, 2);
    }

    private string GetInitialDistanceValue()
    {
        if (HasPlannedDistance)
        {
            return PlannedDistanceMiles.ToString("0.#");
        }

        var estimatedDistance = GetEstimatedDistanceMiles();
        return estimatedDistance > 0 ? estimatedDistance.ToString("0.##") : string.Empty;
    }

    private string GetDistancePromptMessage()
    {
        var estimatedDistance = GetEstimatedDistanceMiles();
        if (SupportsStepTrackingForSelectedActivity && estimatedDistance > 0)
        {
            return $"We estimated {estimatedDistance:0.##} miles from {SessionSteps:N0} steps. Edit it if needed.";
        }

        return "Enter the distance for this cardio workout.";
    }

    private static bool SupportsStepTracking(string? sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return true;
        }

        var normalized = sessionName.Trim().ToLowerInvariant();
        return normalized.Contains("run") ||
               normalized.Contains("walk") ||
               normalized.Contains("hike") ||
               normalized.Contains("stair");
    }

    public void SelectCardioName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        SessionName = name;
        IsSessionNameFocused = false;
    }

    public void ShowAllCardioNameSuggestions()
    {
        CardioNameSuggestions.Clear();
        foreach (var suggestion in CommonCardioNames)
        {
            CardioNameSuggestions.Add(suggestion);
        }
    }

    private void UpdateCardioNameSuggestions()
    {
        var query = SessionName?.Trim() ?? string.Empty;
        var suggestions = string.IsNullOrWhiteSpace(query)
            ? CommonCardioNames
            : CommonCardioNames
                .Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        CardioNameSuggestions.Clear();
        foreach (var suggestion in suggestions)
        {
            CardioNameSuggestions.Add(suggestion);
        }
    }

    private static async Task<bool> EnsureActivityRecognitionPermissionAsync()
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return true;
        }

        var status = await MauiPermissions.CheckStatusAsync<ActivityRecognitionPermission>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        status = await MauiPermissions.RequestAsync<ActivityRecognitionPermission>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
        {
            await page.DisplayAlert(
                "Permission Needed",
                "Allow activity recognition if you want the session to capture steps from your phone or watch.",
                "OK");
        }

        return false;
#else
        await Task.CompletedTask;
        return true;
#endif
    }
}
