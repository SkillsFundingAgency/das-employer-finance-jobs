using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountPaymentsImportService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ILogger<AccountPaymentsImportService> logger)
    : IAccountPaymentsImportService
{
    public async Task<AccountPaymentsImportResult> ImportAccountPaymentsAsync(AccountPaymentsImportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Calling Finance API to import account payments for AccountId {AccountId}, PeriodEnd {PeriodEndRef}, IdempotencyKey {IdempotencyKey}",
                request.AccountId,
                request.PeriodEndRef,
                request.IdempotencyKey);

            var apiRequest = new ImportAccountPaymentsRequest
            {
                Payload = request
            };

            var response = await financeApiClient.Post<FinanceApiAccountPaymentsImportResponse>(apiRequest);

            return new AccountPaymentsImportResult
            {
                ImportId = response.ImportId,
                Status = response.Status,
                AcceptedAt = response.AcceptedAt
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error importing account payments for AccountId {AccountId}, PeriodEnd {PeriodEndRef}: {ErrorMessage}",
                request.AccountId,
                request.PeriodEndRef,
                ex.Message);
            throw;
        }
    }
}
