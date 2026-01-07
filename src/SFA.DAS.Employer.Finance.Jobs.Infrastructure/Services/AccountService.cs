using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, IProviderPaymentApiClient<ProviderPaymentApiConfiguration> providerPaymentApiClient, ILogger<IAccountService> logger) : IAccountService
{
    public async Task<List<string>> GetAccountsAsync(GetAccountsRequest request)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Provider Events API to create period ends", request.CorrelationId);

            var accounts = await providerPaymentApiClient.Get<List<string>>(request);

            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully created period end", request.CorrelationId);

            return accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error creating period end {ErrorMessage}", request.CorrelationId, ex.Message);
            throw;
        }
    }
}
