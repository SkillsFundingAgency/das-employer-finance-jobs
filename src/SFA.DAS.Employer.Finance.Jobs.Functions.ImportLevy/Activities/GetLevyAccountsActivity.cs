using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

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
        var requestCorrelationId = Guid.TryParse(correlationId, out var parsedCorrelationId)
            ? parsedCorrelationId
            : Guid.NewGuid();

        while (true)
        {
            var request = new GetAccountsRequest
            {
                Page = pageNumber,
                PageSize = DefaultPageSize,
                CorrelationId = requestCorrelationId
            };

            var pageAccounts = await RetryAsync(
                () => accountService.GetAccountsAsync(request),
                correlationId) ?? [];

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

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {correlationId}] Failed to retrieve levy accounts after {retries} attempts.",
                ex);
        }
    }
}
