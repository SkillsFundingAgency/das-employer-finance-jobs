using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetExistingPeriod12LevyDeclarationsActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<GetExistingPeriod12LevyDeclarationsActivity> logger)
{
    [Function("GetExistingPeriod12LevyDeclarationsActivity")]
    public async Task<List<NormalizedLevyDeclaration>> Run([ActivityTrigger] GetExistingPeriod12LevyDeclarationsActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving existing period-12 levy declarations for EmpRef {EmpRef}",
            request.CorrelationId,
            request.EmpRef);

        var response = await retryService.ExecuteAsync(
            () => financeApi.GetWithResponseCode<List<ExistingPeriod12LevyDeclarationResult>>(
                new GetExistingPeriod12LevyDeclarationsRequest(request.EmpRef)),
            request.CorrelationId,
            "Finance API");

        if (response == null || response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {request.CorrelationId}] Failed to retrieve existing period-12 declarations for EmpRef {request.EmpRef}");
        }

        return (response.Body ?? [])
            .Select(x => new NormalizedLevyDeclaration
            {
                Id = x.Id,
                LevyDueYtd = x.LevyDueYtd,
                SubmissionDate = x.SubmissionDate,
                PayrollYear = x.PayrollYear,
                PayrollMonth = x.PayrollMonth,
                SubmissionId = x.SubmissionId
            })
            .ToList();
    }
}
