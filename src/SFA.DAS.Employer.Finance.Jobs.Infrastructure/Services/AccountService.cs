using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
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
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get accounts, page {Page}, pageSize {PageSize}", request.CorrelationId, request.Page, request.PageSize);

            var response = await financeApiClient.Get<FinanceApiGetAccountsResponse>(request);

            var accounts = response?.Accounts ?? [];
            logger.LogInformation("[CorrelationId: {CorrelationId}] Finance API returned {Count} accounts for page {Page}", request.CorrelationId, accounts.Count, request.Page);

            return accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, " [CorrelationId: {CorrelationId}] Error getting accounts from Finance API for page {Page}: {ErrorMessage}", request.CorrelationId, request.Page, ex.Message);
            throw;
        }
    }
}