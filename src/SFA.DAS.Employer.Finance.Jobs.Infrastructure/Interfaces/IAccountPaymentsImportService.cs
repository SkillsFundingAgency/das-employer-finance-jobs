using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountPaymentsImportService
{
    Task<AccountPaymentsImportResult> ImportAccountPaymentsAsync(AccountPaymentsImportRequest request, CancellationToken cancellationToken);
}
