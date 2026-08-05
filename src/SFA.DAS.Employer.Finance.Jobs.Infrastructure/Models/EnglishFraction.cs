namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class EnglishFraction
{
    public string EmployerReference { get; set; } = string.Empty;
    public DateTime DateCalculated { get; set; }
    public decimal Amount { get; set; }
}
