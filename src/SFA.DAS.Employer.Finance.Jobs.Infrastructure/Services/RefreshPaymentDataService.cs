using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
public class RefreshPaymentDataService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient,
    ILogger<PeriodEndService> logger) : IRefreshPaymentDataService
{
    public async Task<RefreshPaymentDataResult> RefreshPaymentData(RefreshPaymentDataInput input)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] RefreshPaymentDataActivity started", input.CorrelationId);
        //getting payments from provider events api
        var retrivedPayments = await GetAccountPaymentsFromExternalAsync(input.PeriodEnd, input.AccountId, input.CorrelationId);
        if (!retrivedPayments.Any())
        {
            return new RefreshPaymentDataResult();
        }
        //getting existing payment ids from finance api
        var existingPaymentIds = await GetExistingFinancePaymentIdsAsync(input.AccountId, input.CorrelationId);

        logger.LogInformation("[CorrelationId: {CorrelationId}] Retrieved {paymentsCount} payments from Provider Events API and {existingCount} payment Ids from Finance API",
                                                 input.CorrelationId, retrivedPayments.Count, existingPaymentIds.Count);
        //filtering the payments
        var filteredPayments = FilterPayments(retrivedPayments, existingPaymentIds, input.CorrelationId);
        if (!filteredPayments.Any())
        {
            return new RefreshPaymentDataResult
            {
                PaymentsCreated = 0,
                PaymentDetails = retrivedPayments
            };
        }
   
        var paymentsCreated = await PostPaymentsToStaging(filteredPayments, input.CorrelationId);
    
        return new RefreshPaymentDataResult
        {
            PaymentsCreated = paymentsCreated,
            PaymentDetails = retrivedPayments
        };
    }
    private async Task<List<Payment>> GetAccountPaymentsFromExternalAsync(string periodEnd, long accountId, string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Provider Events API to get payments for PeriodEnd:{periodEnd} AccountId: {accountId}", correlationId, periodEnd, accountId);

            var request = new GetAccountPaymentsRequest(periodEnd, accountId);
            //var response = await providerPaymentApiClient.GetWithResponseCode<List<PaymentDetails>>(request);

            var retryPolicy = GetPaymentsFromEventsApiRetryPolicy(correlationId);
            var response = await retryPolicy.ExecuteAsync(() => providerPaymentApiClient.GetWithResponseCode<GetPaymentsResponse>(request));
            if (response == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Provider Events API for PeriodEnd:{periodEnd} AccountId: {accountId}. Assuming no payments.", correlationId, periodEnd, accountId);
                return new List<Payment>();
            }
            if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Provider Events API returned {StatusCode} with error: {ErrorContent}. Assuming no Assuming no payments.", correlationId, response.StatusCode, response.ErrorContent);
                return new List<Payment>();
            }
            var paymentsResponse = response?.Body;

            if (paymentsResponse == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Got null respons body from Provider Events API for PeriodEnd:{periodEnd} AccountId: {accountId}. Assuming no payments.", correlationId, periodEnd, accountId);
                return new List<Payment>();
            }
            var items = paymentsResponse.Result.Items.ToList();
            var payments = items ?? new List<Payment>();
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} payments for AccountId:{accountId} PeriodEnd:{periodEnd} from Provider Events API", correlationId, (payments?.Count ?? 0), periodEnd, accountId);
            return payments!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving payments from Provider Events API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    private async Task<List<string>> GetExistingFinancePaymentIdsAsync(long accountId, string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing payment Ids for AccountId:{accountId}", correlationId, accountId);

            var request = new GetExistingPaymentIdsRequest(accountId);

            var retryPolicy = GetExistingPaymentsFromFinanceApiRetryPolicy(correlationId);
            var response = await retryPolicy.ExecuteAsync(() => financeApiClient.GetWithResponseCode<GetAccountPaymentIdsResponse>(request));
            if (response == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Finance API. Assuming no existing payment ids for AccountId:{accountId}", correlationId, accountId);
                return new List<string>();
            }
            if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} with error: {ErrorContent}. Assuming no existing payment ids for AccountId:{accountId}", correlationId, response.StatusCode, response.ErrorContent, accountId);
                return new List<string>();
            }
            var paymentIdsResponse = response?.Body;
            if (paymentIdsResponse == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Received null response body from Finance API. Assuming no existing payment ids for AccountId:{accountId}", correlationId, accountId);
                return new List<string>();
            }
            var paymentIds = paymentIdsResponse.PaymentIds;
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} period ends from Finance API", correlationId, paymentIds?.Count ?? 0);

            return paymentIds ?? new List<string>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving period ends from Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
 
    private async Task<int> PostPaymentsToStaging(List<Payment> filteredPayments, string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to upsert payments to staging", correlationId);

            var retryPolicy = GetPostPaymentsToStagingRetryPolicy(correlationId);

            var request = new PostPaymentsToStagingRequest<List<Payment>>(filteredPayments);
            var response = await retryPolicy.ExecuteAsync(() => financeApiClient.PostWithResponseCode<List<Payment>, PostPaymentsToStagingResponse>(request, false));
            if (response == null)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Finance API. Assuming no payments upserted to staging.", correlationId);
                return 0;
            }
            if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} with error: {ErrorContent}. Assuming no payments upserted to staging.", correlationId, response.StatusCode, response.ErrorContent);
                return 0;
            }
            var paymentsResponse = response?.Body;
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully upserted {Count} payments to staging.", correlationId, paymentsResponse?.InsertedCount ?? 0);

            return paymentsResponse?.InsertedCount ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error upserting payments to staging in Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
    private List<Payment> FilterPayments(List<Payment> externalPayments, List<string> existingPaymentIds, string correlationId)
    {
        var existingIdsSet = existingPaymentIds.ToHashSet();
        var filteredPayments = externalPayments
                                            .Where(p => !existingIdsSet.Contains(p.Id!) && p.FundingSource != FundingSource.FullyFundedSfa)
                                            .ToList();

        logger.LogInformation("[CorrelationId: {CorrelationId}] Filtered {NewCount} payments out of {TotalCount} paymens retrived from Provider Events API.",
                                            correlationId, filteredPayments.Count, externalPayments.Count);
        return filteredPayments;
    }

    #region Retry Policies
    private AsyncRetryPolicy<ApiResponse<PostPaymentsToStagingResponse>> GetPostPaymentsToStagingRetryPolicy(string correlationId)
    {
        return Policy.Handle<HttpRequestException>()
                     .Or<TaskCanceledException>()
                     .OrResult<ApiResponse<PostPaymentsToStagingResponse>>(response => response == null || (int)response.StatusCode >= 500)
                     .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: retry => TimeSpan.FromSeconds(2),
                                        onRetry: (outcome, delay, retryCount, context) =>
                                        {
                                            logger.LogWarning(
                                                "[CorrelationId: {CorrelationId}] Retry {RetryCount} calling Finance API. Reason: {Reason}",
                                                correlationId,
                                                retryCount,
                                                outcome.Exception?.Message ??
                                                outcome.Result?.ErrorContent);
                                        });
    }
    private AsyncRetryPolicy<ApiResponse<GetPaymentsResponse>> GetPaymentsFromEventsApiRetryPolicy(string correlationId)
    {
        return Policy.Handle<HttpRequestException>()
                     .Or<TaskCanceledException>()
                     .OrResult<ApiResponse<GetPaymentsResponse>>(response => response == null || (int)response.StatusCode >= 500)
                     .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(2),
                                        onRetry: (outcome, delay, retryCount, context) =>
                                        {
                                            logger.LogWarning(
                                                "[CorrelationId: {CorrelationId}] Retry {RetryCount} calling Provider API",
                                                correlationId,
                                                retryCount);
                                        });
    }
    private AsyncRetryPolicy<ApiResponse<GetAccountPaymentIdsResponse>> GetExistingPaymentsFromFinanceApiRetryPolicy(string correlationId)
    {
        return Policy.Handle<HttpRequestException>()
                    .Or<TaskCanceledException>()
                    .OrResult<ApiResponse<GetAccountPaymentIdsResponse>>(r => r == null || (int)r.StatusCode >= 500)
                    .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(2),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            logger.LogWarning(
                                "[CorrelationId: {CorrelationId}] Retry {RetryCount} getting existing payments",
                                correlationId,
                                retryCount);
                        });
    }
    #endregion
}