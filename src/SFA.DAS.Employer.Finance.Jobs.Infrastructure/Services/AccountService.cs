using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, IProviderPaymentApiClient<ProviderPaymentApiConfiguration> providerPaymentApiClient, ILogger<IAccountService> logger) : IAccountService
{
    public async Task<List<Accounts>> GetAccountsAsync(GetAccountsRequest request)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Provider Events API to get accounts", request.CorrelationId);

            var accounts = await providerPaymentApiClient.Get<AccountsResponse>(request);

            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved accounts", request.CorrelationId);

            return accounts.Accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error getting Accounts {ErrorMessage}", request.CorrelationId, ex.Message);
            throw;
        }
    }
}
