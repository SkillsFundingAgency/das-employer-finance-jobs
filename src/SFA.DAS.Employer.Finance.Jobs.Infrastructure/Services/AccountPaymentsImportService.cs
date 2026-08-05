using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountPaymentsImportService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient,
    ILogger<AccountPaymentsImportService> logger) : IAccountPaymentsImportService
{
    public async Task<AccountPaymentsImportResult> ImportAccountPaymentsAsync(AccountPaymentsImportInput input, CancellationToken cancellationToken)
    {
        logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] ImportAccountPaymentsAsync started. AccountId {AccountId}, PeriodEnd {PeriodEndRef}, IdempotencyKey {IdempotencyKey}",
                        input.CorrelationId,
                        input.AccountId,
                        input.PeriodEndRef,
                        input.IdempotencyKey);

        var result = await GetAllPayments(accountId: input.AccountId, periodEnd: input.PeriodEndRef, correlationId: input.CorrelationId.ToString());

        logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} payments for AccountId:{accountId} PeriodEnd:{periodEnd} from Provider Events API",
                                input.CorrelationId.ToString(),
                                result.Payments.Count,
                                input.AccountId,
                                input.PeriodEndRef);
        return result;
    }

    private async Task<AccountPaymentsImportResult> GetAllPayments(long accountId, string periodEnd, string correlationId)
    {
        var allPayments = new List<Payment>();
        var status = "Succeeded";
        var message = string.Empty;
        var totalPages = 1;

        for (var index = 1; index <= totalPages; index++)
        {
            var request = new GetAccountPaymentsRequest(periodEnd, accountId, index);
            var response = await providerPaymentApiClient.GetWithResponseCode<GetPaymentsResponse>(request);

            var paymentsResponse = response.Body;
            if (paymentsResponse == null)
            {
                throw new InvalidOperationException(
                    $"Provider Events API returned {response.StatusCode} without a response body for PeriodEnd:{periodEnd} AccountId:{accountId}.");
            }

            var payments = paymentsResponse.Items?.ToList() ?? [];
            totalPages = paymentsResponse.TotalNumberOfPages;
            allPayments.AddRange(payments);
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved payments page {Index} of {TotalPages} for AccountId = {EmployerAccountId}, PeriodEnd={PeriodEnd}",
                                    correlationId, index, totalPages, accountId, periodEnd);
        }
        return new AccountPaymentsImportResult
        {
            Payments = allPayments!,
            Status = status,
            Message = allPayments.Count > 0 ? $"Successfully retrieved {allPayments.Count} payments" : message
        };
    }

    public async Task<AccountExistingPaymentIdsImportResult> ImportAccountExistingPaymentIdsAsync(long accountId, string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing payment Ids for AccountId:{accountId}", correlationId, accountId);

            var request = new GetExistingPaymentIdsRequest(accountId);

            var response = await financeApiClient.GetWithResponseCode<GetAccountPaymentIdsResponse>(request);
            var paymentIdsResponse = response.Body
                ?? throw new InvalidOperationException(
                    $"Finance API returned {response.StatusCode} without a response body for AccountId:{accountId}.");

            var paymentIds = paymentIdsResponse.PaymentIds ?? [];
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} existing payment ids from Finance API", correlationId, paymentIds?.Count ?? 0);

            return new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = paymentIds!,
                Status = "Succeeded",
                Message = $"Successfully retrieved {(paymentIds?.Count ?? 0)} payments"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving existing payment ids from Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}
