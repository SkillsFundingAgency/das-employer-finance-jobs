using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, ILogger<IAccountService> logger) : IAccountService
{
    public async Task<List<Accounts>> GetAccountsAsync(GetAccountsRequest request)
    {
        try
        {
            logger.LogInformation("Calling Finance API to get accounts, page {Page}, pageSize {PageSize}", request.Page, request.PageSize);

            var response = await financeApiClient.Get<FinanceApiGetAccountsResponse>(request);

            var accounts = response?.Accounts ?? [];
            logger.LogInformation("Finance API returned {Count} accounts for page {Page}", accounts.Count, request.Page);

            return accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting accounts from Finance API for page {Page}: {ErrorMessage}", request.Page, ex.Message);
            throw;
        }
    }
}
