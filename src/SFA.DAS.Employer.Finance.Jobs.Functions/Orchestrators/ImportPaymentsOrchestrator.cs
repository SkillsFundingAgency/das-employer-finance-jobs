using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ImportPaymentsOrchestrator(ILogger<ImportPaymentsOrchestrator> logger, IPeriodEndService periodEndService)
{
    [Function("ImportPaymentsOrchestrator")]
    public async Task<ImportPaymentsResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ImportPaymentsOrchestratorInput>();
        var correlationId = input?.CorrelationId ?? context.NewGuid().ToString();
        var maxConcurrentAccounts = ImportPaymentsOptions.GetMaxConcurrentAccountsOrDefault(input?.MaxConcurrentAccounts);
        var maxConcurrentPeriodEnds = ImportPaymentsOptions.GetMaxConcurrentPeriodEndsOrDefault(input?.MaxConcurrentPeriodEnds);

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator started", correlationId);

        var result = new ImportPaymentsResult
        {
            CorrelationId = correlationId,
            Success = false
        };

        try
        {
            var newPeriodEnds = await context.CallActivityAsync<List<PeriodEnd>>(nameof(GetNewPeriodEndsActivity), correlationId);

            result.NewPeriodEndsCount = newPeriodEnds?.Count ?? 0;
            result.TotalPeriodEndsCount = newPeriodEnds?.Count ?? 0;

            if (newPeriodEnds is { Count: > 0 })
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Processing {Count} new period ends", correlationId, newPeriodEnds.Count);
                var createdPeriodEnds = await CreatePeriodEnds(context, newPeriodEnds, correlationId);
                result.CreatedPeriodEndsCount = createdPeriodEnds.Count;
                result.FailedPeriodEndsCount = newPeriodEnds.Count - createdPeriodEnds.Count;

                await ProcessPeriodEndAccountsInParallel(context, createdPeriodEnds, correlationId, maxConcurrentAccounts, maxConcurrentPeriodEnds);
            }
            else
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] No new period ends to process", correlationId);
            }

            result.Success = true;

            logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator completed successfully. Processed {Count} period ends", correlationId, result.NewPeriodEndsCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;

            logger.LogError(ex, "[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator failed: {ErrorMessage}", correlationId, ex.Message);
        }

        return result;
    }

    private async Task<List<PeriodEnd>> CreatePeriodEnds(
        TaskOrchestrationContext context,
        IReadOnlyCollection<PeriodEnd> newPeriodEnds,
        string correlationId)
    {
        var createTasks = new Dictionary<Task<PeriodEnd>, PeriodEnd>();

        foreach (var periodEnd in newPeriodEnds)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Scheduling CreatePeriodEndActivity for PeriodEnd: {Year}-{Month}",
                correlationId,
                periodEnd.CalendarPeriodYear,
                periodEnd.CalendarPeriodMonth);

            var createTask = context.CallActivityAsync<PeriodEnd>(
                nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity),
                new CreatePeriodEndActivityInput { PeriodEnd = periodEnd, CorrelationId = correlationId });

            createTasks.Add(createTask, periodEnd);
        }

        var createdPeriodEnds = new List<PeriodEnd>();
        while (createTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(createTasks.Keys);
            var periodEnd = createTasks[completedTask];
            createTasks.Remove(completedTask);

            try
            {
                createdPeriodEnds.Add(await completedTask);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] Failed to create PeriodEnd {PeriodEndId}. Continuing with remaining period ends.",
                    correlationId,
                    GetPeriodEndRef(periodEnd));
            }
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Created {CreatedCount} of {TotalCount} period ends before starting account imports",
            correlationId,
            createdPeriodEnds.Count,
            newPeriodEnds.Count);

        return createdPeriodEnds;
    }

    private async Task ProcessPeriodEndAccountsInParallel(
        TaskOrchestrationContext context,
        IReadOnlyCollection<PeriodEnd> periodEnds,
        string correlationId,
        int maxConcurrentAccounts,
        int maxConcurrentPeriodEnds)
    {
        var activePeriodEndTasks = new List<Task<PeriodEndResult>>();
        var periodEndsScheduled = 0;

        foreach (var periodEnd in periodEnds)
        {
            var periodEndRef = GetPeriodEndRef(periodEnd);
            var instanceId = $"ProcessPeriodEndAccounts-PeriodEnd-{periodEndRef}-Correlation-{correlationId}";

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Scheduling ProcessPeriodEndAccountsOrchestrator for PeriodEndRef: {PeriodEndRef} with InstanceId: {InstanceId}",
                correlationId,
                periodEndRef,
                instanceId);

            activePeriodEndTasks.Add(context.CallSubOrchestratorAsync<PeriodEndResult>(
                nameof(ProcessPeriodEndOrchestrator.ProcessPeriodEndAccountsOrchestrator),
                new ProcessPeriodEndOrchestratorInput
                {
                    CorrelationId = correlationId,
                    PeriodEnd = periodEnd,
                    MaxConcurrentAccounts = maxConcurrentAccounts
                },
                new SubOrchestrationOptions { InstanceId = instanceId }));

            periodEndsScheduled++;

            if (activePeriodEndTasks.Count < maxConcurrentPeriodEnds)
            {
                continue;
            }

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Period end concurrency limit reached ({BatchSize}/{ConcurrencyLimit} active, {ScheduledSoFar}/{TotalCount} scheduled). Waiting for one to complete before scheduling more.",
                correlationId,
                activePeriodEndTasks.Count,
                maxConcurrentPeriodEnds,
                periodEndsScheduled,
                periodEnds.Count);

            var completedTask = await Task.WhenAny(activePeriodEndTasks);
            activePeriodEndTasks.Remove(completedTask);
            await ObservePeriodEndAccountProcessingResult(completedTask, correlationId);
        }

        if (activePeriodEndTasks.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Waiting for the remaining {BatchSize} active period end orchestrators to complete",
            correlationId,
            activePeriodEndTasks.Count);

        while (activePeriodEndTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(activePeriodEndTasks);
            activePeriodEndTasks.Remove(completedTask);
            await ObservePeriodEndAccountProcessingResult(completedTask, correlationId);
        }
    }

    private async Task ObservePeriodEndAccountProcessingResult(Task<PeriodEndResult> task, string correlationId)
    {
        try
        {
            var result = await task;
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Period end account processing completed for PeriodEndId {PeriodEndId}. Total account imports scheduled: {TotalCommandsPublished}",
                correlationId,
                result.PeriodEndId,
                result.TotalCommandsPublished);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Period end account processing failed. Continuing with remaining period ends.",
                correlationId);
        }
    }

    private static string GetPeriodEndRef(PeriodEnd periodEnd)
    {
        return string.IsNullOrWhiteSpace(periodEnd.PeriodEndId)
            ? periodEnd.Id.ToString()
            : periodEnd.PeriodEndId;
    }

    private static Guid DeterministicGuid(string input)
    {
        using var provider = System.Security.Cryptography.MD5.Create();
        var hash = provider.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }


    [Function("GetNewPeriodEndsActivity")]
    public async Task<List<PeriodEnd>> GetNewPeriodEndsActivity([ActivityTrigger] string correlationId)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] GetNewPeriodEndsActivity started", correlationId);
        return await periodEndService.GetNewPeriodEndsAsync(correlationId);
    }

    //This activity will be here to process period end login TODO: Implement actual processing logic
    [Function("ProcessPeriodEndActivity")]
    public async Task ProcessPeriodEndActivity([ActivityTrigger] ProcessPeriodEndInput input)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] Processing period end: Year={Year}, Period={Period}", input.CorrelationId, input.PeriodEnd.CalendarPeriodYear, input.PeriodEnd.PaymentsForPeriod);
        await Task.CompletedTask;
    }
}

public class ProcessPeriodEndInput
{
    public string CorrelationId { get; set; }
    public PeriodEnd PeriodEnd { get; set; }
}
