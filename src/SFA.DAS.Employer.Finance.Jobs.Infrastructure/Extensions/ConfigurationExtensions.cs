using Microsoft.Extensions.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class ConfigurationExtensions
{
    const string EncodingConfigKey = "SFA.DAS.Encoding";

    public static IConfiguration BuildDasConfiguration(this IConfigurationBuilder configBuilder)
    {
        configBuilder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables();

        configBuilder.AddJsonFile("local.settings.json", optional: true);

        var config = configBuilder.Build();

        configBuilder.AddAzureTableStorage(options =>
        {
#if DEBUG
            options.ConfigurationKeys = config["Values:ConfigNames"].Split(",");
            options.StorageConnectionString = config["Values:ConfigurationStorageConnectionString"];
            options.EnvironmentName = config["Values:EnvironmentName"];
#else
        options.ConfigurationKeys = config["ConfigNames"].Split(",");
        options.StorageConnectionString = config["ConfigurationStorageConnectionString"];
        options.EnvironmentName = config["EnvironmentName"];
#endif
            options.PreFixConfigurationKeys = false;
            options.ConfigurationKeysRawJsonResult = [EncodingConfigKey];
        });

        return configBuilder.Build();
    }

    public static string NServiceBusConnectionString(this IConfiguration config) => config["NServiceBusConnectionString"] ?? "UseLearningEndpoint=true";
    public static string NServiceBusLicense(this IConfiguration config) => config["NServiceBusLicense"];
    public static string NServiceBusFullNamespace(this IConfiguration config) => config["AzureWebJobsServiceBus:fullyQualifiedNamespace"];
    public static string NServiceBusSASConnectionString(this IConfiguration config) => config["AzureWebJobsServiceBus"];

}