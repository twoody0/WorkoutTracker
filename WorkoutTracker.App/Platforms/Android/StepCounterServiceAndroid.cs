using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Util;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
using WorkoutTracker.PlatformPermissions;
using WorkoutTracker.Services;
using Application = Android.App.Application;

namespace WorkoutTracker.Platforms.Android;

public class StepCounterServiceAndroid : Java.Lang.Object, IStepCounterService, ISensorEventListener
{
    private readonly Context _context;
    private readonly SensorManager _sensorManager;
    private readonly Sensor? _stepSensor;
    private readonly bool _usingStepCounter;
    private float _baselineStepCount = -1;
    private int _sessionStepCount;
    private bool _hasSensorAccess;
    private bool _isTracking;

    public StepCounterServiceAndroid()
    {
        _context = Application.Context;
        _sensorManager = (SensorManager?)_context.GetSystemService(Context.SensorService)
            ?? throw new InvalidOperationException("Sensor manager is unavailable.");

        _stepSensor = _sensorManager.GetDefaultSensor(SensorType.StepCounter);
        if (_stepSensor != null)
        {
            _usingStepCounter = true;
        }
        else
        {
            _stepSensor = _sensorManager.GetDefaultSensor(SensorType.StepDetector);
            _usingStepCounter = false;
        }

        Log.Debug("StepCounterService", $"Initialized. StepCounter={_usingStepCounter}, SensorAvailable={_stepSensor != null}");
    }

    public event EventHandler<int>? StepsUpdated;

    public string SourceDescription => _usingStepCounter
        ? "phone step sensor"
        : "phone motion sensor";

    public bool IsAvailable => _stepSensor != null;

    public async Task<bool> EnsureAccessAsync()
    {
        _hasSensorAccess = await EnsureSensorAccessAsync();
        await EnsureNotificationAccessAsync();
        return _hasSensorAccess;
    }

    public void StartTracking(DateTimeOffset sessionStartedAtUtc)
    {
        if (_isTracking)
        {
            return;
        }

        BeginForegroundTracking(sessionStartedAtUtc);

        try
        {
            var intent = ActiveCardioTrackingService.CreateStartIntent(_context, sessionStartedAtUtc);
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                _context.StartForegroundService(intent);
            }
            else
            {
                _context.StartService(intent);
            }
        }
        catch (Exception ex)
        {
            Log.Error("StepCounterService", $"Unable to start foreground cardio tracking service: {ex}");
            BeginForegroundTracking(sessionStartedAtUtc);
        }
    }

    public void StopTracking()
    {
        EndForegroundTracking();

        try
        {
            var intent = ActiveCardioTrackingService.CreateStopIntent(_context);
            _context.StartService(intent);
        }
        catch (Exception ex)
        {
            Log.Warn("StepCounterService", $"Unable to stop foreground cardio tracking service cleanly: {ex}");
        }
    }

    public Task<int> GetFinalStepCountAsync(DateTimeOffset sessionStartedAtUtc, DateTimeOffset sessionEndedAtUtc)
    {
        return Task.FromResult(Math.Max(0, _sessionStepCount));
    }

    internal void BeginForegroundTracking(DateTimeOffset sessionStartedAtUtc)
    {
        if (_isTracking)
        {
            return;
        }

        _sessionStepCount = 0;
        _baselineStepCount = -1;

        if (!_hasSensorAccess || _stepSensor == null)
        {
            _isTracking = false;
            StepsUpdated?.Invoke(this, 0);
            return;
        }

        try
        {
            _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Normal);
            _isTracking = true;
            Log.Debug("StepCounterService", "Started sensor-based cardio tracking.");
        }
        catch (Exception ex)
        {
            _isTracking = false;
            Log.Error("StepCounterService", $"Unable to start sensor tracking: {ex}");
        }
    }

    internal void EndForegroundTracking()
    {
        if (!_isTracking)
        {
            return;
        }

        if (_stepSensor != null)
        {
            _sensorManager.UnregisterListener(this, _stepSensor);
        }

        _isTracking = false;
        Log.Debug("StepCounterService", $"Stopped tracking. Final steps: {_sessionStepCount}");
        StepsUpdated?.Invoke(this, _sessionStepCount);
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
    {
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (!_isTracking || e?.Values == null || e.Values.Count == 0)
        {
            return;
        }

        if (_usingStepCounter)
        {
            var currentValue = e.Values[0];
            if (_baselineStepCount < 0)
            {
                _baselineStepCount = currentValue;
            }

            PublishCurrentStepCount(Math.Max(0, (int)(currentValue - _baselineStepCount)));
            return;
        }

        if (e.Values[0] == 1.0f)
        {
            PublishCurrentStepCount(_sessionStepCount + 1);
        }
    }

    private void PublishCurrentStepCount(int stepCount)
    {
        var normalized = Math.Max(0, stepCount);
        if (normalized == _sessionStepCount)
        {
            return;
        }

        _sessionStepCount = normalized;
        StepsUpdated?.Invoke(this, _sessionStepCount);
    }

    private static async Task EnsureNotificationAccessAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return;
        }

        var status = await MauiPermissions.CheckStatusAsync<NotificationPermission>();
        if (status == PermissionStatus.Granted)
        {
            return;
        }

        await MauiPermissions.RequestAsync<NotificationPermission>();
    }

    private async Task<bool> EnsureSensorAccessAsync()
    {
        if (_stepSensor == null)
        {
            return false;
        }

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
        return status == PermissionStatus.Granted;
    }
}
