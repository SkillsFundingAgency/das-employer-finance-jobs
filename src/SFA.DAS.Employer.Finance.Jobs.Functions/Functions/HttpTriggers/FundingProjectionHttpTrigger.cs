using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions.HttpTriggers;

public class FundingProjectionHttpTrigger(ILogger<FundingProjectionHttpTrigger> logger)
{
    // This function is triggered by an HTTP POST request to start the funding projection orchestration.
    [Function(nameof(FundingProjectionHttpTrigger))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "funding-projection/run")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        var correlationId = Guid.NewGuid().ToString();
        var instanceId = await FundingProjectionOrchestrationHandler.StartAsync(client, logger, correlationId);

        if (instanceId == null)
        {
            var conflictResponse = request.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(new
            {
                message = "Funding projection orchestration is already running.",
                instanceId = FundingProjectionOrchestrationHandler.InstanceId,
                correlationId
            });
            return conflictResponse;
        }

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId, correlationId });
        return response;
    }
}