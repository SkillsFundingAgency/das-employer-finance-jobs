using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

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

        logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessPeriodEndOrchestrator started for PeriodEnd: {PeriodEnd}", input.CorrelationId, input.PeriodEnd);
       
        ValidateOrThrow(input.PeriodEnd, context);

        var periodEnd = await context.CallActivityAsync<PeriodEnd>(
            nameof(CreatePeriodEndActivity),
            new CreatePeriodEndActivityInput { PeriodEnd = input.PeriodEnd, CorrelationId = input.CorrelationId });

        var periodEndRef = string.IsNullOrWhiteSpace(periodEnd.PeriodEndId)
            ? periodEnd.Id.ToString()
            : periodEnd.PeriodEndId;
        var totalCommandsPublished = await FanOutAccountImports(context, periodEndRef, input.MaxConcurrentAccounts, input.CorrelationId, input.TargetAccountId);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessPeriodEndOrchestrator completed for PeriodEndRef {PeriodEndRef}. Total account imports scheduled: {TotalCommandsPublished}",
            input.CorrelationId,
            periodEndRef,
            totalCommandsPublished);

        return new PeriodEndResult
        {
            PeriodEndId = periodEnd.Id.ToString(),
            TotalCommandsPublished = totalCommandsPublished
        };
    }

    [Function(nameof(ProcessPeriodEndAccountsOrchestrator))]
    public async Task<PeriodEndResult> ProcessPeriodEndAccountsOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessPeriodEndOrchestratorInput>();

        if (input?.PeriodEnd == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var periodEndRef = string.IsNullOrWhiteSpace(input.PeriodEnd.PeriodEndId)
            ? input.PeriodEnd.Id.ToString()
            : input.PeriodEnd.PeriodEndId;

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessPeriodEndAccountsOrchestrator started for PeriodEndRef {PeriodEndRef}",
            input.CorrelationId,
            periodEndRef);

        ValidateOrThrow(input.PeriodEnd, context);

        var totalCommandsPublished = await FanOutAccountImports(context, periodEndRef, input.MaxConcurrentAccounts, input.CorrelationId, input.TargetAccountId);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessPeriodEndAccountsOrchestrator completed for PeriodEndRef {PeriodEndRef}. Total account imports scheduled: {TotalCommandsPublished}",
            input.CorrelationId,
            periodEndRef,
            totalCommandsPublished);

        return new PeriodEndResult
        {
            PeriodEndId = input.PeriodEnd.Id.ToString(),
            TotalCommandsPublished = totalCommandsPublished
        };
    }

    [Function(nameof(CreatePeriodEndActivity))]
    public async Task<PeriodEnd> CreatePeriodEndActivity([ActivityTrigger] CreatePeriodEndActivityInput input)
    {
        return await periodEndService.CreatePeriodEndAsync(input.PeriodEnd, input.CorrelationId);
    }

    private async Task<int> FanOutAccountImports(TaskOrchestrationContext context, string periodEndRef, int maxConcurrentAccounts, string CorrelationId, long? targetAccountId)
    {
        var retryPolicy = new RetryPolicy(
            5,
            TimeSpan.FromSeconds(5));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] FanOutAccountImports started for period end {PeriodEndRef}, fetching accounts from Finance API in pages of {PageSize}",
            CorrelationId,
            periodEndRef,
            PageSize);

        var totalPublished = 0;
        var page = 1;
        var maxConcurrency = maxConcurrentAccounts <= 0 ? 50 : maxConcurrentAccounts;
        var targetAccountScheduled = false;

        while (true)
        {
            var pageInput = new GetAccountsRequest
            {
                Page = page,
                PageSize = PageSize,
                CorrelationId = CorrelationId
            };

            var accounts = await context.CallActivityAsync<List<Accounts>>(
                nameof(GetAccountsPageActivity),
                pageInput,
                new TaskOptions(retryPolicy));

            if (accounts == null || accounts.Count == 0)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports: no accounts returned for page {Page}, ending paged fetch",
                    CorrelationId,
                    page);
                break;
            }

            var accountsToProcess = targetAccountId.HasValue
                ? accounts.Where(account => account.Id == targetAccountId.Value).ToList()
                : accounts.ToList();

            if (targetAccountId.HasValue)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports is restricted to AccountId {TargetAccountId}. Page {Page} contains {MatchedCount} matching accounts out of {PageCount}.",
                    CorrelationId,
                    targetAccountId.Value,
                    page,
                    accountsToProcess.Count,
                    accounts.Count);
            }

            var activeAccountTasks = new List<(long AccountId, Task<AccountProcessingResult> Task)>();

            foreach (var account in accountsToProcess)
            {
                while (activeAccountTasks.Count >= maxConcurrency)
                {
                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] FanOutAccountImports reached account concurrency limit ({ActiveCount}/{MaxConcurrency} active) for period end {PeriodEndRef} on page {Page}. Waiting for one account import to complete before scheduling more.",
                        CorrelationId,
                        activeAccountTasks.Count,
                        maxConcurrency,
                        periodEndRef,
                        page);

                    await WaitForOneAccountImportToComplete(activeAccountTasks, CorrelationId, periodEndRef);
                }

                var instanceId = $"ProcessAccount-PeriodEnd-{periodEndRef}-Account-{account.Id}-Correlation-{CorrelationId}";
                var idempotencyKey = DeterministicGuid($"ImportAccountPayments-{periodEndRef}-{account.Id}");
                var accountInput = new ProcessAccountInput
                {
                    AccountId = account.Id,
                    AccountName = account.Name,
                    PeriodEndRef = periodEndRef,
                    CorrelationId = CorrelationId,
                    IdempotencyKey = idempotencyKey.ToString(),
                    TriggeredAt = context.CurrentUtcDateTime
                };

                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports scheduling account import for AccountId {AccountId} PeriodEnd {PeriodEndRef} on page {Page} with InstanceId {InstanceId}",
                    CorrelationId,
                    account.Id,
                    periodEndRef,
                    page,
                    instanceId);

                var accountTask = context.CallSubOrchestratorAsync<AccountProcessingResult>(
                    nameof(ProcessAccountOrchestrator),
                    accountInput,
                    new SubOrchestrationOptions { InstanceId = instanceId });

                activeAccountTasks.Add((account.Id, accountTask));
                totalPublished++;

                if (targetAccountId.HasValue && account.Id == targetAccountId.Value)
                {
                    targetAccountScheduled = true;
                }
            }

            if (activeAccountTasks.Count > 0)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Waiting for the remaining {ActiveCount} active account imports to complete for period end {PeriodEndRef} on page {Page}.",
                    CorrelationId,
                    activeAccountTasks.Count,
                    periodEndRef,
                    page);

                while (activeAccountTasks.Count > 0)
                {
                    await WaitForOneAccountImportToComplete(activeAccountTasks, CorrelationId, periodEndRef);
                }
            }

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] FanOutAccountImports: scheduled {Count} account imports for page {Page} (total so far: {TotalPublished})",
                CorrelationId,
                accountsToProcess.Count,
                page,
                totalPublished);

            if (accounts.Count < PageSize)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports completed for period end {PeriodEndRef}: {TotalPublished} account imports scheduled across {TotalPages} pages",
                    CorrelationId,
                    periodEndRef,
                    totalPublished,
                    page);
                break;
            }

            page++;
        }

        if (targetAccountId.HasValue)
        {
            if (targetAccountScheduled)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports completed paged fetch for restricted AccountId {TargetAccountId} PeriodEnd {PeriodEndRef}. Target account was scheduled.",
                    CorrelationId,
                    targetAccountId.Value,
                    periodEndRef);
            }
            else
            {
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports completed paged fetch for restricted AccountId {TargetAccountId} PeriodEnd {PeriodEndRef}. Target account was not found in any page.",
                    CorrelationId,
                    targetAccountId.Value,
                    periodEndRef);
            }
        }

        return totalPublished;

        async Task WaitForOneAccountImportToComplete(
            List<(long AccountId, Task<AccountProcessingResult> Task)> activeAccountTasks,
            string correlationId,
            string periodEndRef)
        {
            var completedTask = await Task.WhenAny(activeAccountTasks.Select(active => active.Task));
            var completedAccountTask = activeAccountTasks.First(active => active.Task == completedTask);
            activeAccountTasks.Remove(completedAccountTask);

            try
            {
                var accountResult = await completedTask;

                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports completed account import for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Success {Success}. PaymentsProcessed {PaymentsProcessed}. TransfersProcessed {TransfersProcessed}",
                    correlationId,
                    completedAccountTask.AccountId,
                    periodEndRef,
                    accountResult.Success,
                    accountResult.PaymentsProcessed,
                    accountResult.TransfersProcessed);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] FanOutAccountImports failed account import for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Continuing with next account.",
                    correlationId,
                    completedAccountTask.AccountId,
                    periodEndRef);
            }
        }
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
