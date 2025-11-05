using Microsoft.Extensions.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs;

public static class ConfigurationExtensions
{  
    public static IConfiguration BuildDasConfiguration(this IConfigurationBuilder configBuilder)
    {
        var config = configBuilder.Build();    
        return configBuilder.Build();
    }
    public static string NServiceBusConnectionString(this IConfiguration config) => config["NServiceBusConnectionString"] ?? "UseLearningEndpoint=true";
    public static string NServiceBusLicense(this IConfiguration config) => config["NServiceBusLicense"];
    public static string NServiceBusFullNamespace(this IConfiguration config) => config["AzureWebJobsServiceBus:fullyQualifiedNamespace"];
    public static string NServiceBusSASConnectionString(this IConfiguration config) => config["AzureWebJobsServiceBus"];

}