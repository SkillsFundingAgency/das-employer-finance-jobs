using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;
using System.Net;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
public class RefreshPaymentDataService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, ILogger<RefreshPaymentDataService> logger) : IRefreshPaymentDataService
{
    public async Task<RefreshPaymentDataResult> PostPaymentsToStaging(List<PaymentStaging> filteredPayments, string correlationId)
    {
        try
        {
            var remainingPayments = filteredPayments;
            var alreadyStagedPaymentIds = new HashSet<Guid>();
            var totalInserted = 0;

            while (remainingPayments.Count > 0)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Calling Finance API to upsert {Count} payments to staging. AccountIds: {AccountIds}. PaymentIds: {PaymentIds}.",
                    correlationId,
                    remainingPayments.Count,
                    string.Join(",", remainingPayments.Select(payment => payment.AccountId).Distinct()),
                    string.Join(",", remainingPayments.Select(payment => payment.PaymentId)));

                var paymentsRequest = new BulkPaymentsRequest
                {
                    Payments = remainingPayments
                };
                var request = new PostPaymentsToStagingRequest(paymentsRequest);
                var response = await financeApiClient.PostWithResponseCode<PostPaymentsToStagingResponse>(request);
                if (response == null)
                {
                    const string message = "No response received from Finance API";
                    logger.LogError(
                        new InvalidOperationException(message),
                        "[CorrelationId: {CorrelationId}] {ErrorMessage}. Assuming no payments upserted to staging.",
                        correlationId,
                        message);
                    return new RefreshPaymentDataResult
                    {
                        PaymentsCreated = totalInserted,
                        Status = "Failed",
                        Message = message
                    };
                }

                if (response.StatusCode == HttpStatusCode.Conflict
                    && TryGetConflictingPaymentIds(response.ErrorContent, out var conflictedPaymentIds))
                {
                    var conflictedRequestedPaymentIds = remainingPayments
                        .Select(payment => payment.PaymentId)
                        .Intersect(conflictedPaymentIds)
                        .ToHashSet();

                    if (conflictedRequestedPaymentIds.Count == 0)
                    {
                        return FailedResult(
                            totalInserted,
                            $"Finance API returned {response.StatusCode} with error: {response.ErrorContent}.",
                            correlationId);
                    }

                    alreadyStagedPaymentIds.UnionWith(conflictedRequestedPaymentIds);

                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] {ConflictCount} payments are already in staging. Retrying {RemainingCount} payments that still need staging.",
                        correlationId,
                        conflictedRequestedPaymentIds.Count,
                        remainingPayments.Count - conflictedRequestedPaymentIds.Count);

                    remainingPayments = remainingPayments
                        .Where(payment => !conflictedRequestedPaymentIds.Contains(payment.PaymentId))
                        .ToList();

                    continue;
                }

                if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
                {
                    return FailedResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} with error: {response.ErrorContent}.",
                        correlationId);
                }

                var paymentsResponse = response.Body;
                if (paymentsResponse == null)
                {
                    return FailedResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} but no response body while staging {remainingPayments.Count} payments.",
                        correlationId);
                }

                if (paymentsResponse.InsertedCount == 0 && remainingPayments.Count > 0)
                {
                    return FailedResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} but inserted 0 of {remainingPayments.Count} requested payments.",
                        correlationId);
                }

                totalInserted += paymentsResponse.InsertedCount;
                logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully upserted {Count} payments to staging.", correlationId, paymentsResponse.InsertedCount);

                break;
            }

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Payment staging completed. Inserted: {InsertedCount}. AlreadyStaged: {AlreadyStagedCount}.",
                correlationId,
                totalInserted,
                alreadyStagedPaymentIds.Count);

            return new RefreshPaymentDataResult
            {
                PaymentsCreated = totalInserted,
                Status = "Succeeded",
                Message = alreadyStagedPaymentIds.Count > 0
                    ? $"Successfully upserted {totalInserted} payments to staging. {alreadyStagedPaymentIds.Count} payments already existed in staging."
                    : $"Successfully upserted {totalInserted} payments to staging."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error upserting payments to staging in Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }

        RefreshPaymentDataResult FailedResult(int paymentsCreated, string message, string failedCorrelationId)
        {
            logger.LogError(
                new InvalidOperationException(message),
                "[CorrelationId: {CorrelationId}] {ErrorMessage} Assuming no payments upserted to staging.",
                failedCorrelationId,
                message);
            return new RefreshPaymentDataResult
            {
                PaymentsCreated = paymentsCreated,
                Status = "Failed",
                Message = message
            };
        }
    }

    private static bool TryGetConflictingPaymentIds(string errorContent, out HashSet<Guid> conflictedPaymentIds)
    {
        conflictedPaymentIds = [];

        if (string.IsNullOrWhiteSpace(errorContent))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(errorContent);

            if (!document.RootElement.TryGetProperty("paymentIds", out var paymentIdsElement)
                || paymentIdsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            conflictedPaymentIds = paymentIdsElement
                .EnumerateArray()
                .Select(element => Guid.TryParse(element.GetString(), out var paymentId) ? paymentId : Guid.Empty)
                .Where(paymentId => paymentId != Guid.Empty)
                .ToHashSet();

            return conflictedPaymentIds.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public List<PaymentStaging> FilterPayments(List<Payment> externalPayments, List<string> existingPaymentIds, long accountId, string correlationId)
    {
        var existingIdsSet = existingPaymentIds.ToHashSet();
        var filteredPayments = externalPayments
                                            .Where(p => !existingIdsSet.Contains(p.Id!) && p.FundingSource != FundingSource.FullyFundedSfa)
                                            .ToList();

        var existingPaymentCount = externalPayments.Count(p => existingIdsSet.Contains(p.Id!));
        var fullyFundedSfaPaymentCount = externalPayments.Count(p => p.FundingSource == FundingSource.FullyFundedSfa);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Filtered {NewCount} payments out of {TotalCount} payments retrieved from Provider Events API for AccountId {AccountId}. Existing: {ExistingCount}. FullyFundedSfa: {FullyFundedSfaCount}.",
            correlationId,
            filteredPayments.Count,
            externalPayments.Count,
            accountId,
            existingPaymentCount,
            fullyFundedSfaPaymentCount);

        if (filteredPayments != null && filteredPayments.Any())
        {
            var payments = filteredPayments.Select(p => new PaymentStaging
            {
                PaymentId = Guid.Parse(p.Id),
                AccountId = accountId,
                Ukprn = p.Ukprn,
                Uln = p.Uln,
                ApprenticeshipId = p.ApprenticeshipId,
                CollectionPeriodId = p.CollectionPeriod.Id,
                CollectionPeriodMonth = p.CollectionPeriod.Month,
                CollectionPeriodYear = p.CollectionPeriod.Year,
                DeliveryPeriodMonth = p.DeliveryPeriod.Month,
                DeliveryPeriodYear = p.DeliveryPeriod.Year,
                FundingSource = p.FundingSource.ToString(),
                TransactionType = p.TransactionType.ToString(),
                Amount = p.Amount,
                EvidenceSubmittedOn = p.EvidenceSubmittedOn,
                EmployerAccountVersion = p.EmployerAccountVersion,
                ApprenticeshipVersion = p.ApprenticeshipVersion
            }).ToList();
            return payments;
        }
        return new List<PaymentStaging>();
    }
}
