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

    public async Task<List<PayeScheme>> GetPayeSchemesAsync(GetAccountPayeSchemesRequest request)
    {
        try
        {
            logger.LogInformation(
                "Calling Finance API to get PAYE schemes for account {AccountId} from source {Source}",
                request.AccountId,
                request.Source);

            var response = await financeApiClient.Get<FinanceApiGetPayeSchemesResponse>(request);
            var payeSchemes = response?.Schemes?
                .Where(scheme => !string.IsNullOrWhiteSpace(scheme.EmpRef))
                .Select(scheme => new PayeScheme
                {
                    Reference = scheme.EmpRef,
                    Name = scheme.Name ?? string.Empty
                })
                .ToList() ?? [];

            logger.LogInformation(
                "Finance API returned {Count} PAYE schemes for account {AccountId}",
                payeSchemes.Count,
                request.AccountId);

            return payeSchemes;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error getting PAYE schemes from Finance API for account {AccountId}: {ErrorMessage}",
                request.AccountId,
                ex.Message);
            throw new InvalidOperationException(
                $"Failed to get PAYE schemes for account {request.AccountId}.",
                ex);
        }
    }
}
