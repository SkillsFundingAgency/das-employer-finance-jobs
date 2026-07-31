using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;

public class ImportLevyOrchestrator(ILogger<ImportLevyOrchestrator> logger)
{
    private const int MaxConcurrentHmrcImportActivities = 100;

    [Function("ImportLevyOrchestrator")]
    public async Task<ImportLevyResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var replaySafeLogger = context.CreateReplaySafeLogger(nameof(ImportLevyOrchestrator)) ?? logger;
        var input = context.GetInput<ImportLevyInput>();
        var correlationId = input?.CorrelationId ?? context.NewGuid().ToString();

        replaySafeLogger.LogInformation("[CorrelationId: {CorrelationId}] ImportLevyOrchestrator started", correlationId);

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

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {AccountCount} levy accounts; now retrieving PAYE schemes",
                correlationId,
                accountIds.Count);

            var payeSchemeTasks = new List<Task<List<PayeScheme>>>(accountIds.Count);
            foreach (var accountId in accountIds)
            {
                payeSchemeTasks.Add(context.CallActivityAsync<List<PayeScheme>>(
                    nameof(GetAccountPayeSchemesActivity),
                    new GetAccountPayeSchemesActivityInput
                    {
                        CorrelationId = correlationId,
                        AccountId = accountId
                    }));
            }

            var payeSchemesByAccount = await Task.WhenAll(payeSchemeTasks);
            var allPayeSchemes = new List<PayeScheme>();
            foreach (var payeSchemes in payeSchemesByAccount)
            {
                if (payeSchemes == null || payeSchemes.Count == 0)
                {
                    result.AccountsWithoutPayeSchemesCount++;
                    continue;
                }

                allPayeSchemes.AddRange(payeSchemes);
            }

            result.TotalPayeSchemesCount = allPayeSchemes.Count;

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {PayeSchemeCount} total PAYE schemes across {AccountCount} accounts; now retrieving last submission dates",
                correlationId,
                allPayeSchemes.Count,
                accountIds.Count);

            if (allPayeSchemes.Count == 0)
            {
                result.Success = true;
                return result;
            }

            var submissionDateTasks = new List<Task<PayeScheme>>(allPayeSchemes.Count);
            foreach (var payeScheme in allPayeSchemes)
            {
                submissionDateTasks.Add(context.CallActivityAsync<PayeScheme>(
                    nameof(GetLevyDeclarationLastSubmissionDateActivity),
                    new GetLevyDeclarationLastSubmissionDateActivityRequest(payeScheme.Reference, correlationId)));
            }

            var payeSchemesWithSubmissionDate = (await Task.WhenAll(submissionDateTasks)).ToList();

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved last submission dates for {PayeSchemeCount} PAYE schemes",
                correlationId,
                payeSchemesWithSubmissionDate.Count);

            result.PayeSchemes = payeSchemesWithSubmissionDate;

            var levyImportResults = new List<ImportLevyDeclarationsActivityResult>(payeSchemesWithSubmissionDate.Count);
            foreach (var payeSchemeBatch in payeSchemesWithSubmissionDate.Chunk(MaxConcurrentHmrcImportActivities))
            {
                var importLevyTasks = new List<Task<ImportLevyDeclarationsActivityResult>>(payeSchemeBatch.Length);
                foreach (var payeScheme in payeSchemeBatch)
                {
                    importLevyTasks.Add(context.CallActivityAsync<ImportLevyDeclarationsActivityResult>(
                        nameof(ImportLevyDeclarationsActivity),
                        new ImportLevyActivityRequest(payeScheme.Reference, payeScheme.LastSubmissionDate?.AddDays(-1), correlationId)));
                }

                levyImportResults.AddRange(await Task.WhenAll(importLevyTasks));
            }

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Levy import activities completed. EmployeeReferencesProcessed: {EmployeeReferencesProcessed}, DeclarationsImported: {DeclarationsImported}",
                correlationId,
                levyImportResults.Count,
                levyImportResults.Sum(x => x.DeclarationsCount));

            result.LevyDeclarationsActivityResults = levyImportResults;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;

            replaySafeLogger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator failed: {ErrorMessage}",
                correlationId,
                ex.Message);
        }

        return result;
    }
}
