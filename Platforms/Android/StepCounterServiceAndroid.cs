using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using WorkoutTracker.Services;
using System;
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
        // Fully qualify Application to use the Android version
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
    }

    public void StartTracking()
    {
        if (_isTracking)
            return;

        _sessionStepCount = 0;
        _baselineStepCount = -1;
        _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Normal);
        _isTracking = true;
    }

    public void StopTracking()
    {
        if (!_isTracking)
            return;

        _sensorManager.UnregisterListener(this, _stepSensor);
        _isTracking = false;
        StepsUpdated?.Invoke(this, _sessionStepCount);
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {
        // Not used
    }

    public void OnSensorChanged(SensorEvent e)
    {
        if (_usingStepCounter)
        {
            // For Step Counter, subtract the baseline value
            float currentValue = e.Values[0];
            if (_baselineStepCount < 0)
            {
                _baselineStepCount = currentValue;
            }
            int sessionSteps = (int)(currentValue - _baselineStepCount);
            if (sessionSteps != _sessionStepCount)
            {
                _sessionStepCount = sessionSteps;
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
        else
        {
            // For Step Detector, each event represents one step
            if (e.Values[0] == 1.0f)
            {
                _sessionStepCount++;
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
    }
}
