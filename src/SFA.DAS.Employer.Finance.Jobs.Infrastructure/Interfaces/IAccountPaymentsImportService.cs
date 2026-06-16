using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountPaymentsImportService
{
    Task<AccountPaymentsImportResult> ImportAccountPaymentsAsync(AccountPaymentsImportInput input, CancellationToken cancellationToken);
    Task<AccountExistingPaymentIdsImportResult> ImportAccountExistingPaymentIdsAsync(long accountId, string correlationId);
}
