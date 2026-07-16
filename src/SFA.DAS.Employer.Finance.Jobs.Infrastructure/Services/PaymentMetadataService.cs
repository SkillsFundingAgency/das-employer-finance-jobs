using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class PaymentMetadataService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ICommitmentsApiClient commitmentsApiClient,
    IRoatpApiClient roatpApiClient,
    ICoursesApiClient coursesApiClient,
    ILogger<PaymentMetadataService> logger) : IPaymentMetadataService
{
    private Task<StandardsResponse?>? _standardsTask;
    private Task<FrameworksResponse?>? _frameworksTask;
    private Dictionary<int, StandardResponse>? _standardsById;
    private Dictionary<(int FrameworkCode, int ProgType, int PathwayCode), FrameworkResponse>? _frameworksByKey;

    public async Task<CreatePaymentMetadataResult> CreatePaymentMetadata(CreatePaymentMetadataInput input, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] CreatePaymentMetadata started for AccountId {AccountId}. Payments: {PaymentCount}",
            input.CorrelationId,
            input.AccountId,
            input.PaymentDetails.Count);

        var correlationId = Guid.TryParse(input.CorrelationId, out var parsedCorrelationId)
            ? parsedCorrelationId
            : Guid.NewGuid();

        var metadataCreated = 0;
        var failed = 0;
        var providerByUkprn = new Dictionary<long, ProviderDetails?>();

        foreach (var payment in input.PaymentDetails)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParse(payment.Id, out var paymentId))
            {
                failed++;
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Payment metadata staging skipped because PaymentId {PaymentId} is not a valid Guid.",
                    input.CorrelationId,
                    payment.Id);
                continue;
            }

            try
            {
                var metadata = await BuildPaymentMetadata(input.AccountId, payment, correlationId, providerByUkprn);
                var success = await PostPaymentMetadataToStaging(paymentId, metadata, input.CorrelationId);

                if (success)
                {
                    metadataCreated++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] Error creating payment metadata for AccountId {AccountId}, PaymentId {PaymentId}.",
                    input.CorrelationId,
                    input.AccountId,
                    payment.Id);
            }
        }

        var status = failed == 0 ? "Succeeded" : metadataCreated > 0 ? "PartiallySucceeded" : "Failed";

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] CreatePaymentMetadata completed for AccountId {AccountId}. MetadataCreated: {MetadataCreated}. Failed: {Failed}. Status: {Status}",
            input.CorrelationId,
            input.AccountId,
            metadataCreated,
            failed,
            status);

        return new CreatePaymentMetadataResult
        {
            MetadataCreated = metadataCreated,
            Status = status,
            Message = $"Created {metadataCreated} payment metadata staging rows. Failed {failed}."
        };
    }

    public Task<PaymentMetadataStaging> BuildPaymentMetadata(long accountId, Payment payment, Guid correlationId)
    {
        return BuildPaymentMetadata(accountId, payment, correlationId, new Dictionary<long, ProviderDetails?>());
    }

    private async Task<PaymentMetadataStaging> BuildPaymentMetadata(
        long accountId,
        Payment payment,
        Guid correlationId,
        Dictionary<long, ProviderDetails?> providerByUkprn)
    {
        if (!providerByUkprn.TryGetValue(payment.Ukprn, out var provider))
        {
            provider = await roatpApiClient.GetProvider(payment.Ukprn);
            providerByUkprn[payment.Ukprn] = provider;
        }

        var apprenticeshipTask = payment.ApprenticeshipId.HasValue
            ? commitmentsApiClient.GetApprenticeship(payment.ApprenticeshipId.Value)
            : Task.FromResult<ApprenticeshipDetails?>(null);

        var apprenticeship = await apprenticeshipTask;

        var metadata = new PaymentMetadataStaging
        {
            PaymentId = Guid.Parse(payment.Id),
            ProviderName = provider?.Name,
            IsHistoricProviderName = provider?.IsHistoricProviderName ?? false,
            StandardCode = payment.StandardCode,
            FrameworkCode = payment.FrameworkCode,
            ProgrammeType = payment.ProgrammeType,
            PathwayCode = payment.PathwayCode,
            ApprenticeName = BuildApprenticeName(apprenticeship),
            ApprenticeNINumber = apprenticeship?.NINumber,
            ApprenticeshipCourseStartDate = apprenticeship?.StartDate,
            CorrelationId = correlationId
        };

        await AddCourseDetails(metadata, payment);

        return metadata;
    }

    private async Task AddCourseDetails(PaymentMetadataStaging metadata, Payment payment)
    {
        if (payment.StandardCode is > 0)
        {
            var standardsById = await GetStandardsById();
            standardsById.TryGetValue((int)payment.StandardCode.Value, out var standard);

            metadata.ApprenticeshipCourseName = standard?.Title;
            metadata.ApprenticeshipCourseLevel = standard?.Level;
            return;
        }

        if (payment.FrameworkCode is > 0 && payment.ProgrammeType.HasValue && payment.PathwayCode.HasValue)
        {
            var frameworksByKey = await GetFrameworksByKey();
            frameworksByKey.TryGetValue(
                (payment.FrameworkCode.Value, payment.ProgrammeType.Value, payment.PathwayCode.Value),
                out var framework);

            metadata.ApprenticeshipCourseName = framework?.FrameworkName;
            metadata.ApprenticeshipCourseLevel = framework?.Level;
            metadata.PathwayName = framework?.PathwayName;
        }
    }

    private async Task<Dictionary<int, StandardResponse>> GetStandardsById()
    {
        if (_standardsById != null)
        {
            return _standardsById;
        }

        var standards = await GetStandards();
        _standardsById = standards?.Standards
            .GroupBy(standard => standard.Id)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

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

    private Task<StandardsResponse?> GetStandards()
    {
        return _standardsTask ??= coursesApiClient.GetStandards();
    }

    private Task<FrameworksResponse?> GetFrameworks()
    {
        return _frameworksTask ??= coursesApiClient.GetFrameworks();
    }

    private async Task<bool> PostPaymentMetadataToStaging(Guid paymentId, PaymentMetadataStaging metadata, string correlationId)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Calling Finance API to upsert payment metadata staging for PaymentId {PaymentId}",
            correlationId,
            paymentId);

        var request = new PutPaymentMetadataToStagingRequest(paymentId, metadata);
        var response = await financeApiClient.PutWithResponseCode<PaymentMetadataStagingResponse>(request);

        if (response == null)
        {
            logger.LogWarning(
                "[CorrelationId: {CorrelationId}] No response received from Finance API while upserting payment metadata staging for PaymentId {PaymentId}.",
                correlationId,
                paymentId);
            return false;
        }

        if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
        {
            logger.LogWarning(
                "[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} while upserting payment metadata staging for PaymentId {PaymentId}. Error: {ErrorContent}",
                correlationId,
                response.StatusCode,
                paymentId,
                response.ErrorContent);
            return false;
        }

        return true;
    }

    private static string? BuildApprenticeName(ApprenticeshipDetails? apprenticeship)
    {
        if (apprenticeship == null)
        {
            return null;
        }

        var nameParts = new[] { apprenticeship.FirstName, apprenticeship.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part));

        var apprenticeName = string.Join(" ", nameParts);
        return string.IsNullOrWhiteSpace(apprenticeName) ? null : apprenticeName;
    }
}
