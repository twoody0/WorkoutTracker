using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
using WorkoutTracker.Models;
using WorkoutTracker.PlatformPermissions;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class CardioWorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;
    private readonly IStepCounterService _stepCounterService;
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _sessionLoopCancellation;
    private int _sessionSteps;
    private int _elapsedMinutes;
    private string _distanceMilesText = string.Empty;
    private string _sessionName = "Cardio Session";
    private bool _isTracking;
    private bool _useStepTracking;

    public CardioWorkoutViewModel(IWorkoutService workoutService, IStepCounterService stepCounterService)
    {
        _workoutService = workoutService;
        _stepCounterService = stepCounterService;
        _stepCounterService.StepsUpdated += OnStepsUpdated;
    }

    public int SessionSteps
    {
        get => _sessionSteps;
        set => SetProperty(ref _sessionSteps, value);
    }

    public int ElapsedMinutes
    {
        get => _elapsedMinutes;
        set => SetProperty(ref _elapsedMinutes, value);
    }

    public string DistanceMilesText
    {
        get => _distanceMilesText;
        set => SetProperty(ref _distanceMilesText, value);
    }

    public string SessionName
    {
        get => _sessionName;
        set => SetProperty(ref _sessionName, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetProperty(ref _isTracking, value);
    }

    public bool UseStepTracking
    {
        get => _useStepTracking;
        set => SetProperty(ref _useStepTracking, value);
    }

    public ICommand StartSessionCommand => new Command(async () => await StartSessionAsync());
    public ICommand StopSessionCommand => new Command(async () => await StopSessionAsync());

    private async Task StartSessionAsync()
    {
        if (UseStepTracking && !await EnsureActivityRecognitionPermissionAsync())
        {
            return;
        }

        SessionSteps = 0;
        ElapsedMinutes = 0;
        _stopwatch.Restart();

        _sessionLoopCancellation?.Cancel();
        _sessionLoopCancellation = new CancellationTokenSource();
        _ = RunSessionClockAsync(_sessionLoopCancellation.Token);

        if (UseStepTracking)
        {
            _stepCounterService.StartTracking();
        }

        IsTracking = true;
    }

    private async Task StopSessionAsync()
    {
        _sessionLoopCancellation?.Cancel();
        _stopwatch.Stop();

        if (UseStepTracking)
        {
            _stepCounterService.StopTracking();
        }

        IsTracking = false;
        ElapsedMinutes = Math.Max(1, (int)Math.Ceiling(_stopwatch.Elapsed.TotalMinutes));
        double.TryParse(DistanceMilesText, out var parsedDistanceMiles);

        var workout = new Workout(
            name: string.IsNullOrWhiteSpace(SessionName) ? "Cardio Session" : SessionName.Trim(),
            weight: 0,
            reps: 0,
            sets: 0,
            muscleGroup: "Cardio",
            startTime: DateTime.Now.AddMinutes(-ElapsedMinutes),
            type: WorkoutType.Cardio,
            gymLocation: "Outdoor")
        {
            Steps = UseStepTracking ? SessionSteps : 0,
            DurationMinutes = ElapsedMinutes,
            DistanceMiles = parsedDistanceMiles,
            EndTime = DateTime.Now
        };

        await _workoutService.AddWorkout(workout);
        SessionSteps = 0;
        ElapsedMinutes = 0;
        DistanceMilesText = string.Empty;
    }

    private async Task RunSessionClockAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ElapsedMinutes = Math.Max(0, (int)_stopwatch.Elapsed.TotalMinutes);
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
