using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;


namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ImportPaymentsAdmin(
    ILogger<ImportPaymentsAdmin> logger)
{
    private const int MaxConcurrentAccounts = 50;

    [Function("ImportPaymentsAdmin_StartPeriodEnd")]
    public async Task<HttpResponseData> StartPeriodEnd(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/imports/period-end")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        var payload = await request.ReadFromJsonAsync<StartPeriodEndImportRequest>();
        if (payload?.PeriodEnd == null)
        {
            return await request.CreateErrorResponse(HttpStatusCode.BadRequest, "PeriodEnd payload is required.");
        }

        var periodEndRef = string.IsNullOrWhiteSpace(payload.PeriodEnd.PeriodEndId)
            ? payload.PeriodEnd.Id.ToString()
            : payload.PeriodEnd.PeriodEndId;
        if (string.IsNullOrWhiteSpace(periodEndRef))
        {
            return await request.CreateErrorResponse(HttpStatusCode.BadRequest, "PeriodEndId is required.");
        }

        var maxConcurrent = payload.MaxConcurrentAccounts
                           ?? MaxConcurrentAccounts;

        var instanceId = $"ProcessPeriodEnd-{periodEndRef}";
        var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProcessPeriodEndOrchestrator),
            new ProcessPeriodEndOrchestratorInput
            {
                PeriodEnd = payload.PeriodEnd,
                MaxConcurrentAccounts = maxConcurrent
            },
            new StartOrchestrationOptions { InstanceId = instanceId });

        logger.LogInformation("Started ProcessPeriodEndOrchestrator instance {InstanceId} for period end {PeriodEndRef}", newInstanceId, periodEndRef);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId = newInstanceId });
        return response;
    }

    [Function("ImportPaymentsAdmin_StartAccount")]
    public async Task<HttpResponseData> StartAccount(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/imports/account")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        var payload = await request.ReadFromJsonAsync<StartAccountImportRequest>();
        if (payload == null || payload.AccountId <= 0 || string.IsNullOrWhiteSpace(payload.PeriodEndRef))
        {
            return await request.CreateErrorResponse(HttpStatusCode.BadRequest, "AccountId and PeriodEndRef are required.");
        }

        var idempotencyKey = payload.IdempotencyKey ?? DeterministicGuid($"ImportAccountPayments-{payload.PeriodEndRef}-{payload.AccountId}");
        var instanceId = $"ProcessAccount-{payload.PeriodEndRef}-{payload.AccountId}";

        var input = new ProcessAccountInput
        {
            AccountId = payload.AccountId,
            PeriodEndRef = payload.PeriodEndRef,
            CorrelationId = Guid.NewGuid().ToString(),
            IdempotencyKey = idempotencyKey.ToString(),
            TriggeredAt = DateTime.UtcNow
        };

        var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProcessAccountOrchestrator),
            input,
            new StartOrchestrationOptions { InstanceId = instanceId });

        logger.LogInformation("Started ProcessAccountOrchestrator instance {InstanceId} for AccountId {AccountId} PeriodEnd {PeriodEndRef}",
            newInstanceId,
            payload.AccountId,
            payload.PeriodEndRef);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId = newInstanceId, idempotencyKey });
        return response;
    }

    [Function("ImportPaymentsAdmin_Status")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/imports/status/{instanceId}")] HttpRequestData request,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return await request.CreateErrorResponse(HttpStatusCode.BadRequest, "instanceId is required.");
        }

        var status = await client.GetInstanceAsync(instanceId);
        if (status == null)
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(status);
        return response;
    }

    private static Guid DeterministicGuid(string input)
    {
        using var provider = System.Security.Cryptography.MD5.Create();
        var hash = provider.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    private class StartPeriodEndImportRequest
    {
        public PeriodEnd PeriodEnd { get; set; }
        public int? MaxConcurrentAccounts { get; set; }
    }

    private class StartAccountImportRequest
    {
        public long AccountId { get; set; }
        public string PeriodEndRef { get; set; }
        public Guid? IdempotencyKey { get; set; }
    }
}

internal static class HttpResponseExtensions
{
    public static async Task<HttpResponseData> CreateErrorResponse(this HttpRequestData request, HttpStatusCode statusCode, string message)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
