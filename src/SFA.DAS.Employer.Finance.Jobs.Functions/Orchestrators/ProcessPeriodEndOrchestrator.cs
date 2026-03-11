using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessPeriodEndOrchestrator(
    ILogger<ProcessPeriodEndOrchestrator> logger,
    IPeriodEndService periodEndService,
    IAccountService accountService)
{
    private const int PageSize = 10000;

    [Function(nameof(ProcessPeriodEndOrchestrator))]
    public async Task<PeriodEndResult> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessPeriodEndOrchestratorInput>();

        if (input?.PeriodEnd == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        ValidateOrThrow(input.PeriodEnd, context);

        var periodEnd = await context.CallActivityAsync<PeriodEnd>(
            nameof(CreatePeriodEndActivity),
            new CreatePeriodEndActivityInput { PeriodEnd = input.PeriodEnd, CorrelationId = context.NewGuid() });

        var periodEndRef = string.IsNullOrWhiteSpace(periodEnd.PeriodEndId)
            ? periodEnd.Id.ToString()
            : periodEnd.PeriodEndId;
        var totalCommandsPublished = await FanOutAccountImports(context, periodEndRef, input.MaxConcurrentAccounts);

        return new PeriodEndResult
        {
            PeriodEndId = periodEnd.Id.ToString(),
            TotalCommandsPublished = totalCommandsPublished
        };
    }

    [Function(nameof(CreatePeriodEndActivity))]
    public async Task<PeriodEnd> CreatePeriodEndActivity([ActivityTrigger] CreatePeriodEndActivityInput input)
    {
        return await periodEndService.CreatePeriodEndAsync(input.PeriodEnd, input.CorrelationId);
    }

    private async Task<int> FanOutAccountImports(TaskOrchestrationContext context, string periodEndRef, int maxConcurrentAccounts)
    {
        var retryPolicy = new RetryPolicy(
            5,
            TimeSpan.FromSeconds(5));

        logger.LogInformation(
            "FanOutAccountImports started for period end {PeriodEndRef}, fetching accounts from Finance API in pages of {PageSize}",
            periodEndRef,
            PageSize);

        var totalPublished = 0;
        var page = 1;
        var maxConcurrency = maxConcurrentAccounts <= 0 ? 50 : maxConcurrentAccounts;

        while (true)
        {
            var pageInput = new GetAccountsRequest
            {
                Page = page,
                PageSize = PageSize,
                CorrelationId = context.NewGuid()
            };

            var accounts = await context.CallActivityAsync<List<Accounts>>(
                nameof(GetAccountsPageActivity),
                pageInput,
                new TaskOptions(retryPolicy));

            if (accounts == null || accounts.Count == 0)
            {
                logger.LogInformation(
                    "FanOutAccountImports: no accounts returned for page {Page}, ending paged fetch",
                    page);
                break;
            }

            var runningTasks = new List<Task>();
            foreach (var account in accounts)
            {
                var instanceId = $"ProcessAccount-{periodEndRef}-{account.Id}";
                var idempotencyKey = DeterministicGuid($"ImportAccountPayments-{periodEndRef}-{account.Id}");
                var accountInput = new ProcessAccountInput
                {
                    AccountId = account.Id,
                    PeriodEndRef = periodEndRef,
                    CorrelationId = context.NewGuid().ToString(),
                    IdempotencyKey = idempotencyKey.ToString(),
                    TriggeredAt = context.CurrentUtcDateTime
                };

                runningTasks.Add(context.CallSubOrchestratorAsync<AccountProcessingResult>(
                    nameof(ProcessAccountOrchestrator),
                    accountInput,
                    new SubOrchestrationOptions { InstanceId = instanceId }));

                totalPublished++;

                if (runningTasks.Count >= maxConcurrency)
                {
                    await Task.WhenAll(runningTasks);
                    runningTasks.Clear();
                }
            }

            if (runningTasks.Count > 0)
            {
                await Task.WhenAll(runningTasks);
            }

            logger.LogInformation(
                "FanOutAccountImports: scheduled {Count} account imports for page {Page} (total so far: {TotalPublished})",
                accounts.Count,
                page,
                totalPublished);

            if (accounts.Count < PageSize)
            {
                logger.LogInformation(
                    "FanOutAccountImports completed for period end {PeriodEndRef}: {TotalPublished} account imports scheduled across {TotalPages} pages",
                    periodEndRef,
                    totalPublished,
                    page);
                break;
            }

            page++;
        }

        return totalPublished;
    }

    [Function(nameof(GetAccountsPageActivity))]
    public async Task<List<Accounts>> GetAccountsPageActivity([ActivityTrigger] GetAccountsRequest input)
    {
        return await accountService.GetAccountsAsync(input);
    }

    private static Guid DeterministicGuid(string input)
    {
        using var provider = System.Security.Cryptography.MD5.Create();
        var hash = provider.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
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
