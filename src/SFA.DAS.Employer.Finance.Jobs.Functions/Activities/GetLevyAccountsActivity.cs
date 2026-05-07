using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities
{
    public class GetLevyAccountsActivity
    {
        private readonly IFinanceApiClient<FinanceApiConfiguration> _financeApi;
        private readonly ILogger<GetLevyAccountsActivity> _logger;

        public GetLevyAccountsActivity(
            IFinanceApiClient<FinanceApiConfiguration> financeApi,
            ILogger<GetLevyAccountsActivity> logger)
        {
            _financeApi = financeApi;
            _logger = logger;
        }

        [Function("GetLevyAccountsActivity")]
        public async Task<List<long>> Run([ActivityTrigger] string correlationId)
        {
            _logger.LogInformation("[CorrelationId: {CorrelationId}] Retrieving levy accounts", correlationId);

            try
            {
                var response = await RetryAsync(
                    () => _financeApi.GetWithResponseCode<List<long>>(new GetLevyAccountsRequest()),
                    correlationId);

                if (response == null || response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning(
                        "[CorrelationId: {CorrelationId}] No accounts returned or API failure",
                        correlationId);

                    return new List<long>();
                }

                var accounts = response.Body ?? new List<long>();
                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Retrieved {Count} levy accounts",
                    correlationId,
                    accounts.Count);

                return accounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CorrelationId: {CorrelationId}] Error retrieving levy accounts: {Message}",
                    correlationId,
                    ex.Message);

                throw;
            }
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
                catch (Exception ex) when (attempt < retries)
                {
                    _logger.LogWarning(
                        ex,
                        "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error calling Finance API, retrying...",
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