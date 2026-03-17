namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class AccountPaymentsImportResult
{
    public Guid ImportId { get; set; }
    public string Status { get; set; }
    public DateTime AcceptedAt { get; set; }
}
