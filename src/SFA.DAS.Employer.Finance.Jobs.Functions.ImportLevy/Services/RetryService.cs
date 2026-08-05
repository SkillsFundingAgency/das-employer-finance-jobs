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
        return await ExecuteAsync(action, correlationId, operationName, _ => true, retries);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string correlationId,
        string operationName,
        Func<Exception, bool> shouldRetry,
        int retries = DefaultRetries)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(shouldRetry);

        if (retries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retries), "At least one attempt is required.");
        }

        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (attempt >= retries || !shouldRetry(ex))
                {
                    throw;
                }

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

        throw new InvalidOperationException("Retry execution ended without a result.");
    }
}
