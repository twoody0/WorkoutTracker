using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Util;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Aggregate;
using AndroidX.Health.Connect.Client.Permission;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Kotlin.Coroutines;
using Kotlin.Coroutines.Intrinsics;
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
    private readonly IHealthConnectClient? _healthConnectClient;
    private CancellationTokenSource? _healthConnectPollingCancellation;
    private DateTimeOffset? _sessionStartedAtUtc;
    private int _sessionStepCount;
    private float _baselineStepCount = -1;
    private bool _isTracking;
    private bool _useHealthConnect;

    public StepCounterServiceAndroid()
    {
        _context = Application.Context;
        _sensorManager = (SensorManager?)_context.GetSystemService(Context.SensorService)
            ?? throw new InvalidOperationException("Sensor manager is unavailable.");
        _healthConnectClient = GetHealthConnectClientOrNull(_context);

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

        Log.Debug("StepCounterService", $"Initialized. HealthConnect={_healthConnectClient != null}, StepCounter={_usingStepCounter}");
    }

    public event EventHandler<int>? StepsUpdated;

    public string SourceDescription => _useHealthConnect
        ? "Health Connect"
        : _usingStepCounter
            ? "Phone step sensor"
            : "Phone motion sensor";

    public bool IsAvailable => _healthConnectClient != null || _stepSensor != null;

    public async Task<bool> EnsureAccessAsync()
    {
        if (_healthConnectClient != null)
        {
            var grantedPermissions = await GetGrantedHealthPermissionsAsync();
            if (grantedPermissions.Contains(HealthPermission.ReadSteps))
            {
                _useHealthConnect = true;
                return true;
            }

            if (MainActivity.Current != null)
            {
                var requestedPermissions = await MainActivity.Current.RequestHealthPermissionsAsync([HealthPermission.ReadSteps]);
                if (requestedPermissions.Contains(HealthPermission.ReadSteps))
                {
                    _useHealthConnect = true;
                    return true;
                }
            }

            _useHealthConnect = false;
            return await EnsureSensorAccessAsync();
        }

        _useHealthConnect = false;
        return await EnsureSensorAccessAsync();
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

    public void StartTracking(DateTimeOffset sessionStartedAtUtc)
    {
        if (_isTracking)
        {
            return;
        }

        _sessionStartedAtUtc = sessionStartedAtUtc;
        _sessionStepCount = 0;
        _baselineStepCount = -1;

        if (_useHealthConnect && _healthConnectClient != null)
        {
            _healthConnectPollingCancellation?.Cancel();
            _healthConnectPollingCancellation = new CancellationTokenSource();
            _isTracking = true;
            _ = PollHealthConnectStepsAsync(_healthConnectPollingCancellation.Token);
            return;
        }

        if (_stepSensor == null)
        {
            return;
        }

        try
        {
            _sensorManager.RegisterListener(this, _stepSensor, SensorDelay.Normal);
            _isTracking = true;
            Log.Debug("StepCounterService", "Started sensor-based step tracking.");
        }
        catch (Exception ex)
        {
            Log.Error("StepCounterService", $"Unable to start tracking: {ex}");
        }
    }

    public void StopTracking()
    {
        if (!_isTracking)
        {
            return;
        }

        _healthConnectPollingCancellation?.Cancel();
        _healthConnectPollingCancellation = null;

        if (!_useHealthConnect && _stepSensor != null)
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
        if (e?.Values == null || e.Values.Count == 0)
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

            var sessionSteps = (int)(currentValue - _baselineStepCount);
            if (sessionSteps != _sessionStepCount)
            {
                _sessionStepCount = sessionSteps;
                StepsUpdated?.Invoke(this, _sessionStepCount);
            }
        }
        else if (e.Values[0] == 1.0f)
        {
            _sessionStepCount++;
            StepsUpdated?.Invoke(this, _sessionStepCount);
        }
    }

    private static IHealthConnectClient? GetHealthConnectClientOrNull(Context context)
    {
        var status = HealthConnectClient.GetSdkStatus(context);
        return status == HealthConnectClient.SdkAvailable
            ? HealthConnectClient.GetOrCreate(context)
            : null;
    }

    private async Task<HashSet<string>> GetGrantedHealthPermissionsAsync()
    {
        if (_healthConnectClient?.PermissionController == null)
        {
            return [];
        }

        return await InvokeSuspendAsync(
            continuation => _healthConnectClient.PermissionController.GetGrantedPermissions(continuation),
            value => value switch
            {
                ICollection<string> permissions => new HashSet<string>(permissions, StringComparer.Ordinal),
                System.Collections.IEnumerable enumerable => new HashSet<string>(
                    enumerable.Cast<object>()
                        .Select(item => item?.ToString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item!),
                    StringComparer.Ordinal),
                _ => []
            });
    }

    private async Task PollHealthConnectStepsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var steps = await ReadHealthConnectStepsAsync();
                if (steps != _sessionStepCount)
                {
                    _sessionStepCount = steps;
                    StepsUpdated?.Invoke(this, _sessionStepCount);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error("StepCounterService", $"Health Connect polling failed: {ex}");
        }
    }

    private async Task<int> ReadHealthConnectStepsAsync()
    {
        if (_healthConnectClient == null || !_sessionStartedAtUtc.HasValue)
        {
            return 0;
        }

        var startInstant = Instant.OfEpochMilli(_sessionStartedAtUtc.Value.ToUnixTimeMilliseconds());
        var endInstant = Instant.OfEpochMilli(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var request = new AggregateRequest(
            [StepsRecord.CountTotal],
            TimeRangeFilter.Between(startInstant!, endInstant!),
            Array.Empty<DataOrigin>());

        var aggregationResult = await InvokeSuspendAsync(
            continuation => _healthConnectClient.Aggregate(request, continuation),
            value => (AggregationResult)value);

        var total = aggregationResult.Get(StepsRecord.CountTotal);
        if (total is Java.Lang.Long longValue)
        {
            return (int)longValue.LongValue();
        }

        if (total is Java.Lang.Integer intValue)
        {
            return intValue.IntValue();
        }

        return 0;
    }

    private static async Task<T> InvokeSuspendAsync<T>(
        Func<IContinuation, Java.Lang.Object?> call,
        Func<object, T> mapResult)
    {
        var continuation = new SuspendContinuation<T>(mapResult);
        var result = call(continuation);
        if (!ReferenceEquals(result, IntrinsicsKt.COROUTINE_SUSPENDED))
        {
            return continuation.ResolveImmediate(result);
        }

        return await continuation.Task;
    }

    private sealed class SuspendContinuation<T> : Java.Lang.Object, IContinuation
    {
        private readonly Func<object, T> _mapResult;
        private readonly TaskCompletionSource<T> _completionSource = new();

        public SuspendContinuation(Func<object, T> mapResult)
        {
            _mapResult = mapResult;
        }

        public ICoroutineContext Context => EmptyCoroutineContext.Instance;

        public Task<T> Task => _completionSource.Task;

        public T ResolveImmediate(Java.Lang.Object? result)
        {
            if (TryCreateException(result, out var exception))
            {
                throw exception;
            }

            return _mapResult(result!);
        }

        public void ResumeWith(Java.Lang.Object? result)
        {
            try
            {
                if (TryCreateException(result, out var exception))
                {
                    _completionSource.TrySetException(exception);
                    return;
                }

                _completionSource.TrySetResult(_mapResult(result!));
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
            }
        }

        private static bool TryCreateException(Java.Lang.Object? result, out Exception exception)
        {
            if (result != null)
            {
                try
                {
                    var throwable = result.JavaCast<Java.Lang.Throwable>();
                    exception = new InvalidOperationException(throwable.ToString());
                    return true;
                }
                catch (InvalidCastException)
                {
                }

                var runtimeType = result.GetType();
                if (runtimeType.FullName?.Contains("Failure", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var inner =
                        runtimeType.GetProperty("Exception", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(result)
                        ?? runtimeType.GetField("exception", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(result);

                    if (inner is Java.Lang.Throwable javaThrowable)
                    {
                        exception = new InvalidOperationException(javaThrowable.ToString());
                    }
                    else if (inner is Exception ex)
                    {
                        exception = ex;
                    }
                    else
                    {
                        exception = new InvalidOperationException("Health Connect request failed.");
                    }
                    return true;
                }
            }

            exception = null!;
            return false;
        }
    }
}
