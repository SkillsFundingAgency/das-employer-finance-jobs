using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
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
                "[CorrelationId: {CorrelationId}] No levy declarations to persist for AccountId {AccountId} EmpRef {EmpRef}. DeclarationsSubmitted {DeclarationsSubmitted}, DeclarationsPersisted {DeclarationsPersisted}, DeclarationsSkipped {DeclarationsSkipped}, TransactionsCreated {TransactionsCreated}. Duplicate {DuplicateCount}, Existing {ExistingCount}, Future {FutureCount}, PreLevy {PreLevyCount}",
                input.CorrelationId,
                input.AccountId,
                input.EmpRef,
                0,
                0,
                input.SourceDeclarationCount,
                0,
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

        ApiResponse<PersistLevyDeclarationsResponse> response;
        try
        {
            response = await retryService.ExecuteAsync(
                () => PostLevyDeclarations(input, request),
                input.CorrelationId,
                "Finance API POST /api/levy-declarations",
                IsTransientException);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Failed to persist levy declarations for AccountId {AccountId} EmpRef {EmpRef}",
                input.CorrelationId,
                input.AccountId,
                input.EmpRef);
            throw;
        }

        var body = response.Body!;

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

        if (response == null)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {input.CorrelationId}] Finance API returned no response while persisting levy declarations for AccountId {input.AccountId} EmpRef {input.EmpRef}.");
        }

        if (!IsSuccess(response.StatusCode))
        {
            throw new HttpRequestContentException(
                $"Finance API failed to persist levy declarations for AccountId {input.AccountId} EmpRef {input.EmpRef}. StatusCode: {response.StatusCode}",
                response.StatusCode,
                response.ErrorContent);
        }

        if (response.Body == null)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {input.CorrelationId}] Finance API returned a successful response without result metrics for AccountId {input.AccountId} EmpRef {input.EmpRef}.");
        }

        return response;
    }

    private static PersistLevyDeclarationRequestData CreateRequestData(NormalizeLevyDeclarationsResult input)
    {
        return new PersistLevyDeclarationRequestData
        {
            CorrelationId = input.CorrelationId,
            AccountId = input.AccountId,
            EmpRef = input.EmpRef,
            Declarations = input.Declarations,
            GenerateTransactions = true
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

    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            HttpRequestContentException contentException => IsTransient(contentException.StatusCode),
            HttpRequestException requestException when requestException.StatusCode.HasValue =>
                IsTransient(requestException.StatusCode.Value),
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false
        };
    }
}
