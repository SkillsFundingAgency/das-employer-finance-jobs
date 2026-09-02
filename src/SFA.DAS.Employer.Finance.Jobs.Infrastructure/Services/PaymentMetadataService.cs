using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;
using LearningType = SFA.DAS.Provider.Events.Api.Types.LearningType;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class PaymentMetadataService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ICommitmentsApiClient commitmentsApiClient,
    IRoatpApiClient roatpApiClient,
    ICoursesApiClient coursesApiClient,
    ILogger<PaymentMetadataService> logger) : IPaymentMetadataService
{
    public const int MaxConcurrentMetadataPerAccount = 8;

    private readonly object _courseLookupLock = new();
    private Task<StandardsResponse?>? _standardsTask;
    private Task<FrameworksResponse?>? _frameworksTask;
    private Dictionary<string, StandardResponse>? _standardsById;
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
        var providerByUkprn = new ConcurrentDictionary<long, Lazy<Task<ProviderDetails?>>>();
        var apprenticeshipById = new ConcurrentDictionary<long, Lazy<Task<ApprenticeshipDetails?>>>();

        await Parallel.ForEachAsync(
            input.PaymentDetails,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrentMetadataPerAccount,
                CancellationToken = cancellationToken
            },
            async (payment, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                if (!Guid.TryParse(payment.Id, out var paymentId))
                {
                    Interlocked.Increment(ref failed);
                    logger.LogWarning(
                        "[CorrelationId: {CorrelationId}] Payment metadata staging skipped because PaymentId {PaymentId} is not a valid Guid.",
                        input.CorrelationId,
                        payment.Id);
                    return;
                }

                try
                {
                    var metadata = await BuildPaymentMetadata(input.AccountId, payment, correlationId, providerByUkprn, apprenticeshipById);
                    var success = await PostPaymentMetadataToStaging(paymentId, metadata, input.CorrelationId);

                    if (success)
                    {
                        Interlocked.Increment(ref metadataCreated);
                    }
                    else
                    {
                        Interlocked.Increment(ref failed);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    logger.LogError(
                        ex,
                        "[CorrelationId: {CorrelationId}] Error creating payment metadata for AccountId {AccountId}, PaymentId {PaymentId}.",
                        input.CorrelationId,
                        input.AccountId,
                        payment.Id);
                }
            });

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
        return BuildPaymentMetadata(
            accountId,
            payment,
            correlationId,
            new ConcurrentDictionary<long, Lazy<Task<ProviderDetails?>>>(),
            new ConcurrentDictionary<long, Lazy<Task<ApprenticeshipDetails?>>>());
    }

    private async Task<PaymentMetadataStaging> BuildPaymentMetadata(
        long accountId,
        Payment payment,
        Guid correlationId,
        ConcurrentDictionary<long, Lazy<Task<ProviderDetails?>>> providerByUkprn,
        ConcurrentDictionary<long, Lazy<Task<ApprenticeshipDetails?>>> apprenticeshipById)
    {
        var providerTask = providerByUkprn.GetOrAdd(
            payment.Ukprn,
            ukprn => new Lazy<Task<ProviderDetails?>>(() => roatpApiClient.GetProvider(ukprn)));
        var provider = await providerTask.Value;

        ApprenticeshipDetails? apprenticeship = null;
        if (payment.ApprenticeshipId.HasValue)
        {
            var apprenticeshipTask = apprenticeshipById.GetOrAdd(
                payment.ApprenticeshipId.Value,
                id => new Lazy<Task<ApprenticeshipDetails?>>(() => commitmentsApiClient.GetApprenticeship(id)));
            apprenticeship = await apprenticeshipTask.Value;
        }

        var metadata = new PaymentMetadataStaging
        {
            PaymentId = Guid.Parse(payment.Id),
            ProviderName = provider?.Name,
            IsHistoricProviderName = provider?.IsHistoricProviderName ?? false,
            StandardCode = payment.StandardCode,
            FrameworkCode = payment.FrameworkCode,
            ProgrammeType = payment.ProgrammeType,
            PathwayCode = payment.PathwayCode,
            CourseCode = payment.CourseCode,
            ApprenticeName = BuildApprenticeName(apprenticeship),
            ApprenticeNINumber = apprenticeship?.NINumber,
            ApprenticeshipCourseStartDate = apprenticeship?.StartDate,
            CohortId = apprenticeship?.CohortId,
            CorrelationId = correlationId
        };

        await AddCourseDetails(metadata, payment);

        return metadata;
    }

    private async Task AddCourseDetails(PaymentMetadataStaging metadata, Payment payment)
    {
        if (payment.StandardCode is > 0)
        {
            var standard = await GetStandard(payment.StandardCode.Value.ToString());
            AddStandardDetails(metadata, standard);
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
            return;
        }

        if (!string.IsNullOrEmpty(payment.CourseCode))
        {
            var standard = await GetStandard(payment.CourseCode);
            AddStandardDetails(metadata, standard);
            return;
        }

        logger.LogWarning(
            "No framework code, standard code or course code set on payment. Cannot get course details. PaymentId: {PaymentId}",
            payment.Id);
    }

    private async Task<Dictionary<string, StandardResponse>> GetStandardsById()
    {
        if (_standardsById != null)
        {
            return _standardsById;
        }

        var standards = await GetStandards();
        lock (_courseLookupLock)
        {
            return _standardsById ??= standards?.Standards
                .Where(standard => !string.IsNullOrWhiteSpace(standard.Id))
                .GroupBy(standard => standard.Id!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, StandardResponse>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<Dictionary<(int FrameworkCode, int ProgType, int PathwayCode), FrameworkResponse>> GetFrameworksByKey()
    {
        if (_frameworksByKey != null)
        {
            return _frameworksByKey;
        }

        var frameworks = await GetFrameworks();
        lock (_courseLookupLock)
        {
            return _frameworksByKey ??= frameworks?.Frameworks
                .GroupBy(framework => (framework.FrameworkCode, framework.ProgType, framework.PathwayCode))
                .ToDictionary(group => group.Key, group => group.First())
                ?? [];
        }
    }

    private Task<StandardsResponse?> GetStandards()
    {
        lock (_courseLookupLock)
        {
            return _standardsTask ??= coursesApiClient.GetStandards();
        }
    }

    private Task<FrameworksResponse?> GetFrameworks()
    {
        lock (_courseLookupLock)
        {
            return _frameworksTask ??= coursesApiClient.GetFrameworks();
        }
    }

    private async Task<StandardResponse?> GetStandard(string standardId)
    {
        var standardsById = await GetStandardsById();
        standardsById.TryGetValue(standardId, out var standard);
        return standard;
    }

    private static void AddStandardDetails(PaymentMetadataStaging metadata, StandardResponse? standard)
    {
        metadata.ApprenticeshipCourseName = standard?.Title;
        metadata.ApprenticeshipCourseLevel = standard?.Level;
        metadata.LearningType = Enum.TryParse(standard?.LearningType, out LearningType learningType)
            ? learningType.ToString()
            : LearningType.Apprenticeship.ToString();
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
