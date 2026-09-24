using System.Security.Cryptography;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Orchestrators;

public class ExpireFundsOrchestrator(ILogger<ExpireFundsOrchestrator> logger)
{
    public const string ProcessAccountActivityName = nameof(ExpireFundsActivities.ProcessAccountExpireFundsActivity);
    public const string PublishAccountFundsExpiredEventActivityName =
        nameof(AccountFundsExpiredEventActivities.PublishAccountFundsExpiredEventActivity);

    private static readonly TaskOptions AccountPageRetryOptions = TaskOptions.FromRetryPolicy(
        new RetryPolicy(3, TimeSpan.FromSeconds(5)));

    private static readonly TaskOptions ProcessAccountRetryOptions = TaskOptions.FromRetryPolicy(
        new RetryPolicy(3, TimeSpan.FromSeconds(5)));

    private static readonly TaskOptions PublishEventRetryOptions = TaskOptions.FromRetryPolicy(
        new RetryPolicy(3, TimeSpan.FromSeconds(5)));

    [Function(nameof(ExpireFundsOrchestrator))]
    public async Task<ExpireFundsOrchestrationResult> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var replaySafeLogger = context.CreateReplaySafeLogger(nameof(ExpireFundsOrchestrator)) ?? logger;
        var input = context.GetInput<ExpireFundsOrchestratorInput>();
        var correlationId = string.IsNullOrWhiteSpace(input?.CorrelationId)
            ? context.NewGuid().ToString()
            : input.CorrelationId;
        var accountPageSize = ExpireFundsOptions.GetAccountPageSizeOrDefault(input?.AccountPageSize);
        var maxConcurrentAccounts = ExpireFundsOptions.GetMaxConcurrentAccountsOrDefault(input?.MaxConcurrentAccounts);

        var result = new ExpireFundsOrchestrationResult
        {
            CorrelationId = correlationId
        };

        replaySafeLogger.LogInformation(
            "[CorrelationId: {CorrelationId}] ExpireFundsOrchestrator started. AccountPageSize {AccountPageSize}, MaxConcurrentAccounts {MaxConcurrentAccounts}",
            correlationId,
            accountPageSize,
            maxConcurrentAccounts);

        try
        {
            var page = 1;

            while (true)
            {
                var accounts = await context.CallActivityAsync<List<Accounts>>(
                                   nameof(ExpireFundsActivities.GetAccountsPageActivity),
                                   new GetAccountsRequest
                                   {
                                       Page = page,
                                       PageSize = accountPageSize,
                                       CorrelationId = correlationId
                                   },
                                   AccountPageRetryOptions) ?? [];

                result.PagesProcessed++;

                replaySafeLogger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Retrieved {AccountCount} accounts from page {Page}",
                    correlationId,
                    accounts.Count,
                    page);

                if (accounts.Count == 0)
                {
                    break;
                }

                result.TotalAccountsCount += accounts.Count;
                await ProcessAccountPage(
                    context,
                    accounts,
                    maxConcurrentAccounts,
                    correlationId,
                    result,
                    replaySafeLogger);

                replaySafeLogger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Completed account page {Page}. Processed {ProcessedAccountsCount} of {TotalAccountsCount} accounts discovered so far, Failures {FailedAccountsCount}",
                    correlationId,
                    page,
                    result.ProcessedAccountsCount,
                    result.TotalAccountsCount,
                    result.FailedAccountsCount);

                if (accounts.Count < accountPageSize)
                {
                    break;
                }

