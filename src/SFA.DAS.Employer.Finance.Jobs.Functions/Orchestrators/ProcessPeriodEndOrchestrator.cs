using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessPeriodEndOrchestrator(ILogger<ProcessPeriodEndOrchestrator> logger)
{
    private const int PageSize = 10000;

    [Function(nameof(ProcessPeriodEndOrchestrator))]
    public async Task<PeriodEndResult> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessPeriodEndInput>();

        ValidateOrThrow(input, context);

        var periodEnd = await context.CallActivityAsync<PeriodEnd>(nameof(CreatePeriodEndActivity),input);

        var allAccounts = new List<Account>();

        int page = 1;
        while (true)
        {
            var pageInput = new GetAccountsRequest
            {
                Page = page,
                PageSize = PageSize
            };

            var accounts = await context.CallActivityAsync<List<Account>>(nameof(GetAccountsActivity),pageInput);

            if (accounts == null || accounts.Count == 0)
                break;

            allAccounts.AddRange(accounts);

            if (accounts.Count < PageSize)
                break;

            page++;
        }

        return new PeriodEndResult
        {
            PeriodEndId = periodEnd.Id,
            TotalAccountsRetrieved = allAccounts.Count
        };
    }


    private void ValidateOrThrow(ProcessPeriodEndInput input, TaskOrchestrationContext context)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.AccountDataValidAt == default)
            throw new InvalidOperationException("AccountDataValidAt must be provided.");

        if (input.CommitmentDataValidAt == default)
            throw new InvalidOperationException("CommitmentDataValidAt must be provided.");

        var now = context.CurrentUtcDateTime;

        if (input.AccountDataValidAt > now)
            throw new InvalidOperationException("AccountDataValidAt cannot be in the future.");

        if (input.CommitmentDataValidAt > now)
            throw new InvalidOperationException("CommitmentDataValidAt cannot be in the future.");
    }
}
