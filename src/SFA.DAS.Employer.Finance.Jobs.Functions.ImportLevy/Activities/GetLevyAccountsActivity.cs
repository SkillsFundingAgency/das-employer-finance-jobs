using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyAccountsActivity(
    IAccountService accountService,
    IRetryService retryService,
    ILogger<GetLevyAccountsActivity> logger)
{
    public const int DefaultPageSize = 10000;

    [Function("GetLevyAccountsActivity")]
    public async Task<List<long>> Run([ActivityTrigger] string correlationId)
    {
        var allAccountIds = new List<long>();
        var pageNumber = 1;
        var requestCorrelationId = ActivityExecutionHelper.ParseCorrelationIdOrNew(correlationId).ToString();

        while (true)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieving levy accounts page {PageNumber} with page size {PageSize}",
                correlationId,
                pageNumber,
                DefaultPageSize);

            var request = new GetAccountsRequest
            {
                Page = pageNumber,
                PageSize = DefaultPageSize,
                CorrelationId = requestCorrelationId
            };

            var pageAccounts = await retryService.ExecuteAsync(
                () => accountService.GetAccountsAsync(request),
                correlationId,
                "Finance API") ?? [];

            var pageAccountIds = pageAccounts
                .Select(account => account.Id)
                .ToList();

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
            pageNumber++;
        }

        return allAccountIds;
    }
}
