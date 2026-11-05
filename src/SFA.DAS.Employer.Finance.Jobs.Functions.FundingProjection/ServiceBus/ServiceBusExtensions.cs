using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.ServiceBus;

[ExcludeFromCodeCoverage]
public static class ServiceBusExtensions
{
    public static IServiceCollection AddServiceBusQueueInitialiser(this IServiceCollection services)
    {
        services.AddSingleton<ServiceBusAdministrationClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var fqdn = config[$"{QueueConstants.ServiceBusConnection}:fullyQualifiedNamespace"];
            if (fqdn != null)
                return new ServiceBusAdministrationClient(fqdn, new DefaultAzureCredential());
            return new ServiceBusAdministrationClient(config[QueueConstants.ServiceBusConnection]!);
        });

        services.AddHostedService<ServiceBusQueueInitialiser>();

        return services;
    }
}
