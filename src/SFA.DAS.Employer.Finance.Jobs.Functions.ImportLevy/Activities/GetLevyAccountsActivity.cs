using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyAccountsActivity(
    IAccountService accountService,
    ILogger<GetLevyAccountsActivity> logger)
{
    public const int DefaultPageSize = 10000;

    [Function("GetLevyAccountsActivity")]
    public async Task<List<long>> Run([ActivityTrigger] string correlationId)
    {
        var allAccountIds = new List<long>();
        var pageNumber = 1;

        try
        {
            while (true)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Retrieving levy accounts page {PageNumber} with page size {PageSize}",
                    correlationId,
                    pageNumber,
                    DefaultPageSize);

                var pageAccounts = await RetryAsync(
                    () => accountService.GetAccountsAsync(
                        new GetAccountsRequest
                        {
                            Page = pageNumber,
                            PageSize = DefaultPageSize
                        }),
                    correlationId);

                var pageAccountIds = pageAccounts.Select(account => account.Id).ToList();

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
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Failed retrieving levy accounts on page {PageNumber}: {ErrorMessage}",
                correlationId,
                pageNumber,
                ex.Message);

            throw new InvalidOperationException(
                $"[CorrelationId: {correlationId}] Failed retrieving levy accounts on page {pageNumber}: {ex.Message}",
                ex);
        }
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
