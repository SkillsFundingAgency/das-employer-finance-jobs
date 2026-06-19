using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;

public class ImportLevyOrchestrator(ILogger<ImportLevyOrchestrator> logger)
{
    [Function("ImportLevyOrchestrator")]
    public async Task<ImportLevyResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ImportLevyInput>();
        var correlationId = input?.CorrelationId ?? context.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportLevyOrchestrator started", correlationId);

        var result = new ImportLevyResult
        {
            CorrelationId = correlationId,
            Success = false
        };

        try
        {
            var accountIds = await context.CallActivityAsync<List<long>>(
                nameof(GetLevyAccountsActivity),
                correlationId) ?? [];

            result.AccountIds = accountIds;
            result.TotalAccountsCount = accountIds.Count;
            result.Success = true;

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator retrieved {Count} levy accounts",
                correlationId,
                accountIds.Count);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;

            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator failed: {ErrorMessage}",
                correlationId,
                ex.Message);
        }

        return result;
    }
}
