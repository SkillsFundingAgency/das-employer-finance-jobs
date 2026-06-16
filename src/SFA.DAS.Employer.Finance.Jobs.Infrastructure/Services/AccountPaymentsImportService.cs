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
            if (response == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Provider Events API for PeriodEnd:{periodEnd} AccountId: {accountId}. Assuming no payments.", correlationId, periodEnd, accountId);
                status = "Failed";
                message = "No response received from Provider Events API";
                continue;
            }
            if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Provider Events API returned {StatusCode} with error: {ErrorContent}. Assuming no payments.", correlationId, response.StatusCode, response.ErrorContent);
                status = "Failed";
                message = "No response received from Provider Events API";
                continue;
            }
            var paymentsResponse = response?.Body;

            if (paymentsResponse == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Got null response body from Provider Events API for PeriodEnd:{periodEnd} AccountId: {accountId}. Assuming no payments.", correlationId, periodEnd, accountId);
                status = "Failed";
                message = "Got null response body from Provider Events API.";
                continue;
            }

            var payments = paymentsResponse.Items.ToList();
            if (payments == null)
            {
                continue;
            }

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
            if (response == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Finance API. Assuming no existing payment ids for AccountId:{accountId}", correlationId, accountId);
                return new AccountExistingPaymentIdsImportResult
                {
                    PaymentIds = new List<string>(),
                    Status = "Failed",
                    Message = "No response received from Finance API"
                };
            }
            if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} with error: {ErrorContent}. Assuming no existing payment ids for AccountId:{accountId}", correlationId, response.StatusCode, response.ErrorContent, accountId);
                return new AccountExistingPaymentIdsImportResult
                {
                    PaymentIds = new List<string>(),
                    Status = "Failed",
                    Message = $"Finance API returned {response.StatusCode} with error: {response.ErrorContent}"
                };
            }
            var paymentIdsResponse = response?.Body;
            if (paymentIdsResponse == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Received null response body from Finance API. Assuming no existing payment ids for AccountId:{accountId}", correlationId, accountId);
                return new AccountExistingPaymentIdsImportResult
                {
                    PaymentIds = new List<string>(),
                    Status = "Failed",
                    Message = "Got null response body from Finance API."
                };
            }
            var paymentIds = paymentIdsResponse?.PaymentIds ?? new List<string>();
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
