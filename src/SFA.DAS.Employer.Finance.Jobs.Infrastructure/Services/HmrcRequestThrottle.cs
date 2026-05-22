using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class HmrcRequestThrottle(
    IHmrcClock hmrcClock,
    IOptions<LevyImportResilienceOptions> resilienceOptions,
    ILogger<HmrcRequestThrottle> logger) : IHmrcRequestThrottle
{
    private readonly int _maxRequestsPerWindow = resilienceOptions.Value.MaxRequestsPerWindow;
    private readonly TimeSpan _window = TimeSpan.FromSeconds(resilienceOptions.Value.WindowSeconds);

    private readonly Queue<DateTimeOffset> _requestTimes = new();
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public async Task WaitAsync(string operationName, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            TimeSpan delay;

            await _mutex.WaitAsync(cancellationToken);

            try
            {
                var now = hmrcClock.UtcNow;

                while (_requestTimes.Count > 0 && now - _requestTimes.Peek() >= _window)
                {
                    _requestTimes.Dequeue();
                }

                if (_requestTimes.Count < _maxRequestsPerWindow)
                {
                    _requestTimes.Enqueue(now);
                    return;
                }

                delay = _window - (now - _requestTimes.Peek());

                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }
            }
            finally
            {
                _mutex.Release();
            }

            logger.LogInformation(
                "HMRC throttle reached for {OperationName}. Waiting {DelayMs}ms before sending the next request.",
                operationName,
                (int)delay.TotalMilliseconds);

            await hmrcClock.DelayAsync(delay, cancellationToken);
        }
    }
}

