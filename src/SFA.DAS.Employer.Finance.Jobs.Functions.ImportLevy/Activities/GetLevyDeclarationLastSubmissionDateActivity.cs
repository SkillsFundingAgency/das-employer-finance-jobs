using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLevyDeclarationLastSubmissionDateActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    ILogger<GetLevyDeclarationLastSubmissionDateActivity> logger)
{
    [Function("GetLevyDeclarationLastSubmissionDateActivity")]
    public async Task<PayeScheme> Run([ActivityTrigger] GetLevyDeclarationLastSubmissionDateActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving last levy submission date for EmpRef {EmpRef}",
            request.CorrelationId,
            request.EmpRef);

        var response = await RetryAsync(
            () => financeApi.GetWithResponseCode<LastSubmissionDateResult>(
                new GetLevyDeclarationLastSubmissionDateRequest(request.EmpRef)),
            request.CorrelationId);

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

    private async Task<T> RetryAsync<T>(Func<Task<T>> action, string correlationId, int retries = 3)
    {
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < retries)
            {
                logger.LogWarning(
                    ex,
                    "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error calling Finance API, retrying...",
                    correlationId,
                    attempt);

                await Task.Delay(delay);
                delay *= 2;
            }
        }

        return await action();
    }
}
