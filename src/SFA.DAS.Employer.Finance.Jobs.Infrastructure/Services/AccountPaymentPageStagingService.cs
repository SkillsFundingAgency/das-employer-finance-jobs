using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountPaymentPageStagingService(
    IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient,
    IAccountPaymentsImportService accountPaymentsImportService,
    IRefreshPaymentDataService refreshPaymentDataService,
    IPaymentMetadataService paymentMetadataService,
    IPaymentTransactionLinesService paymentTransactionLinesService,
    ILogger<AccountPaymentPageStagingService> logger) : IAccountPaymentPageStagingService
{
    public async Task<StageAccountPaymentsPageResult> StageAccountPaymentsPageAsync(
        StageAccountPaymentsPageInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] StageAccountPaymentsPage started for AccountId {AccountId} PeriodEnd {PeriodEndRef} Page {PageNumber}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            input.PageNumber);

        var paymentsPage = await GetPaymentsPage(input.AccountId, input.PeriodEndRef, input.PageNumber, input.CorrelationId);
        var payments = paymentsPage.Items?.ToList() ?? [];
        var totalPages = paymentsPage.TotalNumberOfPages;

        if (payments.Count == 0)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] StageAccountPaymentsPage page {PageNumber} has no payments for AccountId {AccountId}. Skipping existing-id load, staging, metadata and transaction lines.",
                input.CorrelationId,
                input.PageNumber,
                input.AccountId);

            return new StageAccountPaymentsPageResult
            {
                PageNumber = input.PageNumber,
                TotalPages = totalPages,
                ItemCount = 0,
                PaymentsCreated = 0,
                MetadataCreated = 0,
                TransactionsCreated = 0,
                TransferLookups = [],
                Status = "Succeeded",
                Message = "No payments on page."
            };
        }

        if (payments.Count > StageAccountPaymentsPageResult.MaxTransferLookupsPerPage)
        {
            throw new InvalidOperationException(
                $"Provider Events returned {payments.Count} payments on page {input.PageNumber}, which exceeds the supported page bound of {StageAccountPaymentsPageResult.MaxTransferLookupsPerPage}.");
        }

        var existingPaymentIds = await accountPaymentsImportService.ImportAccountExistingPaymentIdsAsync(
            input.AccountId,
            input.CorrelationId);

        if (existingPaymentIds.Status != "Succeeded")
        {
            return FailedPageResult(
                input.PageNumber,
                totalPages,
                payments.Count,
                existingPaymentIds.Message ?? "Failed to load existing payment ids.");
        }

        var existingIds = existingPaymentIds.PaymentIds ?? [];
        var filteredPayments = refreshPaymentDataService.FilterPayments(
            payments,
            existingIds,
            input.AccountId,
            input.CorrelationId);

        var paymentsCreated = 0;
        var stagingStatus = "Succeeded";
        var stagingMessage = "No new payments to post into staging.";

        if (filteredPayments.Count > 0)
        {
            var stagingResult = await refreshPaymentDataService.PostPaymentsToStaging(filteredPayments, input.CorrelationId);
            paymentsCreated = stagingResult.PaymentsCreated;
            stagingStatus = stagingResult.Status;
            stagingMessage = stagingResult.Message;

            if (stagingStatus != "Succeeded")
            {
                return FailedPageResult(input.PageNumber, totalPages, payments.Count, stagingMessage);
            }
        }

        var filteredPaymentIds = filteredPayments.Select(payment => payment.PaymentId).ToHashSet();
        var newPaymentDetails = payments
            .Where(payment => Guid.TryParse(payment.Id, out var paymentId) && filteredPaymentIds.Contains(paymentId))
            .ToList();

        var metadataCreated = 0;
        var metadataStatus = "Succeeded";
        var metadataMessage = "No new payment metadata to create.";

        if (newPaymentDetails.Count > 0)
        {
            var metadataResult = await paymentMetadataService.CreatePaymentMetadata(
                new CreatePaymentMetadataInput
                {
                    AccountId = input.AccountId,
                    CorrelationId = input.CorrelationId,
                    PaymentDetails = newPaymentDetails
                },
                cancellationToken);

            metadataCreated = metadataResult.MetadataCreated;
            metadataStatus = metadataResult.Status;
            metadataMessage = metadataResult.Message;
        }

        var transactionsCreated = 0;
        var transactionStatus = "Succeeded";
        var transactionMessage = "No new transaction lines to create.";

        if (newPaymentDetails.Count > 0)
        {
            var transactionResult = await paymentTransactionLinesService.CreatePaymentTransactionLines(
                new CreatePaymentTransactionLinesInput
                {
                    AccountId = input.AccountId,
                    PeriodEnd = input.PeriodEndRef,
                    CorrelationId = input.CorrelationId,
                    PaymentDetails = newPaymentDetails,
                    IdempotencyKey = input.IdempotencyKey
                });

            transactionsCreated = transactionResult.TransactionsCreated;
            transactionStatus = transactionResult.Status;
            transactionMessage = transactionResult.Message;
        }

        var transferLookups = MapTransferPaymentLookups(payments);
        var status = stagingStatus == "Succeeded"
                     && metadataStatus == "Succeeded"
                     && transactionStatus == "Succeeded"
            ? "Succeeded"
            : "Failed";

        var message = status == "Succeeded"
            ? $"Page {input.PageNumber}/{Math.Max(totalPages, 1)} staged {paymentsCreated} payments, metadata {metadataCreated}, transactions {transactionsCreated}."
            : $"Staging: {stagingMessage}. Metadata: {metadataMessage}. Transactions: {transactionMessage}";

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] StageAccountPaymentsPage completed for AccountId {AccountId} PeriodEnd {PeriodEndRef} Page {PageNumber}/{TotalPages}. ItemCount: {ItemCount}. PaymentsCreated: {PaymentsCreated}. Status: {Status}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            input.PageNumber,
            totalPages,
            payments.Count,
            paymentsCreated,
            status);

        return new StageAccountPaymentsPageResult
        {
            PageNumber = input.PageNumber,
            TotalPages = totalPages,
            ItemCount = payments.Count,
            PaymentsCreated = paymentsCreated,
            MetadataCreated = metadataCreated,
            TransactionsCreated = transactionsCreated,
            TransferLookups = transferLookups,
            Status = status,
            Message = message
        };
    }

    private async Task<GetPaymentsResponse> GetPaymentsPage(long accountId, string periodEnd, int pageNumber, string correlationId)
    {
        var request = new GetAccountPaymentsRequest(periodEnd, accountId, pageNumber);
        var response = await providerPaymentApiClient.GetWithResponseCode<GetPaymentsResponse>(request);
        var paymentsResponse = response.Body
            ?? throw new InvalidOperationException(
                $"Provider Events API returned {response.StatusCode} without a response body for PeriodEnd:{periodEnd} AccountId:{accountId} page {pageNumber}.");

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved Provider Events payments page {PageNumber} of {TotalPages} for AccountId {AccountId}, PeriodEnd {PeriodEnd}. ItemCount: {ItemCount}",
            correlationId,
            pageNumber,
            paymentsResponse.TotalNumberOfPages,
            accountId,
            periodEnd,
            paymentsResponse.Items?.Length ?? 0);

        return paymentsResponse;
    }

    private static StageAccountPaymentsPageResult FailedPageResult(int pageNumber, int totalPages, int itemCount, string message)
    {
        return new StageAccountPaymentsPageResult
        {
            PageNumber = pageNumber,
            TotalPages = totalPages,
            ItemCount = itemCount,
            PaymentsCreated = 0,
            MetadataCreated = 0,
            TransactionsCreated = 0,
            TransferLookups = [],
            Status = "Failed",
            Message = message
        };
    }

    private static List<TransferPaymentLookup> MapTransferPaymentLookups(IEnumerable<Payment> payments)
    {
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
                Ukprn = payment.Ukprn,
                ApprenticeshipId = payment.ApprenticeshipId,
                StandardCode = payment.StandardCode,
                FrameworkCode = payment.FrameworkCode,
                ProgrammeType = payment.ProgrammeType,
                PathwayCode = payment.PathwayCode,
                CourseCode = payment.CourseCode
            });
        }

        return lookups;
    }
}
