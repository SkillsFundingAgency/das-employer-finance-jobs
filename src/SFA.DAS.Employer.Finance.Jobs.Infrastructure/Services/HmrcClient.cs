using HMRC.ESFA.Levy.Api.Client;
using HMRC.ESFA.Levy.Api.Types;
using HMRC.ESFA.Levy.Api.Types.Exceptions;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class HmrcClient(
    IApprenticeshipLevyApiClient apprenticeshipLevyApiClient,
    IHmrcRequestThrottle hmrcRequestThrottle,
    IHmrcTokenProvider hmrcTokenProvider,
    IHmrcClock hmrcClock,
    ILogger<HmrcClient> logger) : IHmrcClient
{
    private const int MaxAttempts = 4;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan TooManyRequestsDelay = TimeSpan.FromSeconds(10);

    public Task<EnglishFractionDeclarations> GetEnglishFractionsAsync(
        string employerReference,
        DateTime? fromDate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            operationName: $"GetEnglishFractions:{employerReference}",
            action: token => apprenticeshipLevyApiClient.GetEmployerFractionCalculations(token, employerReference, fromDate, null),
            notFoundFallback: () => new EnglishFractionDeclarations
            {
                Empref = employerReference,
                FractionCalculations = []
            },
            cancellationToken: cancellationToken);
    }

    public Task<DateTime> GetLastEnglishFractionUpdateAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            operationName: "GetLastEnglishFractionUpdate",
            action: token => apprenticeshipLevyApiClient.GetLastEnglishFractionUpdate(token),
            notFoundFallback: () => DateTime.MinValue,
            cancellationToken: cancellationToken);
    }

    private async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<string, Task<T>> action,
        Func<T> notFoundFallback,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await hmrcRequestThrottle.WaitAsync(operationName, cancellationToken);

            try
            {
                var accessToken = await hmrcTokenProvider.GetAccessTokenAsync(cancellationToken);
                return await action(accessToken);
            }
            catch (ApiHttpException ex) when (ex.HttpCode == 404)
            {
                logger.LogInformation("HMRC returned 404 for {OperationName}. Returning an empty result.", operationName);
                return notFoundFallback();
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < MaxAttempts)
            {
                lastException = ex;
                var delay = GetDelay(ex);

                logger.LogWarning(
                    ex,
                    "Transient HMRC error during {OperationName} on attempt {Attempt}. Retrying in {DelayMs}ms.",
                    operationName,
                    attempt,
                    (int)delay.TotalMilliseconds);

                await hmrcClock.DelayAsync(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        logger.LogError(lastException, "HMRC call failed for {OperationName} after retries.", operationName);
        throw new InvalidOperationException($"HMRC call failed for {operationName}.", lastException);
    }

    private static bool IsRetryable(Exception ex)
    {
        return ex switch
        {
            ApiHttpException apiHttpException when apiHttpException.HttpCode is 429 or 500 or 502 or 503 or 504 => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false
        };
    }

    private static TimeSpan GetDelay(Exception ex)
    {
        return ex is ApiHttpException { HttpCode: 429 }
            ? TooManyRequestsDelay
            : RetryDelay;
    }
}
