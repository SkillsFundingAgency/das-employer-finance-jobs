namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class FinanceApiPayeScheme
{
    public string EmpRef { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Aorn { get; set; } = string.Empty;
}
