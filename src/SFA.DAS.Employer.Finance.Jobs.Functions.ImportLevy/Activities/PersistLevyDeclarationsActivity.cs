using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class PersistLevyDeclarationsActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<PersistLevyDeclarationsActivity> logger)
{
    [Function("PersistLevyDeclarationsActivity")]
    public async Task<PersistLevyDeclarationsActivityResult> Run([ActivityTrigger] NormalizeLevyDeclarationsResult input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Declarations.Count == 0)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] No levy declarations to persist for AccountId {AccountId} EmpRef {EmpRef}. SourceCount {SourceCount}, Duplicate {DuplicateCount}, Existing {ExistingCount}, Future {FutureCount}, PreLevy {PreLevyCount}",
                input.CorrelationId,
                input.AccountId,
                input.EmpRef,
                input.SourceDeclarationCount,
                input.DuplicateDeclarationCount,
                input.ExistingDeclarationCount,
                input.FutureDeclarationCount,
                input.PreLevyDeclarationCount);

            return new PersistLevyDeclarationsActivityResult
            {
                CorrelationId = input.CorrelationId,
                AccountId = input.AccountId,
                EmpRef = input.EmpRef,
                Success = true,
                DeclarationsSubmitted = 0,
                DeclarationsSkipped = input.SourceDeclarationCount,
                Message = "No levy declarations to persist."
            };
        }

        var request = new PersistLevyDeclarationsRequest(CreateRequestData(input));

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Persisting {DeclarationCount} levy declarations for AccountId {AccountId} EmpRef {EmpRef} with GenerateTransactions {GenerateTransactions}",
            input.CorrelationId,
            input.Declarations.Count,
            input.AccountId,
            input.EmpRef,
            true);

        var response = await retryService.ExecuteAsync(
            () => PostLevyDeclarations(input, request),
            input.CorrelationId,
            "Finance API POST /api/levy-declarations");

        if (response == null || !IsSuccess(response.StatusCode))
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {input.CorrelationId}] Finance API failed to persist levy declarations for AccountId {input.AccountId} EmpRef {input.EmpRef}. StatusCode: {response?.StatusCode}. Error: {response?.ErrorContent}");
        }

        var body = response.Body ?? new PersistLevyDeclarationsResponse();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Persisted levy declarations for AccountId {AccountId} EmpRef {EmpRef}. DeclarationsSubmitted {DeclarationsSubmitted}, DeclarationsPersisted {DeclarationsPersisted}, DeclarationsSkipped {DeclarationsSkipped}, TransactionsCreated {TransactionsCreated}",
            input.CorrelationId,
            input.AccountId,
            input.EmpRef,
            input.Declarations.Count,
            body.DeclarationsPersisted,
            body.DeclarationsSkipped,
            body.TransactionsCreated);

        return new PersistLevyDeclarationsActivityResult
        {
            CorrelationId = input.CorrelationId,
            AccountId = input.AccountId,
            EmpRef = input.EmpRef,
            Success = true,
            DeclarationsSubmitted = input.Declarations.Count,
            DeclarationsPersisted = body.DeclarationsPersisted,
            DeclarationsSkipped = body.DeclarationsSkipped,
            TransactionsCreated = body.TransactionsCreated,
            Message = "Levy declarations persisted."
        };
    }

    private async Task<ApiResponse<PersistLevyDeclarationsResponse>> PostLevyDeclarations(
        NormalizeLevyDeclarationsResult input,
        PersistLevyDeclarationsRequest request)
    {
        var response = await financeApi.PostWithResponseCode<PersistLevyDeclarationsResponse>(request);

        if (response != null && IsTransient(response.StatusCode))
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {input.CorrelationId}] Transient Finance API failure persisting levy declarations for AccountId {input.AccountId} EmpRef {input.EmpRef}. StatusCode: {response.StatusCode}. Error: {response.ErrorContent}");
        }

        return response!;
    }

    private static PersistLevyDeclarationRequestData CreateRequestData(NormalizeLevyDeclarationsResult input)
    {
        return new PersistLevyDeclarationRequestData
        {
            AccountId = input.AccountId,
            EmpRef = input.EmpRef,
            Declarations = input.Declarations
        };
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
    {
        return (int)statusCode is >= 200 and <= 299;
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
