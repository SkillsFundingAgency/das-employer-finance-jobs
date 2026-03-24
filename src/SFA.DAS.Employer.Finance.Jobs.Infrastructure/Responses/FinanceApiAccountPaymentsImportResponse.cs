namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class FinanceApiAccountPaymentsImportResponse
{
    public Guid ImportId { get; set; }
    public string Status { get; set; }
    public DateTime AcceptedAt { get; set; }
}
