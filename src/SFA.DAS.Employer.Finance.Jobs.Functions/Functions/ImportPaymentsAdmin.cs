using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;
using System.Net;
using System.Text;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ImportPaymentsAdmin(
    ILogger<ImportPaymentsAdmin> logger,
    IOptions<ImportPaymentsOptions> importPaymentsOptions)
{
    private readonly ImportPaymentsOptions _options = importPaymentsOptions.Value;

    [Function("ImportPaymentsAdmin_StartPeriodEnd")]
    public async Task<HttpResponseData> StartPeriodEnd(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "imports/admin/period-end")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        if (IsAdminEndpointDisabled())
        {
            return await CreateDisabledResponse(request);
        }

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

        var maxConcurrent = ImportPaymentsOptions.GetMaxConcurrentAccountsOrDefault(payload.MaxConcurrentAccounts);
        var correlationId = Guid.NewGuid().ToString();

        var instanceId = $"ProcessPeriodEnd-PeriodEnd-{periodEndRef}-Correlation-{correlationId}";
        var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProcessPeriodEndOrchestrator),
                new ProcessPeriodEndOrchestratorInput
                {
                    CorrelationId = correlationId,
                    PeriodEnd = payload.PeriodEnd,
                    MaxConcurrentAccounts = maxConcurrent
                },
            new StartOrchestrationOptions { InstanceId = instanceId });

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Started ProcessPeriodEndOrchestrator instance {InstanceId} for period end {PeriodEndRef}",
            correlationId,
            newInstanceId,
            periodEndRef);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId = newInstanceId, correlationId });
        return response;
    }

    [Function("ImportPaymentsAdmin_StartAccount")]
    public async Task<HttpResponseData> StartAccount(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "imports/admin/account")] HttpRequestData request,
        [DurableClient] DurableTaskClient client)
    {
        if (IsAdminEndpointDisabled())
        {
            return await CreateDisabledResponse(request);
        }

        var payload = await request.ReadFromJsonAsync<StartAccountImportRequest>();
        if (payload == null || payload.AccountId <= 0 || string.IsNullOrWhiteSpace(payload.PeriodEndRef))
        {
            return await request.CreateErrorResponse(HttpStatusCode.BadRequest, "AccountId and PeriodEndRef are required.");
        }

        var correlationId = Guid.NewGuid().ToString();
        var idempotencyKey = payload.IdempotencyKey ?? DeterministicGuid($"ImportAccountPayments-{payload.PeriodEndRef}-{payload.AccountId}");
        var instanceId = $"ProcessAccount-PeriodEnd-{payload.PeriodEndRef}-Account-{payload.AccountId}-Correlation-{correlationId}";

        var input = new ProcessAccountInput
        {
            AccountId = payload.AccountId,
            PeriodEndRef = payload.PeriodEndRef,
            CorrelationId = correlationId,
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
        await response.WriteAsJsonAsync(new { instanceId = newInstanceId, correlationId, idempotencyKey });
        return response;
    }

    [Function("ImportPaymentsAdmin_Status")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "imports/admin/status/{instanceId}")] HttpRequestData request,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        if (IsAdminEndpointDisabled())
        {
            return await CreateDisabledResponse(request);
        }

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

    private bool IsAdminEndpointDisabled() => !_options.AdminEndpointsEnabled;

    private async Task<HttpResponseData> CreateDisabledResponse(HttpRequestData request)
    {
        logger.LogWarning("Import payments admin endpoint rejected because ImportPaymentsOptions.AdminEndpointsEnabled is false.");
        return await request.CreateErrorResponse(HttpStatusCode.Forbidden, "Import payments admin endpoints are disabled.");
    }

    private static Guid DeterministicGuid(string input)
    {
        using var provider = System.Security.Cryptography.MD5.Create();
        var hash = provider.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
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
