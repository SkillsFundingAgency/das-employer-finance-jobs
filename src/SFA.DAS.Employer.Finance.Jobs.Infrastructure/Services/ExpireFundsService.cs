using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class ExpireFundsService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ILogger<ExpireFundsService> logger) : IExpireFundsService
{
    public async Task<ExpireFundsResponse> ExpireFundsAsync(long accountId, string correlationId)
    {
        if (accountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountId), "Account ID must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID must be provided.", nameof(correlationId));
        }

        var request = new ExpireFundsRequest(accountId, new ExpireFundsRequestData
        {
            CorrelationId = correlationId
        });

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Requesting funds expiry for AccountId {AccountId}.",
            correlationId,
            accountId);

        try
        {
            var response = await financeApiClient
                .PostWithResponseCode<ExpireFundsResponse>(request)
                .ConfigureAwait(false);

            if (response == null)
            {
                throw new InvalidOperationException("Employer Finance API returned no response while expiring funds.");
            }

            if (!IsSuccess(response.StatusCode))
            {
                throw new HttpRequestContentException(
                    $"Employer Finance API returned {response.StatusCode} while expiring funds for AccountId {accountId}.",
                    response.StatusCode,
                    response.ErrorContent);
            }

            if (response.Body == null)
            {
                throw new InvalidOperationException(
                    $"Employer Finance API returned {response.StatusCode} without an expire-funds response body.");
            }

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Funds expiry completed for AccountId {AccountId}. FundsExpired {FundsExpired}, LongTermExpiredFundsCount {LongTermExpiredFundsCount}, ShortTermExpiredFundsCount {ShortTermExpiredFundsCount}.",
                correlationId,
                accountId,
                response.Body.FundsExpired,
                response.Body.LongTermExpiredFundsCount,
                response.Body.ShortTermExpiredFundsCount);

            return response.Body;
        }
        catch (Exception exception) when (IsTransient(exception))
        {
            logger.LogWarning(
                exception,
                "[CorrelationId: {CorrelationId}] Transient Employer Finance API failure while expiring funds for AccountId {AccountId}.",
                correlationId,
                accountId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] Employer Finance API failure while expiring funds for AccountId {AccountId}.",
                correlationId,
                accountId);
            throw;
        }
    }

    private static bool IsSuccess(HttpStatusCode statusCode) =>
        (int)statusCode is >= 200 and <= 299;

    private static bool IsTransient(Exception exception) =>
        exception switch
        {
            HttpRequestContentException contentException => IsTransient(contentException.StatusCode),
            HttpRequestException requestException when requestException.StatusCode.HasValue =>
                IsTransient(requestException.StatusCode.Value),
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false
        };

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
