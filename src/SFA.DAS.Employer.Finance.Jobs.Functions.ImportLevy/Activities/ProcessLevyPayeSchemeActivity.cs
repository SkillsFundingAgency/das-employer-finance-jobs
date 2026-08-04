using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class ProcessLevyPayeSchemeActivity(ILogger<ProcessLevyPayeSchemeActivity> logger)
{
    [Function("ProcessLevyPayeSchemeActivity")]
    public Task Run([ActivityTrigger] ProcessLevyPayeSchemeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Queued downstream PAYE work item for account {AccountId} and PAYE scheme {PayeSchemeReference}",
            input.CorrelationId,
            input.AccountId,
            input.PayeSchemeReference);

        return Task.CompletedTask;
    }
}
