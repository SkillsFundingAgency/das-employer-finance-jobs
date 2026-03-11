using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessAccountOrchestrator(ILogger<ProcessAccountOrchestrator> logger)
{
    [Function(nameof(ProcessAccountOrchestrator))]
    public async Task<AccountProcessingResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessAccountInput>();
        var correlationId = input?.CorrelationId ?? context.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator started", correlationId);

        var retryPolicy = new RetryPolicy(
            5,
            TimeSpan.FromSeconds(5));

        var importResult = await context.CallActivityAsync<AccountPaymentsImportResult>(
            nameof(AccountPaymentsActivities.ImportAccountPaymentsActivity),
            input,
            new TaskOptions(retryPolicy));

        var result = new AccountProcessingResult
        {
            AccountId = input?.AccountId ?? 0,
            Success = true,
            PaymentsProcessed = 0,
            TransfersProcessed = 0
        };

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator completed for AccountId {AccountId} PeriodEnd {PeriodEndRef}. ImportId {ImportId}, Status {Status}",
            correlationId,
            input?.AccountId,
            input?.PeriodEndRef,
            importResult.ImportId,
            importResult.Status);

        return result;
    }
}
