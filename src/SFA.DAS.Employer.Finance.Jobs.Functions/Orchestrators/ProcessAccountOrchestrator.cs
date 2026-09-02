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

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator started for AccountId {AccountId} PeriodEnd {PeriodEndRef}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef);

        var retryPolicy = new RetryPolicy(5, TimeSpan.FromSeconds(5));

        //Refresh Payment Activities
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator scheduling ImportAccountPaymentsActivity for AccountId {AccountId} PeriodEnd {PeriodEndRef}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef);

        var importPaymentsResult = await context.CallActivityAsync<AccountPaymentsImportResult>(
                                    nameof(AccountPaymentsActivities.ImportAccountPaymentsActivity),
                                    input,
                                    new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received ImportAccountPaymentsActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. Payments: {PaymentCount}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            importPaymentsResult.Status,
            importPaymentsResult.Payments?.Count ?? 0,
            importPaymentsResult.Message);

        if (importPaymentsResult.Payments == null || importPaymentsResult.Payments.Count == 0)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator taking empty-account fast path for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Skipping existing payment ids, staging, metadata, transaction lines and staged-to-operational.",
                correlationId,
                input.AccountId,
                input.PeriodEndRef);

            var emptyAccountTransfersInput = new RefreshAccountTransfersInput
            {
                AccountId = input.AccountId,
                AccountName = input.AccountName,
                PeriodEndRef = input.PeriodEndRef,
                CorrelationId = correlationId,
                TriggeredAt = input.TriggeredAt,
                Payments = []
            };

            var emptyAccountTransfersResult = await context.CallActivityAsync<RefreshAccountTransfersResult>(
                nameof(AccountTransferActivities.RefreshAccountTransfersActivity),
                emptyAccountTransfersInput,
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
                Success = importPaymentsResult.Status == "Succeeded"
                          && emptyAccountTransfersResult.Status == "Succeeded",
                PaymentsProcessed = 0,
                TransfersProcessed = emptyAccountTransfersResult.TransfersProcessed
            };
        }

        var importExistingPaymentIdsResult = await context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                                    nameof(AccountPaymentsActivities.ImportAccountExistingFinancePaymentIdsActivity),
                                    input,
                                    new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received ImportAccountExistingFinancePaymentIdsActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. ExistingPaymentIds: {ExistingPaymentIdCount}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            importExistingPaymentIdsResult.Status,
            importExistingPaymentIdsResult.PaymentIds?.Count ?? 0,
            importExistingPaymentIdsResult.Message);

        var refreshPaymentInput = new RefreshPaymentDataInput
        {
            Payments = importPaymentsResult.Payments ?? new(),
            PaymentIds = importExistingPaymentIdsResult.PaymentIds ?? new(),
            AccountId = input.AccountId,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey
        };

        var refreshPaymentsResult = await context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                                    nameof(RefreshPaymentDataActivities.RefreshPaymentDataActivity),
                                    refreshPaymentInput,
                                    new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received RefreshPaymentDataActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. PaymentsCreated: {PaymentsCreated}. PaymentDetails: {PaymentDetailsCount}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            refreshPaymentsResult.Status,
            refreshPaymentsResult.PaymentsCreated,
            refreshPaymentsResult.PaymentDetails?.Count ?? 0,
            refreshPaymentsResult.Message);

        if (refreshPaymentsResult.Status == "Succeeded")
        {
            var publishRefreshPaymentDataCompletedEventInput = new PublishRefreshPaymentDataCompletedEventInput
            {
                AccountId = input.AccountId,
                PeriodEnd = input.PeriodEndRef,
                PaymentsProcessed = refreshPaymentsResult.PaymentsCreated > 0,
                CorrelationId = correlationId
            };

            try
            {
                var publishRefreshPaymentDataCompletedEventResult = await context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                    nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity),
                    publishRefreshPaymentDataCompletedEventInput,
                    new TaskOptions(retryPolicy));

                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received PublishRefreshPaymentDataCompletedEventActivity result with Status: {Status} Message: {Message}",
                    correlationId,
                    publishRefreshPaymentDataCompletedEventResult.Status,
                    publishRefreshPaymentDataCompletedEventResult.Message);
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
        else
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, RefreshPaymentDataCompletedEvent is not published because RefreshPaymentDataActivity returned Status: {RefreshPaymentDataStatus}.",
                correlationId,
                refreshPaymentsResult.Status);
        }

        var refreshAccountTransfersInput = new RefreshAccountTransfersInput
        {
            AccountId = input.AccountId,
            AccountName = input.AccountName,
            PeriodEndRef = input.PeriodEndRef,
            CorrelationId = correlationId,
            TriggeredAt = input.TriggeredAt,
            Payments = MapTransferPaymentLookups(importPaymentsResult.Payments)
        };

        var refreshAccountTransfersResult = await context.CallActivityAsync<RefreshAccountTransfersResult>(
                                    nameof(AccountTransferActivities.RefreshAccountTransfersActivity),
                                    refreshAccountTransfersInput,
                                    new TaskOptions(retryPolicy));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received RefreshAccountTransfersActivity result for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Status: {Status}. TransfersProcessed: {TransfersProcessed}. Message: {Message}",
            correlationId,
            input.AccountId,
            input.PeriodEndRef,
            refreshAccountTransfersResult.Status,
            refreshAccountTransfersResult.TransfersProcessed,
            refreshAccountTransfersResult.Message);
       
        var paymentMetadataResult = new CreatePaymentMetadataResult
        {
            Status = "Succeeded",
            Message = "No new payment metadata to create."
        };
        var paymentTransactionLinesResult = new CreatePaymentTransactionLinesResult
        {
            Transactions = [],
            Status = "Succeeded",
            Message = "No new transaction lines to create."
        };

        if (refreshPaymentsResult.Status == "Succeeded" && refreshPaymentsResult.PaymentDetails != null && refreshPaymentsResult.PaymentDetails.Count > 0)
        {
            var createPaymentMetadataInput = new CreatePaymentMetadataInput
            {
                AccountId = input.AccountId,
                CorrelationId = correlationId,
                PaymentDetails = refreshPaymentsResult.PaymentDetails.ToList()
            };

            try
            {
                paymentMetadataResult = await context.CallActivityAsync<CreatePaymentMetadataResult>(
                    nameof(PaymentMetadataActivities.CreatePaymentMetadataActivity),
                    createPaymentMetadataInput,
                    new TaskOptions(retryPolicy));

                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received CreatePaymentMetadataActivity result with Status: {Status} Message: {Message}",
                    correlationId,
                    paymentMetadataResult.Status,
                    paymentMetadataResult.Message);
            }
            catch (Exception ex)
            {
                paymentMetadataResult = new CreatePaymentMetadataResult
                {
                    Status = "Failed",
                    Message = ex.Message
                };

                logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, CreatePaymentMetadataActivity failed for AccountId {AccountId} PeriodEnd {PeriodEndRef}. Continuing with transaction line creation.",
                    correlationId,
                    input.AccountId,
                    input.PeriodEndRef);
            }

            //Create Payment Transaction Lines Activities
            var createTransactionLinesActivityInput = new CreatePaymentTransactionLinesInput
            {
                AccountId = input.AccountId,
                PeriodEnd = input.PeriodEndRef,
                CorrelationId = correlationId,
                PaymentDetails = refreshPaymentsResult.PaymentDetails,
                IdempotencyKey = idempotencyKey
            };
            try
            {
                paymentTransactionLinesResult = await context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                                    nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity),
                                    createTransactionLinesActivityInput,
                                    new TaskOptions(retryPolicy));

                logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, received CreatePaymentTransactionLinesActivity result with Status: {Status} Message: {Message}", correlationId, paymentTransactionLinesResult.Status, paymentTransactionLinesResult.Message);
            }
            catch (Exception ex)
            {
                paymentTransactionLinesResult = new CreatePaymentTransactionLinesResult
                {
                    Transactions = [],
                    Status = "Failed",
                    Message = ex.Message
                };

                logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, CreatePaymentTransactionLinesActivity failed for AccountId {AccountId} PeriodEnd {PeriodEndRef}.",
                    correlationId,
                    input.AccountId,
                    input.PeriodEndRef);
            }
        }
        else
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator, CreatePaymentTransactionLinesActivity is not started because staging did not produce payment details for AccountId {AccountId} PeriodEnd {PeriodEndRef}. RefreshPaymentDataStatus: {RefreshPaymentDataStatus}",
                correlationId,
                input.AccountId,
                input.PeriodEndRef,
                refreshPaymentsResult.Status);
        }

        var transferStagedToOperationalInput = new TransferStagedToOperationalInput
        {
            AccountId = input.AccountId,
            PeriodEndRef = input.PeriodEndRef,
            CorrelationId = correlationId
        };

        var transferStagedToOperationalResult = await context.CallActivityAsync<TransferStagedToOperationalResult>(
            nameof(TransferStagedToOperationalActivities.TransferStagedToOperationalActivity),
            transferStagedToOperationalInput,
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
            Success = importPaymentsResult.Status == "Succeeded"
                      && importExistingPaymentIdsResult.Status == "Succeeded"
                      && refreshPaymentsResult.Status == "Succeeded"
                      && refreshAccountTransfersResult.Status == "Succeeded"
                      && paymentMetadataResult.Status == "Succeeded"
                      && paymentTransactionLinesResult.Status == "Succeeded"
                      && transferStagedToOperationalResult.Status != "Failed",
            PaymentsProcessed = refreshPaymentsResult.PaymentsCreated,
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

    private static List<TransferPaymentLookup> MapTransferPaymentLookups(IEnumerable<SFA.DAS.Provider.Events.Api.Types.Payment>? payments)
    {
        if (payments == null)
        {
            return [];
        }

        var lookups = new List<TransferPaymentLookup>();

        foreach (var payment in payments)
        {
            if (!Guid.TryParse(payment.Id, out var paymentId))
            {
                continue;
            }

            lookups.Add(new TransferPaymentLookup
            {
                PaymentId = paymentId,
                EvidenceSubmittedOn = payment.EvidenceSubmittedOn,
                CollectionPeriodMonth = payment.CollectionPeriod?.Month ?? 0,
                CollectionPeriodYear = payment.CollectionPeriod?.Year ?? 0,
                Ukprn = payment.Ukprn
            });
        }

        return lookups;
    }
}
