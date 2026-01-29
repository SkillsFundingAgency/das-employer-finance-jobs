using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessPeriodEndOrchestrator(ILogger<ProcessPeriodEndOrchestrator> logger, IPeriodEndService periodEndService, IAccountService accountService)
{
    private const int PageSize = 10000;

    [Function(nameof(ProcessPeriodEndOrchestrator))]
    public async Task<PeriodEndResult> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<PeriodEnd>();

        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        ValidateOrThrow(input, context);

        var periodEnd = await periodEndService.CreatePeriodEndAsync(input, context.NewGuid());

        var allAccounts = new List<Accounts>(); 

        int page = 1;
        while (true)
        {
            var pageInput = new GetAccountsRequest
            {
                Page = page,
                PageSize = PageSize
            };

            var accounts = await accountService.GetAccountsAsync(pageInput);

            if (accounts == null || accounts.Count == 0)
                break;

            allAccounts.AddRange(accounts);

            if (accounts.Count < PageSize)
                break;

            page++;
        }

        return new PeriodEndResult
        {
            PeriodEndId = periodEnd.Id.ToString(),
            TotalAccountsRetrieved = allAccounts.Count,
            Accounts = allAccounts
        };
    }


    private void ValidateOrThrow(PeriodEnd input, TaskOrchestrationContext context)
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
