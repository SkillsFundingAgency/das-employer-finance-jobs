using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;
using System.Text.Json;
using ProviderAccountTransfer = SFA.DAS.Provider.Events.Api.Types.AccountTransfer;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountTransfersService(
    IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient,
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ICoursesApiClient coursesApiClient,
    ILogger<AccountTransfersService> logger) : IAccountTransfersService
{
    private const string CreatedBy = "EmployerFinanceJobs";
    private const string UnknownCourseName = "Unknown Course";
    private const int MaxTransfersPerRequest = 1000;

    private Task<StandardsResponse?>? _standardsTask;
    private Task<FrameworksResponse?>? _frameworksTask;
    private Dictionary<string, StandardResponse>? _standardsById;
    private Dictionary<(int FrameworkCode, int ProgType, int PathwayCode), FrameworkResponse>? _frameworksByKey;

    public async Task<RefreshAccountTransfersResult> RefreshAccountTransfers(RefreshAccountTransfersInput input)
    {
        var providerTransfers = await GetAllAccountTransfers(input.AccountId, input.PeriodEndRef, input.CorrelationId);

        if (providerTransfers.Count == 0)
        {
            return new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "No transfers to post into staging."
            };
        }

        var transfers = await MapTransfersToStaging(
            providerTransfers,
            input.Payments ?? [],
            input.AccountName,
            input.PeriodEndRef,
            input.CorrelationId,
            input.TriggeredAt);

        return await PostTransfersToStaging(transfers, input.CorrelationId);
    }

    private async Task<List<ProviderAccountTransfer>> GetAllAccountTransfers(long accountId, string periodEnd, string correlationId)
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

            var transfersResponse = response.Body
                ?? throw new InvalidOperationException(
                    $"Provider Events API returned {response.StatusCode} without a response body for ReceiverAccountId {accountId}, PeriodEnd {periodEnd}.");

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

        return transfers;
    }

    private async Task<List<TransferStaging>> MapTransfersToStaging(
        IReadOnlyCollection<ProviderAccountTransfer> accountTransfers,
        IReadOnlyCollection<TransferPaymentLookup> payments,
        string? receiverAccountName,
        string periodEnd,
        string correlationId,
        DateTime triggeredAt)
    {
        var paymentLookup = BuildPaymentLookup(payments);
        var paymentsByApprenticeshipId = payments
            .Where(payment => payment.ApprenticeshipId.HasValue && payment.ApprenticeshipId.Value > 0)
            .ToLookup(payment => payment.ApprenticeshipId!.Value);
        var fallbackTransferDate = triggeredAt == default ? DateTime.UtcNow : triggeredAt;
        var senderAccountNames = new Dictionary<long, string>();

        var transfersById = accountTransfers
            .GroupBy(transfer => transfer.TransferId)
            .ToList();

        var duplicateTransferIds = transfersById
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

        // Stage at AccountTransfers grain: one row per Sender/Receiver/Apprenticeship/PeriodEnd with summed Amount.
        var operationalGroups = transfersById
            .Select(group => group.First())
            .GroupBy(transfer => new
            {
                transfer.SenderAccountId,
                transfer.ReceiverAccountId,
                ApprenticeshipId = transfer.CommitmentId,
                PeriodEnd = periodEnd
            })
            .ToList();

        var collapsedGroupCount = operationalGroups.Count(group => group.Count() > 1);
        if (collapsedGroupCount > 0)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Collapsed {CollapsedGroupCount} Provider Events transfer groups to operational AccountTransfers grain before staging for PeriodEnd {PeriodEnd}.",
                correlationId,
                collapsedGroupCount,
                periodEnd);
        }

        var stagedTransfers = new List<TransferStaging>(operationalGroups.Count);
        foreach (var group in operationalGroups)
        {
            var orderedTransfers = group.OrderBy(transfer => transfer.TransferId).ToList();
            var representative = orderedTransfers[0];
            paymentLookup.TryGetValue(representative.RequiredPaymentId, out var requiredPayment);

            var coursePayment = requiredPayment
                ?? paymentsByApprenticeshipId[representative.CommitmentId].FirstOrDefault();

            var transferDate = fallbackTransferDate;
            if (requiredPayment != null && requiredPayment.EvidenceSubmittedOn != default)
            {
                transferDate = requiredPayment.EvidenceSubmittedOn;
            }

            var (courseName, courseLevel) = await ResolveCourseDetails(coursePayment);
            var senderAccountName = await GetAccountName(representative.SenderAccountId, senderAccountNames, correlationId);

            stagedTransfers.Add(new TransferStaging
            {
                TransferId = representative.TransferId,
                SenderAccountId = representative.SenderAccountId,
                SenderAccountName = senderAccountName,
                ReceiverAccountId = representative.ReceiverAccountId,
                ReceiverAccountName = receiverAccountName ?? string.Empty,
                Amount = group.Sum(transfer => transfer.Amount),
                TransferDate = transferDate,
                PeriodEnd = periodEnd,
                CollectionPeriodMonth = requiredPayment?.CollectionPeriodMonth ?? coursePayment?.CollectionPeriodMonth ?? 0,
                CollectionPeriodYear = requiredPayment?.CollectionPeriodYear ?? coursePayment?.CollectionPeriodYear ?? 0,
                Ukprn = requiredPayment?.Ukprn ?? coursePayment?.Ukprn ?? 0,
                CourseName = courseName,
                CourseLevel = courseLevel,
                LearningType = null,
                ApprenticeshipId = representative.CommitmentId,
                Type = representative.Type.ToString(),
                RequiredPaymentId = representative.RequiredPaymentId,
                CreatedBy = CreatedBy,
                CorrelationId = correlationId
            });
        }

        return stagedTransfers;
    }

    private async Task<string> GetAccountName(
        long accountId,
        IDictionary<long, string> cache,
        string correlationId)
    {
        if (cache.TryGetValue(accountId, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            var response = await financeApiClient.GetWithResponseCode<Accounts>(new GetAccountByIdRequest(accountId));
            var accountName = response.Body?.Name ?? string.Empty;
            cache[accountId] = accountName;
            return accountName;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "[CorrelationId: {CorrelationId}] Unable to get account name for AccountId {AccountId} while staging transfers.",
                correlationId,
                accountId);
            cache[accountId] = string.Empty;
            return string.Empty;
        }
    }

    private async Task<(string CourseName, int? CourseLevel)> ResolveCourseDetails(TransferPaymentLookup? payment)
    {
        if (payment == null)
        {
            return (UnknownCourseName, null);
        }

        if (payment.StandardCode is > 0)
        {
            var standard = await GetStandard(payment.StandardCode.Value.ToString());
            return (string.IsNullOrWhiteSpace(standard?.Title) ? UnknownCourseName : standard!.Title!, standard?.Level);
        }

        if (payment.FrameworkCode is > 0 && payment.ProgrammeType.HasValue && payment.PathwayCode.HasValue)
        {
            var frameworksByKey = await GetFrameworksByKey();
            frameworksByKey.TryGetValue(
                (payment.FrameworkCode.Value, payment.ProgrammeType.Value, payment.PathwayCode.Value),
                out var framework);

            return (
                string.IsNullOrWhiteSpace(framework?.FrameworkName) ? UnknownCourseName : framework!.FrameworkName!,
                framework?.Level);
        }

        if (!string.IsNullOrWhiteSpace(payment.CourseCode))
        {
            var standard = await GetStandard(payment.CourseCode);
            return (string.IsNullOrWhiteSpace(standard?.Title) ? UnknownCourseName : standard!.Title!, standard?.Level);
        }

        return (UnknownCourseName, null);
    }

    private async Task<Dictionary<string, StandardResponse>> GetStandardsById()
    {
        if (_standardsById != null)
        {
            return _standardsById;
        }

        var standards = await GetStandards();
        _standardsById = standards?.Standards
            .Where(standard => !string.IsNullOrWhiteSpace(standard.Id))
            .GroupBy(standard => standard.Id!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, StandardResponse>(StringComparer.OrdinalIgnoreCase);

        return _standardsById;
    }

    private async Task<Dictionary<(int FrameworkCode, int ProgType, int PathwayCode), FrameworkResponse>> GetFrameworksByKey()
    {
        if (_frameworksByKey != null)
        {
            return _frameworksByKey;
        }

        var frameworks = await GetFrameworks();
        _frameworksByKey = frameworks?.Frameworks
            .GroupBy(framework => (framework.FrameworkCode, framework.ProgType, framework.PathwayCode))
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        return _frameworksByKey;
    }

    private Task<StandardsResponse?> GetStandards() => _standardsTask ??= coursesApiClient.GetStandards();

    private Task<FrameworksResponse?> GetFrameworks() => _frameworksTask ??= coursesApiClient.GetFrameworks();

    private async Task<StandardResponse?> GetStandard(string standardId)
    {
        var standardsById = await GetStandardsById();
        standardsById.TryGetValue(standardId, out var standard);
        return standard;
    }

    private async Task<RefreshAccountTransfersResult> PostTransfersToStaging(List<TransferStaging> transfers, string correlationId)
    {
        try
        {
            var alreadyStagedTransferIds = new HashSet<long>();
            var totalInserted = 0;

            for (var offset = 0; offset < transfers.Count; offset += MaxTransfersPerRequest)
            {
                var remainingTransfers = transfers
                    .Skip(offset)
                    .Take(MaxTransfersPerRequest)
                    .ToList();

                while (remainingTransfers.Count > 0)
                {
                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] Calling Finance API to stage {Count} transfers. DistinctReceiverAccountIds: {DistinctReceiverAccountIdCount}.",
                        correlationId,
                        remainingTransfers.Count,
                        remainingTransfers.Select(transfer => transfer.ReceiverAccountId).Distinct().Count());

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

    private static Dictionary<Guid, TransferPaymentLookup> BuildPaymentLookup(IEnumerable<TransferPaymentLookup> payments)
    {
        var paymentLookup = new Dictionary<Guid, TransferPaymentLookup>();

        foreach (var payment in payments)
        {
            if (payment.PaymentId != Guid.Empty && !paymentLookup.ContainsKey(payment.PaymentId))
            {
                paymentLookup.Add(payment.PaymentId, payment);
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
}
