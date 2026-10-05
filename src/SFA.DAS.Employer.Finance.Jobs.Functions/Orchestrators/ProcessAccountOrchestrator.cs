using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessAccountOrchestrator(ILogger<ProcessAccountOrchestrator> logger)
{
    [Function(nameof(ProcessAccountOrchestrator))]
    public async Task<AccountProcessingResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessAccountInput>();
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var correlationId = input.CorrelationId ?? context.NewGuid().ToString();
        var idempotencyKey = input.IdempotencyKey ?? $"{input.AccountId}_{input.PeriodEndRef}";
        var retryPolicy = new RetryPolicy(5, TimeSpan.FromSeconds(5));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator started for AccountId {AccountId} PeriodEnd {PeriodEndRef}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef);

        var firstPage = await context.CallActivityAsync<StageAccountPaymentsPageResult>(
            nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity),
            CreatePageInput(input, correlationId, idempotencyKey, pageNumber: 1),
            new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator received StageAccountPaymentsPageActivity page {PageNumber}/{TotalPages} for AccountId {AccountId}. ItemCount: {ItemCount}. Status: {Status}. Message: {Message}",
            correlationId,
            firstPage.PageNumber,
            firstPage.TotalPages,
            input.AccountId,
            firstPage.ItemCount,
            firstPage.Status,
            firstPage.Message);

        if (firstPage.ItemCount == 0)
        {
            return await CompleteEmptyAccountFastPath(context, input, correlationId, firstPage, retryPolicy);
        }

        var transferLookups = new List<TransferPaymentLookup>(firstPage.TransferLookups ?? []);
        var totalPaymentsCreated = firstPage.PaymentsCreated;
        var pageStatusesSucceeded = firstPage.Status == "Succeeded";
        var totalPages = Math.Max(firstPage.TotalPages, 1);

        for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
        {
            var pageResult = await context.CallActivityAsync<StageAccountPaymentsPageResult>(
                nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity),
                CreatePageInput(input, correlationId, idempotencyKey, pageNumber),
                new TaskOptions(retryPolicy));

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator received StageAccountPaymentsPageActivity page {PageNumber}/{TotalPages} for AccountId {AccountId}. ItemCount: {ItemCount}. Status: {Status}. Message: {Message}",
                correlationId,
                pageResult.PageNumber,
                pageResult.TotalPages,
                input.AccountId,
                pageResult.ItemCount,
                pageResult.Status,
                pageResult.Message);

            totalPaymentsCreated += pageResult.PaymentsCreated;
            pageStatusesSucceeded = pageStatusesSucceeded && pageResult.Status == "Succeeded";

            if (pageResult.TransferLookups is { Count: > 0 })
            {
                transferLookups.AddRange(pageResult.TransferLookups);
            }

            if (pageResult.TotalPages > totalPages)
            {
                totalPages = pageResult.TotalPages;
            }
        }

        if (pageStatusesSucceeded)
        {
            await PublishRefreshPaymentDataCompletedEvent(
                context,
                input,
                correlationId,
                paymentsProcessed: totalPaymentsCreated > 0,
                retryPolicy);
        }
        else
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, RefreshPaymentDataCompletedEvent is not published because one or more StageAccountPaymentsPageActivity calls failed for AccountId {AccountId} PeriodEnd {PeriodEndRef}.",
                correlationId,
                input.AccountId,
                input.PeriodEndRef);
        }

        var refreshAccountTransfersResult = await context.CallActivityAsync<RefreshAccountTransfersResult>(
            nameof(AccountTransferActivities.RefreshAccountTransfersActivity),
            new RefreshAccountTransfersInput
            {
                AccountId = input.AccountId,
                AccountName = input.AccountName,
                PeriodEndRef = input.PeriodEndRef,
                CorrelationId = correlationId,
                TriggeredAt = input.TriggeredAt,
                Payments = transferLookups
            },
            new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received RefreshAccountTransfersActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. TransfersProcessed: {TransfersProcessed}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            refreshAccountTransfersResult.Status,
            refreshAccountTransfersResult.TransfersProcessed,
            refreshAccountTransfersResult.Message);

        var transferStagedToOperationalResult = await context.CallActivityAsync<TransferStagedToOperationalResult>(
            nameof(TransferStagedToOperationalActivities.TransferStagedToOperationalActivity),
            new TransferStagedToOperationalInput
            {
                AccountId = input.AccountId,
                PeriodEndRef = input.PeriodEndRef,
                CorrelationId = correlationId
            },
            new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received TransferStagedToOperationalActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            transferStagedToOperationalResult.Status,
            transferStagedToOperationalResult.Message);

        var result = new AccountProcessingResult
        {
            AccountId = input.AccountId,
            Success = pageStatusesSucceeded
                      && refreshAccountTransfersResult.Status == "Succeeded"
                      && transferStagedToOperationalResult.Status != "Failed",
            PaymentsProcessed = totalPaymentsCreated,
            TransfersProcessed = refreshAccountTransfersResult.TransfersProcessed
        };

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator completed for AccountId {AccountId} PeriodEnd {PeriodEndRef}. PaymentsProcessed: {PaymentsProcessed}. TransfersProcessed: {TransfersProcessed}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            result.PaymentsProcessed,
            result.TransfersProcessed);

        return result;
    }

    private async Task<AccountProcessingResult> CompleteEmptyAccountFastPath(
        TaskOrchestrationContext context,
        ProcessAccountInput input,
        string correlationId,
        StageAccountPaymentsPageResult firstPage,
        RetryPolicy retryPolicy)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator taking empty-account fast path for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Skipping additional payment pages, metadata, transaction lines and staged-to-operational.",
            correlationId,
            input.AccountId,
            input.PeriodEndRef);

        var emptyAccountTransfersResult = await context.CallActivityAsync<RefreshAccountTransfersResult>(
            nameof(AccountTransferActivities.RefreshAccountTransfersActivity),
            new RefreshAccountTransfersInput
            {
                AccountId = input.AccountId,
                AccountName = input.AccountName,
                PeriodEndRef = input.PeriodEndRef,
                CorrelationId = correlationId,
                TriggeredAt = input.TriggeredAt,
                Payments = []
            },
            new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator empty-account fast path completed for AccountId {AccountId} PeriodEnd {PeriodEndRef}. TransfersProcessed: {TransfersProcessed}. Status: {Status}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            emptyAccountTransfersResult.TransfersProcessed,
            emptyAccountTransfersResult.Status);

        return new AccountProcessingResult
        {
            AccountId = input.AccountId,
            Success = firstPage.Status == "Succeeded"
                      && emptyAccountTransfersResult.Status == "Succeeded",
            PaymentsProcessed = 0,
            TransfersProcessed = emptyAccountTransfersResult.TransfersProcessed
        };
    }

    private async Task PublishRefreshPaymentDataCompletedEvent(
        TaskOrchestrationContext context,
        ProcessAccountInput input,
        string correlationId,
        bool paymentsProcessed,
        RetryPolicy retryPolicy)
    {
        try
        {
            var publishResult = await context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity),
                new PublishRefreshPaymentDataCompletedEventInput
                {
                    AccountId = input.AccountId,
                    PeriodEnd = input.PeriodEndRef,
                    PaymentsProcessed = paymentsProcessed,
                    CorrelationId = correlationId
                },
                new TaskOptions(retryPolicy));

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received PublishRefreshPaymentDataCompletedEventActivity result with Status: {Status} Message: {Message}",
                correlationId,
                publishResult.Status,
                publishResult.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, PublishRefreshPaymentDataCompletedEventActivity failed for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Continuing account payment processing.",
                correlationId,
                input.AccountId,
                input.PeriodEndRef);
        }
    }

    private static StageAccountPaymentsPageInput CreatePageInput(
        ProcessAccountInput input,
        string correlationId,
        string idempotencyKey,
        int pageNumber)
    {
        return new StageAccountPaymentsPageInput
        {
            AccountId = input.AccountId,
            AccountName = input.AccountName,
            PeriodEndRef = input.PeriodEndRef,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            TriggeredAt = input.TriggeredAt,
            PageNumber = pageNumber
        };
    }
}
