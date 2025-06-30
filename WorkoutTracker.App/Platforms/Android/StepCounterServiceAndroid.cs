using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Util;
using WorkoutTracker.Services;
using Application = Android.App.Application;

namespace WorkoutTracker.Platforms.Android;

public class StepCounterServiceAndroid : Java.Lang.Object, IStepCounterService, ISensorEventListener
{
    private readonly SensorManager _sensorManager;
    private readonly Sensor _stepSensor;
    private readonly bool _usingStepCounter;
    private int _sessionStepCount;
    private float _baselineStepCount = -1;
    private bool _isTracking;

    public event EventHandler<int> StepsUpdated;

    public StepCounterServiceAndroid()
    {
        var context = Application.Context;
        _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService);

        // Try to get the Step Counter sensor first
        _stepSensor = _sensorManager.GetDefaultSensor(SensorType.StepCounter);
        if (_stepSensor != null)
        {
            _usingStepCounter = true;
        }
        else
        {
            // Fallback to Step Detector if Step Counter is not available
            _stepSensor = _sensorManager.GetDefaultSensor(SensorType.StepDetector);
            _usingStepCounter = false;
        }
        _sessionStepCount = 0;
        Log.Debug("StepCounterService", $"Initialized. Using StepCounter: {_usingStepCounter}");
    }

    public void StartTracking()
    {
        if (_isTracking)
            return;

        _sessionStepCount = 0;
        _baselineStepCount = -1;
        _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Normal);
        _isTracking = true;
        Log.Debug("StepCounterService", "Started tracking.");
    }

    public void StopTracking()
    {
        if (!_isTracking)
            return;

        _sensorManager.UnregisterListener(this, _stepSensor);
        _isTracking = false;
        Log.Debug("StepCounterService", $"Stopped tracking. Final steps: {_sessionStepCount}");
        StepsUpdated?.Invoke(this, _sessionStepCount);
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {
        // Not used
    }

    public void OnSensorChanged(SensorEvent e)
    {
        Log.Debug("StepCounterService", $"Sensor event: {e.Values[0]}");
        if (_usingStepCounter)
        {
            float currentValue = e.Values[0];
            if (_baselineStepCount < 0)
            {
                _baselineStepCount = currentValue;
            }
            int sessionSteps = (int)(currentValue - _baselineStepCount);
            if (sessionSteps != _sessionStepCount)
            {
                _sessionStepCount = sessionSteps;
                Log.Debug("StepCounterService", $"Session steps updated to: {_sessionStepCount}");
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
        else
        {
            if (e.Values[0] == 1.0f)
            {
                _sessionStepCount++;
                Log.Debug("StepCounterService", $"Step detected. Count: {_sessionStepCount}");
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
    }
}
