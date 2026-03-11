using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountPaymentsImportService(
    ILogger<AccountPaymentsImportService> logger)
    : IAccountPaymentsImportService
{
    public async Task<AccountPaymentsImportResult> ImportAccountPaymentsAsync(AccountPaymentsImportRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ImportAccountPaymentsAsync stubbed. AccountId {AccountId}, PeriodEnd {PeriodEndRef}, IdempotencyKey {IdempotencyKey}",
            request.AccountId,
            request.PeriodEndRef,
            request.IdempotencyKey);

        await Task.CompletedTask;

        return new AccountPaymentsImportResult
        {
            ImportId = Guid.NewGuid(),
            Status = "Stubbed",
            AcceptedAt = DateTime.UtcNow
        };
    }
}
