using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class HmrcConfiguration: IHmrcConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ServerToken { get; set; } = string.Empty;
    public string OgdSecret { get; set; } = string.Empty;
    public string OgdClientId { get; set; } = string.Empty;
    public string AzureClientId { get; set; } = string.Empty;
    public string AzureAppKey { get; set; } = string.Empty;
    public string AzureResourceId { get; set; } = string.Empty;
    public string AzureTenant { get; set; } = string.Empty;
    public bool UseHiDataFeed { get; set; }
}
