using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Functions.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions.HttpTriggers;

public class FundingProjectionHttpTrigger(ILogger<FundingProjectionHttpTrigger> logger)
{
    // This function is triggered by an HTTP POST request to start the funding projection orchestration.
    [Function(nameof(FundingProjectionHttpTrigger))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "funding-projection/run")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        var input = await request.ReadFromJsonAsync<ImportLearnersCostEvent>();
        if (input is null || input.JobId == Guid.Empty || input.AccountId <= 0)
        {
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "JobId and AccountId are required." });
            return badRequest;
        }

        var correlationId = Guid.NewGuid().ToString();
        var instanceId = await FundingProjectionOrchestrationHandler.StartAsync(client, input, logger, correlationId);
        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId, correlationId });
        return response;
    }
}
