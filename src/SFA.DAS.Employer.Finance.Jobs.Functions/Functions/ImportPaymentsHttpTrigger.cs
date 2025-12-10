using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ImportPaymentsHttpTrigger(ILogger<ImportPaymentsHttpTrigger> logger)
{
    [Function("ImportPaymentsHttpTrigger")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "orchestrators/ImportPayments")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter)
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsHttpTrigger invoked", correlationId);

        try
        {
            var instanceId = "ImportPaymentsOrchestrator-Singleton";
            
            var existingInstance = await starter.GetInstanceAsync(instanceId);
            if (existingInstance != null && (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running 
                                          || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                
                var response = req.CreateResponse(HttpStatusCode.Conflict);
                await response.WriteStringAsync($"Orchestrator is already running. InstanceId: {existingInstance.InstanceId}");
                return response;
            }

            var newInstanceId = await starter.ScheduleNewOrchestrationInstanceAsync("ImportPaymentsOrchestrator",
                new ImportPaymentsOrchestratorInput
                {
                    CorrelationId = correlationId,
                    TriggeredAt = DateTime.UtcNow
                },
                new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                });

            logger.LogInformation("[CorrelationId: {CorrelationId}] Started ImportPaymentsOrchestrator with InstanceId: {InstanceId}", correlationId, newInstanceId);

            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteStringAsync($"Orchestrator started. InstanceId: {newInstanceId}");
            return successResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ImportPaymentsOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}


