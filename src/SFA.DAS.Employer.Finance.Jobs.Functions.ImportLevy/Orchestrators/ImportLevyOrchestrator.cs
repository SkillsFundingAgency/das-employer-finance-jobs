using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using PersistLevyDeclarationsActivityResult = SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models.PersistLevyDeclarationsActivityResult;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;

public class ImportLevyOrchestrator(
    ILogger<ImportLevyOrchestrator> logger,
    IOptions<ImportLevyProcessingOptions> processingOptions)
{
    private readonly int _maxConcurrentHmrcActivities = Math.Max(1, processingOptions.Value.MaxConcurrentHmrcActivities);
    private const int MaxActivityAttempts = 3;
    private const string GovernmentGatewaySource = "government-gateway";
    private static readonly TaskOptions ActivityRetryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(MaxActivityAttempts, TimeSpan.FromSeconds(5)));

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
                                        correlationId,
                                        ActivityRetryOptions) ?? [];
            result.AccountIds = accountIds;
            result.TotalAccountsCount = accountIds.Count;
            result.RunSummary.AccountsProcessed = accountIds.Count;

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {AccountCount} levy accounts; now retrieving PAYE schemes",
                correlationId,
                accountIds.Count);

            var payeSchemeTasks = new List<Task<AccountPayeSchemes>>(accountIds.Count);
            foreach (var accountId in accountIds)
            {
                payeSchemeTasks.Add(GetPayeSchemesForAccount(accountId));
            }

            var payeSchemesByAccount = await Task.WhenAll(payeSchemeTasks);
            var payeWorkItems = payeSchemesByAccount
                .SelectMany(x => x.PayeSchemes.Select(paye => new PayeWorkItem(x.AccountId, paye.EmpRef)))
                .ToList();
            result.RunSummary.PayeDiscovered = payeWorkItems.Count;

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved {PayeSchemeCount} total PAYE schemes across {AccountCount} accounts; now retriving last submision dates",
                correlationId,
                payeWorkItems.Count,
                accountIds.Count);

            var submissionDateTasks = new List<Task<PayeWorkItem>>(payeWorkItems.Count);
            foreach (var workItem in payeWorkItems)
            {
                submissionDateTasks.Add(GetPayeWorkItemWithSubmissionDate(workItem));
            }

            var payeSchemesWithSubmissionDate = (await Task.WhenAll(submissionDateTasks))
                .Where(x => !string.IsNullOrWhiteSpace(x.EmpRef))
                .ToList();
            result.PayeSchemes = payeSchemesWithSubmissionDate
                .Select(x => new PayeScheme { EmpRef = x.EmpRef, LastSubmissionDate = x.LastSubmissionDate })
                .ToList();

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] Retrieved last submission dates for {PayeSchemeCount} PAYE schemes",
                correlationId,
                payeSchemesWithSubmissionDate.Count);

            var levyImportResults = new List<ImportLevyDeclarationsActivityResult>(payeSchemesWithSubmissionDate.Count);
            var fractionsFetchResults = new List<EnglishFractionsFetchResult>(payeSchemesWithSubmissionDate.Count);
            var fractionsPersistenceResults = new List<EnglishFractionsPersistenceResult>(payeSchemesWithSubmissionDate.Count);
            var calculationDatePersistenceResults = new List<EnglishFractionCalculationDatePersistenceResult>(payeSchemesWithSubmissionDate.Count);
            var levyPersistenceResults = new List<PersistLevyDeclarationsActivityResult>(payeSchemesWithSubmissionDate.Count);

            foreach (var payeSchemeBatch in payeSchemesWithSubmissionDate.Chunk(_maxConcurrentHmrcActivities))
            {
                var payePipelineTasks = new List<Task<PayePipelineResult>>(payeSchemeBatch.Length);
                foreach (var payeWorkItem in payeSchemeBatch)
                {
                    payePipelineTasks.Add(ProcessPayeExecutionFlowPipeline(payeWorkItem));
                }

                var pipelineResults = await Task.WhenAll(payePipelineTasks);

                foreach (var pipelineResult in pipelineResults)
                {
                    if (pipelineResult.LevyImportResult is not null)
                    {
                        levyImportResults.Add(pipelineResult.LevyImportResult);
                    }

                    if (pipelineResult.EnglishFractionsFetchResult is not null)
                    {
                        fractionsFetchResults.Add(pipelineResult.EnglishFractionsFetchResult);
                    }

                    if (pipelineResult.EnglishFractionsPersistenceResult is not null)
                    {
                        fractionsPersistenceResults.Add(pipelineResult.EnglishFractionsPersistenceResult);
                    }

                    if (pipelineResult.EnglishFractionCalculationDatePersistenceResult is not null)
                    {
                        calculationDatePersistenceResults.Add(pipelineResult.EnglishFractionCalculationDatePersistenceResult);
                    }

                    if (pipelineResult.PersistLevyDeclarationsActivityResult is not null)
                    {
                        levyPersistenceResults.Add(pipelineResult.PersistLevyDeclarationsActivityResult);
                    }

                    if (pipelineResult.FailedItem is not null)
                    {
                        result.FailedItems.Add(pipelineResult.FailedItem);
                    }
                }
            }

            result.LevyDeclarationsActivityResults = levyImportResults;
            result.EnglishFractionsFetchResults = fractionsFetchResults;
            result.EnglishFractionsPersistenceResults = fractionsPersistenceResults;
            result.EnglishFractionCalculationDatePersistenceResults = calculationDatePersistenceResults;

            PopulateRunSummary(result, levyPersistenceResults);
            result.Success = result.FailedItems.Count == 0;

            replaySafeLogger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportLevy completed. AccountsProcessed={AccountsProcessed}, PayeDiscovered={PayeDiscovered}, PayeProcessed={PayeProcessed}, LevyDeclarationsFetched={LevyDeclarationsFetched}, LevyDeclarationsNormalized={LevyDeclarationsNormalized}, LevyDeclarationsPersisted={LevyDeclarationsPersisted}, TransactionsCreated={TransactionsCreated}, EnglishFractionsStored={EnglishFractionsStored}, EnglishFractionsSkipped={EnglishFractionsSkipped}, Failures={Failures}, Retries={Retries}",
                correlationId,
                result.RunSummary.AccountsProcessed,
                result.RunSummary.PayeDiscovered,
                result.RunSummary.PayeProcessed,
                result.RunSummary.LevyDeclarationsFetched,
                result.RunSummary.LevyDeclarationsNormalized,
                result.RunSummary.LevyDeclarationsPersisted,
                result.RunSummary.TransactionsCreated,
                result.RunSummary.EnglishFractionsStored,
                result.RunSummary.EnglishFractionsSkipped,
                result.RunSummary.TotalFailures,
                result.RunSummary.TotalRetries);
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

        async Task<AccountPayeSchemes> GetPayeSchemesForAccount(long accountId)
        {
            var stageResult = await CallStage(
                activityName: nameof(GetPayeSchemesByAccountActivity),
                retryCounterUpdater: summary => summary.GetPayeSchemesRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<List<PayeScheme>>(
                    nameof(GetPayeSchemesByAccountActivity),
                    new GetPayeSchemesByAccountActivityRequest(accountId, correlationId, GovernmentGatewaySource),
                    ActivityRetryOptions),
                accountId: accountId);

            if (!stageResult.Success)
            {
                if (stageResult.FailedItem is not null)
                {
                    result.FailedItems.Add(stageResult.FailedItem);
                }
                return new AccountPayeSchemes(accountId, []);
            }

            return new AccountPayeSchemes(accountId, stageResult.Value ?? []);
        }

        async Task<PayeWorkItem> GetPayeWorkItemWithSubmissionDate(PayeWorkItem workItem)
        {
            var stageResult = await CallStage(
                activityName: nameof(GetLevyDeclarationLastSubmissionDateActivity),
                retryCounterUpdater: summary => summary.GetLastSubmissionDateRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<PayeScheme>(
                    nameof(GetLevyDeclarationLastSubmissionDateActivity),
                    new GetLevyDeclarationLastSubmissionDateActivityRequest(workItem.EmpRef, correlationId),
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef);

            if (!stageResult.Success || stageResult.Value == null)
            {
                if (stageResult.FailedItem is not null)
                {
                    result.FailedItems.Add(stageResult.FailedItem);
                }
                return workItem;
            }

            return workItem with { LastSubmissionDate = stageResult.Value.LastSubmissionDate };
        }

        async Task<PayePipelineResult> ProcessPayeExecutionFlowPipeline(PayeWorkItem workItem)
        {
            var levyResult = await CallStage(
                activityName: nameof(ImportLevyDeclarationsActivity),
                retryCounterUpdater: summary => summary.ImportLevyDeclarationsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<ImportLevyDeclarationsActivityResult>(
                    nameof(ImportLevyDeclarationsActivity),
                    new ImportLevyActivityRequest(workItem.EmpRef, workItem.LastSubmissionDate, correlationId),
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!levyResult.Success || levyResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = levyResult.FailedItem };
            }

            var lastStoredFractionDate = await CallStage(
                activityName: nameof(GetLastEnglishFractionCalculatedDateActivity),
                retryCounterUpdater: summary => summary.GetLastEnglishFractionDateRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<DateTime?>(
                    nameof(GetLastEnglishFractionCalculatedDateActivity),
                    new GetLastEnglishFractionCalculatedDateActivityRequest(workItem.EmpRef, correlationId),
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef);
            if (!lastStoredFractionDate.Success)
            {
                return new PayePipelineResult { FailedItem = lastStoredFractionDate.FailedItem };
            }

            var fractionsFetchResult = await CallStage(
                activityName: nameof(GetEnglishFractionsActivity),
                retryCounterUpdater: summary => summary.GetEnglishFractionsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<EnglishFractionsFetchResult>(
                    nameof(GetEnglishFractionsActivity),
                    new GetEnglishFractionsActivityInput
                    {
                        CorrelationId = correlationId,
                        EmployerReference = workItem.EmpRef,
                        LastStoredFractionCalculatedDate = lastStoredFractionDate.Value
                    },
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!fractionsFetchResult.Success || fractionsFetchResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = fractionsFetchResult.FailedItem };
            }

            var fractionsPersistenceResult = await CallStage(
                activityName: nameof(PersistEnglishFractionsActivity),
                retryCounterUpdater: summary => summary.PersistEnglishFractionsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<EnglishFractionsPersistenceResult>(
                    nameof(PersistEnglishFractionsActivity),
                    fractionsFetchResult.Value,
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!fractionsPersistenceResult.Success || fractionsPersistenceResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = fractionsPersistenceResult.FailedItem };
            }

            var calculationDateResult = await CallStage(
                activityName: nameof(PersistEnglishFractionCalculationDateActivity),
                retryCounterUpdater: summary => summary.PersistEnglishFractionDateRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<EnglishFractionCalculationDatePersistenceResult>(
                    nameof(PersistEnglishFractionCalculationDateActivity),
                    fractionsFetchResult.Value,
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!calculationDateResult.Success || calculationDateResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = calculationDateResult.FailedItem };
            }

            var existingSubmissionIds = await CallStage(
                activityName: nameof(GetExistingLevySubmissionIdsActivity),
                retryCounterUpdater: summary => summary.GetExistingSubmissionIdsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<List<string>>(
                    nameof(GetExistingLevySubmissionIdsActivity),
                    new GetExistingLevySubmissionIdsActivityRequest(workItem.EmpRef, correlationId),
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!existingSubmissionIds.Success)
            {
                return new PayePipelineResult { FailedItem = existingSubmissionIds.FailedItem };
            }

            var existingPeriod12Declarations = await CallStage(
                activityName: nameof(GetExistingPeriod12LevyDeclarationsActivity),
                retryCounterUpdater: summary => summary.GetExistingPeriod12DeclarationsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<List<NormalizedLevyDeclaration>>(
                    nameof(GetExistingPeriod12LevyDeclarationsActivity),
                    new GetExistingPeriod12LevyDeclarationsActivityRequest(workItem.EmpRef, correlationId),
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!existingPeriod12Declarations.Success)
            {
                return new PayePipelineResult { FailedItem = existingPeriod12Declarations.FailedItem };
            }

            var normalizedResult = await CallStage(
                activityName: nameof(NormalizeLevyDeclarationsActivity),
                retryCounterUpdater: summary => summary.NormalizeLevyDeclarationsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<NormalizeLevyDeclarationsResult>(
                    nameof(NormalizeLevyDeclarationsActivity),
                    new NormalizeLevyDeclarationsInput
                    {
                        CorrelationId = correlationId,
                        AccountId = workItem.AccountId,
                        EmpRef = workItem.EmpRef,
                        HmrcDeclarations = levyResult.Value.LevyDeclarations?.Declarations ?? [],
                        ExistingSubmissionIds = existingSubmissionIds.Value ?? [],
                        ExistingPeriod12Declarations = existingPeriod12Declarations.Value ?? [],
                        ProcessingDate = context.CurrentUtcDateTime
                    },
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!normalizedResult.Success || normalizedResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = normalizedResult.FailedItem };
            }

            var persistLevyResult = await CallStage(
                activityName: nameof(PersistLevyDeclarationsActivity),
                retryCounterUpdater: summary => summary.PersistLevyDeclarationsRetries += MaxActivityAttempts - 1,
                activityCall: () => context.CallActivityAsync<PersistLevyDeclarationsActivityResult>(
                    nameof(PersistLevyDeclarationsActivity),
                    normalizedResult.Value,
                    ActivityRetryOptions),
                accountId: workItem.AccountId,
                empRef: workItem.EmpRef,
                fromDate: workItem.LastSubmissionDate);
            if (!persistLevyResult.Success || persistLevyResult.Value == null)
            {
                return new PayePipelineResult { FailedItem = persistLevyResult.FailedItem };
            }

            return new PayePipelineResult
            {
                LevyImportResult = levyResult.Value,
                EnglishFractionsFetchResult = fractionsFetchResult.Value,
                EnglishFractionsPersistenceResult = fractionsPersistenceResult.Value,
                EnglishFractionCalculationDatePersistenceResult = calculationDateResult.Value,
                PersistLevyDeclarationsActivityResult = persistLevyResult.Value
            };
        }

        async Task<ActivityStageResult<T>> CallStage<T>(
            string activityName,
            Func<Task<T>> activityCall,
            Action<ImportLevyRunSummary> retryCounterUpdater,
            long accountId,
            string empRef = "",
            DateTime? fromDate = null)
        {
            try
            {
                var value = await activityCall();
                return ActivityStageResult<T>.Succeeded(value);
            }
            catch (Exception ex)
            {
                retryCounterUpdater(result.RunSummary);
                return ActivityStageResult<T>.Failed(new ImportLevyFailedItem
                {
                    CorrelationId = correlationId,
                    AccountId = accountId,
                    EmpRef = empRef,
                    ActivityName = activityName,
                    FailureReason = ex.Message,
                    RetryAttempts = MaxActivityAttempts - 1,
                    FromDate = fromDate
                });
            }
        }

        static void PopulateRunSummary(ImportLevyResult runResult, List<PersistLevyDeclarationsActivityResult> persistResults)
        {
            runResult.RunSummary.PayeProcessed = persistResults.Count;
            runResult.RunSummary.LevyDeclarationsFetched = runResult.LevyDeclarationsActivityResults.Sum(x => x.DeclarationsCount);
            runResult.RunSummary.LevyDeclarationsNormalized = persistResults.Sum(x => x.DeclarationsSubmitted);
            runResult.RunSummary.LevyDeclarationsPersisted = persistResults.Sum(x => x.DeclarationsPersisted);
            runResult.RunSummary.LevyDeclarationsSkipped = persistResults.Sum(x => x.DeclarationsSkipped);
            runResult.RunSummary.TransactionsCreated = persistResults.Sum(x => x.TransactionsCreated);
            runResult.RunSummary.EnglishFractionsStored = runResult.EnglishFractionsPersistenceResults.Sum(x => x.Stored);
            runResult.RunSummary.EnglishFractionsIgnored = runResult.EnglishFractionsPersistenceResults.Sum(x => x.Ignored);
            runResult.RunSummary.EnglishFractionsSkipped = runResult.EnglishFractionsPersistenceResults.Count(x => x.Skipped);
            runResult.RunSummary.EnglishFractionCalculationDatesPersisted = runResult.EnglishFractionCalculationDatePersistenceResults.Count(x => x.Persisted);
            runResult.RunSummary.EnglishFractionCalculationDatesSkipped = runResult.EnglishFractionCalculationDatePersistenceResults.Count(x => x.Skipped);
            runResult.RunSummary.TotalFailures = runResult.FailedItems.Count;
            runResult.RunSummary.TotalRetries =
                runResult.RunSummary.GetPayeSchemesRetries
                + runResult.RunSummary.GetLastSubmissionDateRetries
                + runResult.RunSummary.GetLastEnglishFractionDateRetries
                + runResult.RunSummary.ImportLevyDeclarationsRetries
                + runResult.RunSummary.GetEnglishFractionsRetries
                + runResult.RunSummary.PersistEnglishFractionsRetries
                + runResult.RunSummary.PersistEnglishFractionDateRetries
                + runResult.RunSummary.GetExistingSubmissionIdsRetries
                + runResult.RunSummary.GetExistingPeriod12DeclarationsRetries
                + runResult.RunSummary.NormalizeLevyDeclarationsRetries
                + runResult.RunSummary.PersistLevyDeclarationsRetries;
        }
    }   
}
