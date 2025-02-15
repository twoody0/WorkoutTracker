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
    private readonly Sensor _stepDetectorSensor;
    private int _sessionStepCount;
    private bool _isTracking;

    public event EventHandler<int> StepsUpdated;

    public StepCounterServiceAndroid()
    {
        var context = Application.Context;
        _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService);
        _stepDetectorSensor = _sensorManager.GetDefaultSensor(SensorType.StepDetector);
        _sessionStepCount = 0;
    }

    public void StartTracking()
    {
        if (_isTracking)
            return;

        _sessionStepCount = 0;
        _sensorManager.RegisterListener(this, _stepDetectorSensor, SensorDelay.Normal);
        _isTracking = true;
    }

    public void StopTracking()
    {
        if (!_isTracking)
            return;

        _sensorManager.UnregisterListener(this, _stepDetectorSensor);
        _isTracking = false;
        // Notify final count
        StepsUpdated?.Invoke(this, _sessionStepCount);
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {
        // Not used
    }

    public void OnSensorChanged(SensorEvent e)
    {
        if (e.Sensor.Type == SensorType.StepDetector)
        {
            // The step detector sensor returns 1 for each step.
            if (e.Values[0] == 1.0f)
            {
                _sessionStepCount++;
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
    }
}
