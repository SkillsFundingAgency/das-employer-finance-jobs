using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyAccountsActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
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

            var response = await retryService.ExecuteAsync(
                () => financeApi.GetWithResponseCode<List<long>>(
                    new GetAccountsPageRequest(pageNumber, DefaultPageSize)),
                correlationId,
                "Finance API");

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
}
