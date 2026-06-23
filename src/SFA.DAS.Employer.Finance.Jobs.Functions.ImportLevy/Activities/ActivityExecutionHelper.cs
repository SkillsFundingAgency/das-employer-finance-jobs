using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

internal static class ActivityExecutionHelper
{
    public static Guid ParseCorrelationIdOrNew(string correlationId)
    {
        return Guid.TryParse(correlationId, out var parsedCorrelationId)
            ? parsedCorrelationId
            : Guid.NewGuid();
    }

    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        ILogger logger,
        string correlationId,
        string retryMessage,
        Func<Exception, Exception> finalExceptionFactory,
        int retries = 3)
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
                    retryMessage,
                    correlationId,
                    attempt);

                await Task.Delay(delay);
                delay *= 2;
            }
        }

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            throw finalExceptionFactory(ex);
        }
    }
}
