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

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator retrieved {Count} levy accounts",
                correlationId,
                accountIds.Count);

            foreach (var accountId in accountIds)
            {
                var payeSchemes = await context.CallActivityAsync<List<PayeScheme>>(
                    nameof(GetAccountPayeSchemesActivity),
                    new GetAccountPayeSchemesActivityInput
                    {
                        CorrelationId = correlationId,
                        AccountId = accountId
                    }) ?? [];

                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Account {AccountId} returned {Count} PAYE schemes",
                    correlationId,
                    accountId,
                    payeSchemes.Count);

                if (payeSchemes.Count == 0)
                {
                    result.AccountsWithoutPayeSchemesCount++;
                    continue;
                }

                result.TotalPayeSchemesCount += payeSchemes.Count;

                var payeFanOutTasks = payeSchemes
                    .Select(payeScheme => context.CallActivityAsync(
                        nameof(ProcessLevyPayeSchemeActivity),
                        new ProcessLevyPayeSchemeInput
                        {
                            CorrelationId = correlationId,
                            AccountId = accountId,
                            PayeSchemeReference = payeScheme.Reference
                        }))
                    .ToList();

                await Task.WhenAll(payeFanOutTasks);
            }

            result.Success = true;

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator completed PAYE discovery for {AccountCount} accounts and {PayeSchemeCount} PAYE schemes",
                correlationId,
                result.TotalAccountsCount,
                result.TotalPayeSchemesCount);
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
