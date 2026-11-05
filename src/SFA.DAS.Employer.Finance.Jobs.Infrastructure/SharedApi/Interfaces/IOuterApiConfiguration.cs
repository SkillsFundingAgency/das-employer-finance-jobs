namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

public interface IOuterApiConfiguration
{
    string Key { get; set; }
    string BaseUrl { get; set; }
}