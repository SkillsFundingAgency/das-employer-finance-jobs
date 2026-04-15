using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class HmrcRequestThrottle(
    IHmrcClock hmrcClock,
    ILogger<HmrcRequestThrottle> logger) : IHmrcRequestThrottle
{
    private const int MaxRequestsPerWindow = 6;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(2);

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

                while (_requestTimes.Count > 0 && now - _requestTimes.Peek() >= Window)
                {
                    _requestTimes.Dequeue();
                }

                if (_requestTimes.Count < MaxRequestsPerWindow)
                {
                    _requestTimes.Enqueue(now);
                    return;
                }

                delay = Window - (now - _requestTimes.Peek());

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
