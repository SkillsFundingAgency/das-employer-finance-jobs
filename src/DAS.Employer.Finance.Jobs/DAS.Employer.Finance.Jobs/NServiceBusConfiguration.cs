using Microsoft.Extensions.Hosting;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs;

public static class ConfigureNServiceBusExtension
{
    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder, string endpointName)
    {
        hostBuilder.UseNServiceBus((config, endpointConfiguration) =>
        {
            
            endpointConfiguration.AdvancedConfiguration.EnableInstallers();
            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo($"{endpointName}-error");             

            var decodedLicence = WebUtility.HtmlDecode(config["NServiceBusLicense"]);
            if (!string.IsNullOrWhiteSpace(decodedLicence)) endpointConfiguration.AdvancedConfiguration.License(decodedLicence);
        });

        return hostBuilder;
    }
}