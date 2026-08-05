using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetAccountPayeSchemesActivity(
    IAccountService accountService,
    IRetryService retryService,
    ILogger<GetAccountPayeSchemesActivity> logger)
{
    [Function("GetAccountPayeSchemesActivity")]
    public async Task<List<PayeScheme>> Run([ActivityTrigger] GetAccountPayeSchemesActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving PAYE schemes for account {AccountId}",
            input.CorrelationId,
            input.AccountId);

        var request = new GetAccountPayeSchemesRequest
        {
            AccountId = input.AccountId,
            CorrelationId = ActivityExecutionHelper.ParseCorrelationIdOrNew(input.CorrelationId)
        };

        var payeSchemes = await retryService.ExecuteAsync(
            () => accountService.GetPayeSchemesAsync(request),
            input.CorrelationId,
            "Finance API") ?? [];

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved {Count} PAYE schemes for account {AccountId}",
            input.CorrelationId,
            payeSchemes.Count,
            input.AccountId);

        return payeSchemes;
    }
}
