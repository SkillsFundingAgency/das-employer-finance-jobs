namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PaymentApiConfiguration : IApiConfiguration
{
    public string Url { get; set; }
    public string Identifier { get; set; }
}