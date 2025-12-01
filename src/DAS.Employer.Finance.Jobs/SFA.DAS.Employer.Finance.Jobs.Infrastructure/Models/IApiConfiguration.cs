namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public interface IApiConfiguration
{
    string Url { get; set; }
    string Identifier { get; set; }
}