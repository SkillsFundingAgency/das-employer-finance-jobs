using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyAccountsActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    ILogger<GetLevyAccountsActivity> logger)
{
    public const int DefaultPageSize = 10000;

    [Function("GetLevyAccountsActivity")]
    public async Task<List<long>> Run([ActivityTrigger] string correlationId)
    {
        var allAccountIds = new List<long>();
        var pageNumber = 1;

        while (true)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieving levy accounts page {PageNumber} with page size {PageSize}",
                correlationId,
                pageNumber,
                DefaultPageSize);

            var response = await RetryAsync(
                () => financeApi.GetWithResponseCode<List<long>>(
                    new GetAccountsPageRequest(pageNumber, DefaultPageSize)),
                correlationId);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"[CorrelationId: {correlationId}] Failed to retrieve levy accounts page {pageNumber}");
            }

            var pageAccountIds = response.Body ?? [];

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {Count} levy accounts from page {PageNumber}",
                correlationId,
                pageAccountIds.Count,
                pageNumber);

            if (pageAccountIds.Count == 0)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] No levy accounts returned for page {PageNumber}. Pagination complete with {TotalCount} total accounts.",
                    correlationId,
                    pageNumber,
                    allAccountIds.Count);
                break;
            }

            allAccountIds.AddRange(pageAccountIds);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {TotalCount} levy accounts so far",
                correlationId,
                allAccountIds.Count);

            pageNumber++;
        }

        return allAccountIds;
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> action, string correlationId, int retries = 3)
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
                    "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error calling Finance API, retrying...",
                    correlationId,
                    attempt);

                await Task.Delay(delay);
                delay *= 2;
            }
        }

        return await action();
    }
}
