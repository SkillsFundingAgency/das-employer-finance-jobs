using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities
{
    public interface IIdempotencyStore
    {
        Task<T> GetAsync<T>(string key);
        Task SaveAsync<T>(string key, T value);
    }

    public class GetAccountPaymentIdsRequest : IApiRequest
    {
        public GetAccountPaymentIdsRequest(long accountId)
        {
            AccountId = accountId;
        }

        public long AccountId { get; }

        public string GetUrl => $"/api/accounts/{AccountId}/payments/ids";
        public object? Data => null;
    }

    public class GetAccountPaymentsRequest : IApiRequest
    {
        public GetAccountPaymentsRequest(string periodEndId, long accountId, string correlationId)
        {
            PeriodEndId = periodEndId;
            AccountId = accountId;
            CorrelationId = correlationId;
        }

        public string PeriodEndId { get; }
        public long AccountId { get; }
        public string CorrelationId { get; }

        public string GetUrl =>
            $"/provider-api/accounts/{AccountId}/payments?periodEnd={PeriodEndId}&correlationId={CorrelationId}";
        public object? Data => null;
    }

    public class RefreshPaymentDataActivity
    {
        private readonly IProviderPaymentApiClient<ProviderEventsApiConfiguration> _providerApi;
        private readonly IFinanceApiClient<FinanceApiConfiguration> _financeApi;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<RefreshPaymentDataActivity> _logger;

        public RefreshPaymentDataActivity(
            IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerApi,
            IFinanceApiClient<FinanceApiConfiguration> financeApi,
            IIdempotencyStore idempotencyStore,
            ILogger<RefreshPaymentDataActivity> logger)
        {
            _providerApi = providerApi;
            _financeApi = financeApi;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        [Function("RefreshPaymentDataActivity")]
        public async Task<RefreshPaymentDataResult> Run([ActivityTrigger] RefreshPaymentDataInput input)
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Refreshing payments for account {AccountId}",
                input.CorrelationId,
                input.AccountId);

            var cachedResult =
                await _idempotencyStore.GetAsync<RefreshPaymentDataResult>(input.IdempotencyKey);

            if (cachedResult != null)
            {
                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Returning cached result for key {Key}",
                    input.CorrelationId,
                    input.IdempotencyKey);

                return cachedResult;
            }

            var allPayments = await RetryAsync(async () =>
            {
                var response =
                    await _providerApi.GetWithResponseCode<List<Payment>>(
                        new GetAccountPaymentsRequest(
                            input.PeriodEnd.PeriodEndId,
                            input.AccountId,
                            input.CorrelationId));

                return response?.Body ?? new List<Payment>();
            }, input.CorrelationId);

            if (!allPayments.Any())
            {
                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] No payments found for account {AccountId}",
                    input.CorrelationId,
                    input.AccountId);

                var emptyResult = new RefreshPaymentDataResult
                {
                    CorrelationId = input.CorrelationId,
                    PaymentsCreated = 0,
                    PaymentDetails = allPayments
                };

                await _idempotencyStore.SaveAsync(input.IdempotencyKey, emptyResult);

                return emptyResult;
            }

            var existingPaymentIdsResponse = await RetryAsync(() =>
                    _financeApi.GetWithResponseCode<List<string>>(
                        new GetAccountPaymentIdsRequest(input.AccountId)),
                input.CorrelationId);

            if (existingPaymentIdsResponse == null ||
                existingPaymentIdsResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"[CorrelationId: {input.CorrelationId}] Failed to retrieve existing payment IDs for account {input.AccountId}");
            }

            var existingPaymentIds =
                new HashSet<string>(existingPaymentIdsResponse.Body ?? new List<string>());

            var newPayments = allPayments
                .Where(p => !existingPaymentIds.Contains(p.PaymentId))
                .Where(p =>
                    !string.Equals(p.FundingSource,
                        "FullyFundedSfa",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!newPayments.Any())
            {
                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] No new payments to insert for account {AccountId}",
                    input.CorrelationId,
                    input.AccountId);

                var result = new RefreshPaymentDataResult
                {
                    CorrelationId = input.CorrelationId,
                    PaymentsCreated = 0,
                    PaymentDetails = allPayments
                };

                await _idempotencyStore.SaveAsync(input.IdempotencyKey, result);

                return result;
            }

            foreach (var batch in newPayments.Chunk(1000))
            {
                await RetryAsync(() =>
                        _financeApi.Post("/api/payments/staging", batch),
                    input.CorrelationId);
            }

            var finalResult = new RefreshPaymentDataResult
            {
                CorrelationId = input.CorrelationId,
                PaymentsCreated = newPayments.Count,
                PaymentDetails = allPayments
            };

            await _idempotencyStore.SaveAsync(input.IdempotencyKey, finalResult);

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Finished refreshing payments for account {AccountId}. Payments created: {Count}",
                input.CorrelationId,
                input.AccountId,
                finalResult.PaymentsCreated);

            return finalResult;
        }

        private async Task RetryAsync(Func<Task> action, string correlationId, int retries = 3)
        {
            var delay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (attempt < retries)
                {
                    _logger.LogWarning(
                        ex,
                        "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary API error, retrying...",
                        correlationId,
                        attempt);

                    await Task.Delay(delay);

                    delay = delay * 2;
                }
            }

            await action();
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> action, string correlationId, int retries = 3)
        {
            var delay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < retries)
                {
                    _logger.LogWarning(
                        ex,
                        "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary API error, retrying...",
                        correlationId,
                        attempt);

                    await Task.Delay(delay);

                    delay = delay * 2;
                }
            }

            return await action();
        }
    }
}
