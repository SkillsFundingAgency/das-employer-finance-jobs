using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public class RetryService(
    ILogger<RetryService> logger,
    IRetryDelay retryDelay) : IRetryService
{
    public const int DefaultRetries = 3;

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string correlationId,
        string operationName,
        int retries = DefaultRetries)
    {
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < retries)
            {
                logger.LogWarning(
                    ex,
                    "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error calling {OperationName}, retrying...",
                    correlationId,
                    attempt,
                    operationName);

                await retryDelay.DelayAsync(delay);
                delay *= 2;
            }
        }

        return await action();
    }
}
