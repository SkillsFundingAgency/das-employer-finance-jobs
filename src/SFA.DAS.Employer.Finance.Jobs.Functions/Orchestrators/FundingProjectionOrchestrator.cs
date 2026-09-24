using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

public class FundingProjectionOrchestrator(ILogger<FundingProjectionOrchestrator> logger)
{
    [Function(nameof(FundingProjectionOrchestrator))]
    public async Task Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var replaySafeLogger = context.CreateReplaySafeLogger(nameof(FundingProjectionOrchestrator)) ?? logger;
        replaySafeLogger.LogInformation("FundingProjectionOrchestrator started.");

        var importResponse = await context.CallActivityAsync<ImportCommittedLearnersResponse>(
            nameof(FundingProjectionActivities.ImportCommittedLearnersActivity),
            new object());

        replaySafeLogger.LogInformation(
            "Import committed learners completed. TotalRecords={TotalRecords}, FailedRecords={FailedRecords}, BatchesProcessed={BatchesProcessed}.",
            importResponse.TotalRecords,
            importResponse.FailedRecords,
            importResponse.BatchesProcessed);

        if (importResponse.TotalRecords <= importResponse.FailedRecords)
        {
            replaySafeLogger.LogInformation("Funding projection workflow stopped because no committed learners were imported successfully.");
            return;
        }

        await context.CallActivityAsync<UpdateCommittedLearnersCostResponse>(
            nameof(FundingProjectionActivities.UpdateCommittedLearnersCostActivity),
            new object());
        replaySafeLogger.LogInformation("Updating committed learner costs completed.");

        await context.CallActivityAsync<RecalculateFundingProjectionResponse>(
            nameof(FundingProjectionActivities.RecalculateFundingProjectionActivity),
            new object());
        replaySafeLogger.LogInformation("Recalculating funding projection completed.");
        replaySafeLogger.LogInformation("FundingProjectionOrchestrator completed.");
    }
}