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
    private string _sessionName = "Cardio Session";
    private bool _isTracking;
    private bool _useStepTracking;
    private int _plannedDurationMinutes;
    private double _plannedDistanceMiles;
    private double? _plannedTargetRpe;

    public CardioWorkoutViewModel(IWorkoutService workoutService, IStepCounterService stepCounterService)
    {
        _workoutService = workoutService;
        _stepCounterService = stepCounterService;
        _stepCounterService.StepsUpdated += OnStepsUpdated;

        ApplyWorkoutTemplateIfAvailable();
        RestoreTrackingState();
    }

    public int SessionSteps
    {
        get => _sessionSteps;
        set => SetProperty(ref _sessionSteps, value);
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
            if (SetProperty(ref _sessionName, value) && IsTracking)
            {
                Preferences.Set(NamePreferenceKey, value ?? string.Empty);
            }
        }
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

    public ICommand StartSessionCommand => new Command(async () => await StartSessionAsync());
    public ICommand StopSessionCommand => new Command(async () => await StopSessionAsync());

    private void ApplyWorkoutTemplateIfAvailable()
    {
        if (WorkoutTemplateCache.Template is not Workout template || template.Type != WorkoutType.Cardio)
        {
            return;
        }

        SessionName = string.IsNullOrWhiteSpace(template.Name) ? "Cardio Session" : template.Name;
        DistanceMilesText = template.DistanceMiles > 0 ? template.DistanceMiles.ToString("0.#") : string.Empty;
        PlannedDurationMinutes = template.DurationMinutes;
        PlannedDistanceMiles = template.DistanceMiles;
        PlannedTargetRpe = template.TargetRpe;
        UseStepTracking = template.Steps > 0;
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
        UseStepTracking = Preferences.Get(UseStepTrackingPreferenceKey, false);
        _sessionStartedAtUtc = startedAtUtc;
        IsTracking = true;
        RefreshElapsedTime();

        if (UseStepTracking)
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

        if (UseStepTracking && !await EnsureActivityRecognitionPermissionAsync())
        {
            return;
        }

        SessionSteps = 0;
        _sessionStartedAtUtc = DateTimeOffset.UtcNow;
        ElapsedTime = TimeSpan.Zero;
        IsTracking = true;
        SaveTrackingState();

        if (UseStepTracking)
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

        if (UseStepTracking)
        {
            _stepCounterService.StopTracking();
        }

        RefreshElapsedTime();
        IsTracking = false;
        ClearTrackingState();

        double.TryParse(DistanceMilesText, out var parsedDistanceMiles);
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
            Steps = UseStepTracking ? SessionSteps : 0,
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
        if (UseStepTracking)
        {
            SessionSteps = steps;
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
