using Foundation;
using HealthKit;
using WorkoutTracker.Services;

namespace WorkoutTracker.Platforms.iOS;

public sealed class StepCounterServiceiOS : IStepCounterService
{
    private readonly HKHealthStore? _healthStore;
    private CancellationTokenSource? _pollingCancellation;
    private DateTimeOffset? _sessionStartedAtUtc;

    public StepCounterServiceiOS()
    {
        if (HKHealthStore.IsHealthDataAvailable)
        {
            _healthStore = new HKHealthStore();
        }
    }

    public event EventHandler<int>? StepsUpdated;

    public string SourceDescription => "Apple Health (including synced Apple Watch steps)";

    public bool IsAvailable => _healthStore != null;

    public async Task<bool> EnsureAccessAsync()
    {
        if (_healthStore == null)
        {
            return false;
        }

        var stepType = HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount);
        if (stepType == null)
        {
            return false;
        }

        var readTypes = new NSSet(stepType);
        var completionSource = new TaskCompletionSource<bool>();

        _healthStore.RequestAuthorizationToShare(
            typesToShare: null,
            typesToRead: readTypes,
            completion: (success, error) =>
            {
                completionSource.TrySetResult(success && error == null);
            });

        return await completionSource.Task.ConfigureAwait(false);
    }

    public void StartTracking(DateTimeOffset sessionStartedAtUtc)
    {
        if (_healthStore == null)
        {
            StepsUpdated?.Invoke(this, 0);
            return;
        }

        _sessionStartedAtUtc = sessionStartedAtUtc;
        _pollingCancellation?.Cancel();
        _pollingCancellation = new CancellationTokenSource();
        _ = PollStepsAsync(_pollingCancellation.Token);
    }

    public void StopTracking()
    {
        _pollingCancellation?.Cancel();
        _pollingCancellation = null;
    }

    public async Task<int> GetFinalStepCountAsync(DateTimeOffset sessionStartedAtUtc, DateTimeOffset sessionEndedAtUtc)
    {
        if (_healthStore == null)
        {
            return 0;
        }

        return await QueryStepCountAsync(sessionStartedAtUtc, sessionEndedAtUtc).ConfigureAwait(false);
    }

    private async Task PollStepsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var stepCount = await QueryStepCountAsync().ConfigureAwait(false);
                StepsUpdated?.Invoke(this, stepCount);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task<int> QueryStepCountAsync()
    {
        if (_healthStore == null || !_sessionStartedAtUtc.HasValue)
        {
            return Task.FromResult(0);
        }

        return QueryStepCountAsync(_sessionStartedAtUtc.Value, DateTimeOffset.UtcNow);
    }

    private Task<int> QueryStepCountAsync(DateTimeOffset startTimeUtc, DateTimeOffset endTimeUtc)
    {
        if (_healthStore == null)
        {
            return Task.FromResult(0);
        }

        var completionSource = new TaskCompletionSource<int>();
        var stepType = HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount);
        if (stepType == null)
        {
            completionSource.TrySetResult(0);
            return completionSource.Task;
        }

        var startDate = ToNSDate(startTimeUtc);
        var endDate = ToNSDate(endTimeUtc);
        var predicate = HKQuery.GetPredicateForSamples(startDate, endDate, HKQueryOptions.StrictStartDate);

        var query = new HKStatisticsQuery(
            quantityType: stepType,
            quantitySamplePredicate: predicate,
            options: HKStatisticsOptions.CumulativeSum,
            handler: (_, result, error) =>
            {
                var sumQuantity = result?.SumQuantity();
                if (error != null || sumQuantity == null)
                {
                    completionSource.TrySetResult(0);
                    return;
                }

                var totalSteps = sumQuantity.GetDoubleValue(HKUnit.Count);
                completionSource.TrySetResult((int)Math.Max(0, Math.Round(totalSteps)));
            });

        _healthStore.ExecuteQuery(query);
        return completionSource.Task;
    }

    private static NSDate ToNSDate(DateTimeOffset value)
    {
        return NSDate.FromTimeIntervalSince1970(value.ToUnixTimeSeconds());
    }
}
