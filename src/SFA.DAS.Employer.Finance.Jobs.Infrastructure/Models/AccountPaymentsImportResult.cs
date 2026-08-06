using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class AccountPaymentsImportResult
{
    public List<Payment> Payments { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}
