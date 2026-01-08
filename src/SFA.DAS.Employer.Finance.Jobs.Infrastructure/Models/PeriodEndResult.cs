namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PeriodEndResult
{
    public string PeriodEndId { get; set; }
    public int TotalAccountsRetrieved { get; set; }
    public List<Accounts> Accounts { get; set; }
}
