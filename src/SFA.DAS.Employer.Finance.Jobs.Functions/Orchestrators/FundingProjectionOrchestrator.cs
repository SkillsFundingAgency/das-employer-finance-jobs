using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

public class FundingProjectionOrchestrator(ILogger<FundingProjectionOrchestrator> logger)
{
    [Function(nameof(FundingProjectionOrchestrator))]
    public async Task Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var replaySafeLogger = context.CreateReplaySafeLogger(nameof(FundingProjectionOrchestrator)) ?? logger;
        var input = context.GetInput<ImportLearnersCostEvent>()
            ?? throw new InvalidOperationException("Import learners cost event is required.");

        replaySafeLogger.LogInformation("FundingProjectionOrchestrator started.");

        ImportCommittedLearnersResponse importResponse;
        try
        {
            importResponse = await context.CallActivityAsync<ImportCommittedLearnersResponse>(
                nameof(FundingProjectionActivities.ImportCommittedLearnersActivity),
                input);
        }
        catch (Exception ex)
        {
            await PostJobFailureAsync(context, input, ex.Message);
            throw;
        }

        replaySafeLogger.LogInformation(
            "Import committed learners completed. TotalRecords={TotalRecords}, FailedRecords={FailedRecords}, IsSuccessful={IsSuccessful}.",
            importResponse.TotalRecordsProcessed,
            importResponse.FailedRecordsCount,
            importResponse.IsSuccessful);

        await context.CallActivityAsync(
            nameof(FundingProjectionActivities.PostJobActivity),
            new PostJobActivityInput(input.JobId, CreateJobPayload(input, importResponse)));

        replaySafeLogger.LogInformation("FundingProjectionOrchestrator completed.");
    }

    private static PostJobRequestData CreateJobPayload(
        ImportLearnersCostEvent input,
        ImportCommittedLearnersResponse response) =>
        new()
        {
            AccountId = input.AccountId,
            JobName = "ImportLearnersCost",
            JobState = response.IsSuccessful ? "Completed" : "Failed",
            LastSuccessfulImportDate = response.LastSuccessfulImportDate ?? DateTime.MinValue,
            LastAttemptedDate = response.AttemptedDate,
            LastAttemptSuccessful = response.IsSuccessful,
            TotalRecordsLastRun = response.TotalRecordsProcessed,
            FailedRecordsLastRun = response.FailedRecordsCount,
        };

    private static Task PostJobFailureAsync(
        TaskOrchestrationContext context,
        ImportLearnersCostEvent input,
        string failureReason) =>
        context.CallActivityAsync(
            nameof(FundingProjectionActivities.PostJobActivity),
            new PostJobActivityInput(input.JobId, new PostJobRequestData
            {
                AccountId = input.AccountId,
                JobName = "ImportLearnersCost",
                JobState = "Failed",
                LastSuccessfulImportDate = DateTime.MinValue,
                LastAttemptedDate = context.CurrentUtcDateTime,
                LastAttemptSuccessful = false,
                TotalRecordsLastRun = 0,
                FailedRecordsLastRun = 0,
            }));
}