                page++;
            }

            result.Success = result.FailedAccountsCount == 0;
        }
        catch (Exception exception)
        {
            result.Success = false;
            result.ErrorMessage = exception.Message;

            replaySafeLogger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] ExpireFundsOrchestrator failed while processing account pages: {ErrorMessage}",
                correlationId,
                exception.Message);
        }

        replaySafeLogger.LogInformation(
            "[CorrelationId: {CorrelationId}] ExpireFundsOrchestrator completed. Success {Success}, PagesProcessed {PagesProcessed}, TotalAccounts {TotalAccounts}, ProcessedAccounts {ProcessedAccounts}, SuccessfulAccounts {SuccessfulAccounts}, FailedAccounts {FailedAccounts}, FundsExpiredAccounts {FundsExpiredAccounts}",
            correlationId,
            result.Success,
            result.PagesProcessed,
            result.TotalAccountsCount,
            result.ProcessedAccountsCount,
            result.SuccessfulAccountsCount,
            result.FailedAccountsCount,
            result.FundsExpiredAccountsCount);

        return result;
    }

    private static async Task ProcessAccountPage(
        TaskOrchestrationContext context,
        IReadOnlyCollection<Accounts> accounts,
        int maxConcurrentAccounts,
        string correlationId,
        ExpireFundsOrchestrationResult result,
        ILogger replaySafeLogger)
    {
        var activeAccountTasks = new List<(long AccountId, Task<ProcessAccountExpireFundsResult> Task)>();

        foreach (var account in accounts)
        {
            while (activeAccountTasks.Count >= maxConcurrentAccounts)
            {
                replaySafeLogger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Expire funds concurrency limit reached ({ActiveCount}/{MaxConcurrentAccounts} active). Waiting before scheduling AccountId {AccountId}",
                    correlationId,
                    activeAccountTasks.Count,
                    maxConcurrentAccounts,
                    account.Id);

                await ObserveOneAccountTask(activeAccountTasks, correlationId, result, replaySafeLogger);
            }

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Scheduling expire funds processing for AccountId {AccountId}",
                correlationId,
                account.Id);

            var accountTask = ProcessAccount(
                context,
                account.Id,
                correlationId);

            activeAccountTasks.Add((account.Id, accountTask));
        }

        while (activeAccountTasks.Count > 0)
        {
            await ObserveOneAccountTask(activeAccountTasks, correlationId, result, replaySafeLogger);
        }
    }

    private static async Task<ProcessAccountExpireFundsResult> ProcessAccount(
        TaskOrchestrationContext context,
        long accountId,
        string correlationId)
    {
        var accountResult = await context.CallActivityAsync<ProcessAccountExpireFundsResult>(
            ProcessAccountActivityName,
            new ProcessAccountExpireFundsInput
            {
                AccountId = accountId,
                CorrelationId = correlationId
            },
            ProcessAccountRetryOptions);

        if (accountResult is not { Success: true, FundsExpired: true })
        {
            return accountResult;
        }

        try
        {
            var publicationResult = await context.CallActivityAsync<PublishAccountFundsExpiredEventResult>(
                PublishAccountFundsExpiredEventActivityName,
                new PublishAccountFundsExpiredEventInput
                {
                    AccountId = accountId,
                    CorrelationId = correlationId,
                    Created = context.CurrentUtcDateTime,
                    MessageId = CreateEventMessageId(correlationId, accountId)
                },
                PublishEventRetryOptions);

            if (publicationResult is { Published: true })
            {
                return accountResult;
            }

            return CreatePublicationFailureResult(
                accountResult,
                publicationResult?.ErrorMessage
                ?? "AccountFundsExpiredEvent publication did not return a successful result.");
        }
        catch (Exception exception)
        {
            return CreatePublicationFailureResult(accountResult, exception.Message);
        }
    }

    private static async Task ObserveOneAccountTask(
        List<(long AccountId, Task<ProcessAccountExpireFundsResult> Task)> activeAccountTasks,
        string correlationId,
        ExpireFundsOrchestrationResult result,
        ILogger replaySafeLogger)
    {
        var completedTask = await Task.WhenAny(activeAccountTasks.Select(item => item.Task));
        var completedAccountTask = activeAccountTasks.First(item => item.Task == completedTask);
        activeAccountTasks.Remove(completedAccountTask);
        result.ProcessedAccountsCount++;

        try
        {
            var accountResult = await completedTask;

            if (accountResult?.FundsExpired == true)
            {
                result.FundsExpiredAccountsCount++;
            }

            if (accountResult is not { Success: true })
            {
                var errorMessage = accountResult?.ErrorMessage
                    ?? "Expire funds activity did not return a successful result.";
                result.FailedAccountsCount++;

                replaySafeLogger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Expire funds processing failed for AccountId {AccountId}: {ErrorMessage}. Continuing with remaining accounts.",
                    correlationId,
                    completedAccountTask.AccountId,
                    errorMessage);
                return;
            }

            result.SuccessfulAccountsCount++;

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Expire funds processing completed for AccountId {AccountId}. FundsExpired {FundsExpired}",
                correlationId,
                completedAccountTask.AccountId,
                accountResult.FundsExpired);
        }
        catch (Exception exception)
        {
            result.FailedAccountsCount++;

            replaySafeLogger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] Expire funds processing failed for AccountId {AccountId}. Continuing with remaining accounts.",
                correlationId,
                completedAccountTask.AccountId);
        }
    }

    private static ProcessAccountExpireFundsResult CreatePublicationFailureResult(
        ProcessAccountExpireFundsResult accountResult,
        string errorMessage) =>
        new()
        {
            AccountId = accountResult.AccountId,
            Success = false,
            FundsExpired = accountResult.FundsExpired,
            ErrorMessage = $"AccountFundsExpiredEvent publication failed: {errorMessage}"
        };

    private static string CreateEventMessageId(string correlationId, long accountId)
    {
        var idempotencyKey = System.Text.Encoding.UTF8.GetBytes($"{correlationId}:{accountId}");
        var hash = Convert.ToHexString(SHA256.HashData(idempotencyKey));

        return $"{nameof(AccountFundsExpiredEvent)}-{accountId}-{hash[..32]}";
    }
}
