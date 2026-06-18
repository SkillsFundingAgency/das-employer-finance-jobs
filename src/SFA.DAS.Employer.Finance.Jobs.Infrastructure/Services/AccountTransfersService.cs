using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;
using System.Text.Json;
using Payment = SFA.DAS.Provider.Events.Api.Types.Payment;
using ProviderAccountTransfer = SFA.DAS.Provider.Events.Api.Types.AccountTransfer;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountTransfersService(
    IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient,
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ILogger<AccountTransfersService> logger) : IAccountTransfersService
{
    private const string CreatedBy = "EmployerFinanceJobs";

    public async Task<RefreshAccountTransfersResult> RefreshAccountTransfers(RefreshAccountTransfersInput input)
    {
        var transfersResult = await GetAllAccountTransfers(input.AccountId, input.PeriodEndRef, input.CorrelationId);
        if (transfersResult.Status != "Succeeded")
        {
            return transfersResult;
        }

        if (transfersResult.Transfers.Count == 0)
        {
            return new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "No transfers to post into staging."
            };
        }

        var transfers = MapTransfersToStaging(
            transfersResult.Transfers,
            input.Payments ?? [],
            input.AccountName,
            input.PeriodEndRef,
            input.CorrelationId,
            input.TriggeredAt);

        if (transfers.Count == 0)
        {
            return new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "No transfers to post into staging."
            };
        }

        return await PostTransfersToStaging(transfers, input.CorrelationId);
    }

    private async Task<GetAllAccountTransfersResult> GetAllAccountTransfers(long accountId, string periodEnd, string correlationId)
    {
        var transfers = new List<ProviderAccountTransfer>();
        var totalPages = 1;

        for (var page = 1; page <= totalPages; page++)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Calling Provider Events API to retrieve transfers for ReceiverAccountId {AccountId}, PeriodEnd {PeriodEnd}, Page {Page}.",
                correlationId,
                accountId,
                periodEnd,
                page);

            var request = new GetAccountTransfersRequest(periodEnd, accountId, page);
            var response = await providerPaymentApiClient.GetWithResponseCode<GetTransfersResponse>(request);

            if (response == null)
            {
                return FailedGetAllTransfersResult("No response received from Provider Events API", correlationId);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return FailedGetAllTransfersResult(
                    $"Provider Events API returned {response.StatusCode} with error: {response.ErrorContent}",
                    correlationId);
            }

            var transfersResponse = response.Body;
            if (transfersResponse == null)
            {
                return FailedGetAllTransfersResult("Got null response body from Provider Events API.", correlationId);
            }

            totalPages = transfersResponse.TotalNumberOfPages;
            transfers.AddRange(transfersResponse.Items ?? []);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Successfully retrieved transfers page {Page} of {TotalPages} for ReceiverAccountId {AccountId}, PeriodEnd {PeriodEnd}.",
                correlationId,
                page,
                totalPages,
                accountId,
                periodEnd);
        }

        return new GetAllAccountTransfersResult
        {
            Transfers = transfers,
            Status = "Succeeded",
            Message = transfers.Count > 0
                ? $"Successfully retrieved {transfers.Count} transfers"
                : "No transfers returned from Provider Events API"
        };
    }

    private List<TransferStaging> MapTransfersToStaging(
        IReadOnlyCollection<ProviderAccountTransfer> accountTransfers,
        IReadOnlyCollection<Payment> payments,
        string? receiverAccountName,
        string periodEnd,
        string correlationId,
        DateTime triggeredAt)
    {
        var paymentLookup = BuildPaymentLookup(payments);
        var fallbackTransferDate = triggeredAt == default ? DateTime.UtcNow : triggeredAt;

        var duplicateTransferIds = accountTransfers
            .GroupBy(transfer => transfer.TransferId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateTransferIds.Count > 0)
        {
            logger.LogWarning(
                "[CorrelationId: {CorrelationId}] Provider Events returned duplicate transfer ids. Only the first transfer for each id will be staged. TransferIds: {TransferIds}.",
                correlationId,
                string.Join(",", duplicateTransferIds));
        }

        return accountTransfers
            .GroupBy(transfer => transfer.TransferId)
            .Select(group => group.First())
            .Select(transfer =>
            {
                paymentLookup.TryGetValue(transfer.RequiredPaymentId, out var payment);

                var transferDate = fallbackTransferDate;
                if (payment != null && payment.EvidenceSubmittedOn != default)
                {
                    transferDate = payment.EvidenceSubmittedOn;
                }

                return new TransferStaging
                {
                    TransferId = transfer.TransferId,
                    SenderAccountId = transfer.SenderAccountId,
                    ReceiverAccountId = transfer.ReceiverAccountId,
                    ReceiverAccountName = receiverAccountName ?? string.Empty,
                    Amount = transfer.Amount,
                    TransferDate = transferDate,
                    PeriodEnd = periodEnd,
                    CollectionPeriodMonth = payment?.CollectionPeriod?.Month ?? 0,
                    CollectionPeriodYear = payment?.CollectionPeriod?.Year ?? 0,
                    Ukprn = payment?.Ukprn ?? 0,
                    CourseName = string.Empty,
                    CreatedBy = CreatedBy,
                    CorrelationId = correlationId
                };
            })
            .ToList();
    }

    private async Task<RefreshAccountTransfersResult> PostTransfersToStaging(List<TransferStaging> transfers, string correlationId)
    {
        try
        {
            var remainingTransfers = transfers;
            var alreadyStagedTransferIds = new HashSet<long>();
            var totalInserted = 0;

            while (remainingTransfers.Count > 0)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Calling Finance API to stage {Count} transfers. ReceiverAccountIds: {ReceiverAccountIds}. TransferIds: {TransferIds}.",
                    correlationId,
                    remainingTransfers.Count,
                    string.Join(",", remainingTransfers.Select(transfer => transfer.ReceiverAccountId).Distinct()),
                    string.Join(",", remainingTransfers.Select(transfer => transfer.TransferId)));

                var stageTransfersRequest = new StageTransfersRequest
                {
                    Transfers = remainingTransfers
                };
                var request = new PostTransfersToStagingRequest(stageTransfersRequest);
                var response = await financeApiClient.PostWithResponseCode<PostTransfersToStagingResponse>(request);

                if (response == null)
                {
                    return FailedRefreshAccountTransfersResult(
                        totalInserted,
                        "No response received from Finance API",
                        correlationId);
                }

                if (response.StatusCode == HttpStatusCode.Conflict
                    && TryGetConflictingTransferIds(response.ErrorContent, out var conflictingTransferIds))
                {
                    var conflictedRequestedTransferIds = remainingTransfers
                        .Select(transfer => transfer.TransferId)
                        .Intersect(conflictingTransferIds)
                        .ToHashSet();

                    if (conflictedRequestedTransferIds.Count == 0)
                    {
                        return FailedRefreshAccountTransfersResult(
                            totalInserted,
                            $"Finance API returned {response.StatusCode} with error: {response.ErrorContent}.",
                            correlationId);
                    }

                    alreadyStagedTransferIds.UnionWith(conflictedRequestedTransferIds);

                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] {ConflictCount} transfers are already in staging. Retrying {RemainingCount} transfers that still need staging.",
                        correlationId,
                        conflictedRequestedTransferIds.Count,
                        remainingTransfers.Count - conflictedRequestedTransferIds.Count);

                    remainingTransfers = remainingTransfers
                        .Where(transfer => !conflictedRequestedTransferIds.Contains(transfer.TransferId))
                        .ToList();

                    continue;
                }

                if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
                {
                    return FailedRefreshAccountTransfersResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} with error: {response.ErrorContent}.",
                        correlationId);
                }

                var transfersResponse = response.Body;
                if (transfersResponse == null)
                {
                    return FailedRefreshAccountTransfersResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} but no response body while staging {remainingTransfers.Count} transfers.",
                        correlationId);
                }

                if (transfersResponse.InsertedCount == 0 && remainingTransfers.Count > 0)
                {
                    return FailedRefreshAccountTransfersResult(
                        totalInserted,
                        $"Finance API returned {response.StatusCode} but inserted 0 of {remainingTransfers.Count} requested transfers.",
                        correlationId);
                }

                totalInserted += transfersResponse.InsertedCount;
                logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully staged {Count} transfers.", correlationId, transfersResponse.InsertedCount);

                break;
            }

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Transfer staging completed. Inserted: {InsertedCount}. AlreadyStaged: {AlreadyStagedCount}.",
                correlationId,
                totalInserted,
                alreadyStagedTransferIds.Count);

            return new RefreshAccountTransfersResult
            {
                TransfersProcessed = totalInserted,
                Status = "Succeeded",
                Message = alreadyStagedTransferIds.Count > 0
                    ? $"Successfully staged {totalInserted} transfers. {alreadyStagedTransferIds.Count} transfers already existed in staging."
                    : $"Successfully staged {totalInserted} transfers."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error staging transfers in Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    private static Dictionary<Guid, Payment> BuildPaymentLookup(IEnumerable<Payment> payments)
    {
        var paymentLookup = new Dictionary<Guid, Payment>();

        foreach (var payment in payments)
        {
            if (Guid.TryParse(payment.Id, out var paymentId) && !paymentLookup.ContainsKey(paymentId))
            {
                paymentLookup.Add(paymentId, payment);
            }
        }

        return paymentLookup;
    }

    private static bool TryGetConflictingTransferIds(string errorContent, out HashSet<long> conflictingTransferIds)
    {
        conflictingTransferIds = [];

        if (string.IsNullOrWhiteSpace(errorContent))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(errorContent);

            if (!document.RootElement.TryGetProperty("transferIds", out var transferIdsElement)
                || transferIdsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            conflictingTransferIds = transferIdsElement
                .EnumerateArray()
                .Select(GetTransferId)
                .Where(transferId => transferId > 0)
                .ToHashSet();

            return conflictingTransferIds.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static long GetTransferId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var numericTransferId))
        {
            return numericTransferId;
        }

        return element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out var stringTransferId)
            ? stringTransferId
            : 0;
    }

    private RefreshAccountTransfersResult FailedRefreshAccountTransfersResult(int transfersProcessed, string message, string correlationId)
    {
        logger.LogError(
            new InvalidOperationException(message),
            "[CorrelationId: {CorrelationId}] {ErrorMessage} Assuming no transfers staged.",
            correlationId,
            message);

        return new RefreshAccountTransfersResult
        {
            TransfersProcessed = transfersProcessed,
            Status = "Failed",
            Message = message
        };
    }

    private GetAllAccountTransfersResult FailedGetAllTransfersResult(string message, string correlationId)
    {
        logger.LogError(
            new InvalidOperationException(message),
            "[CorrelationId: {CorrelationId}] {ErrorMessage} Assuming no transfers retrieved.",
            correlationId,
            message);

        return new GetAllAccountTransfersResult
        {
            Transfers = [],
            Status = "Failed",
            Message = message
        };
    }

    private class GetAllAccountTransfersResult : RefreshAccountTransfersResult
    {
        public List<ProviderAccountTransfer> Transfers { get; set; } = [];
    }
}
