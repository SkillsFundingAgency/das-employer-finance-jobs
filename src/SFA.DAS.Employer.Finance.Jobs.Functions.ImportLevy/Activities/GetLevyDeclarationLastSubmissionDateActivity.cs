using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyDeclarationLastSubmissionDateActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<GetLevyDeclarationLastSubmissionDateActivity> logger)
{
    [Function("GetLevyDeclarationLastSubmissionDateActivity")]
    public async Task<PayeScheme> Run([ActivityTrigger] GetLevyDeclarationLastSubmissionDateActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving last levy submission date for EmpRef {EmpRef}",
            request.CorrelationId,
            request.EmpRef);

        var response = await retryService.ExecuteAsync(
            () => financeApi.GetWithResponseCode<LastSubmissionDateResult>(
                new GetLevyDeclarationLastSubmissionDateRequest(request.EmpRef)),
            request.CorrelationId,
            "Finance API");

        if (response == null || response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {request.CorrelationId}] Failed to retrieve last levy submission date for EmpRef {request.EmpRef}");
        }

        var lastSubmissionDate = response.Body?.LastSumissionDate;

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved last levy submission date {LastSubmissionDate} for EmpRef {EmpRef}",
            request.CorrelationId,
            lastSubmissionDate,
            request.EmpRef);

        return new PayeScheme
        {
            EmpRef = request.EmpRef,
            LastSubmissionDate = lastSubmissionDate
        };
    }
}
