using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.NServiceBus.Configuration;
using SFA.DAS.NServiceBus.Configuration.AzureServiceBus;
using SFA.DAS.NServiceBus.Configuration.NewtonsoftJsonSerializer;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class NServiceBusExtensions
{
    private const string EndpointName = "SFA.DAS.EmployerFinance.Jobs";

    public static void ConfigureNServiceBusForSend(this IServiceCollection services, IConfiguration configuration)
    {
        var fullyQualifiedNamespace = GetFullyQualifiedNamespace(configuration);
        var endpointConfiguration = new EndpointConfiguration(EndpointName);

        endpointConfiguration.UseNewtonsoftJsonSerializer();
        endpointConfiguration.UseSendOnly();
        endpointConfiguration.UseMessageConventions();
        endpointConfiguration.UseAzureServiceBusTransport(fullyQualifiedNamespace, _ => { });

        var endpointInstance = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
        services.AddSingleton<IEndpointInstance>(endpointInstance);
        services.AddSingleton<IMessageSession>(endpointInstance);
        services.AddHostedService<NServiceBusEndpointHostedService>();
    }

    public static string GetFullyQualifiedNamespace(IConfiguration configuration)
    {
        var fullyQualifiedNamespace =
            configuration["AzureWebJobsServiceBus:fullyQualifiedNamespace"]
            ?? configuration["AzureWebJobsServiceBus__fullyQualifiedNamespace"];

        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            return fullyQualifiedNamespace;
        }

        var serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            return serviceBusConnectionString.GetFullyQualifiedNamespace();
        }

        throw new InvalidOperationException("No Azure Service Bus fully qualified namespace or connection string has been configured for NServiceBus.");
    }

    public static string GetFullyQualifiedNamespace(this string serviceBusConnectionString)
    {
        if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            throw new ArgumentException("Service Bus connection string cannot be null or empty.", nameof(serviceBusConnectionString));
        }

        var parts = serviceBusConnectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = part.Split('=')[1];
                return new Uri(endpoint).Host;
            }
        }

        throw new FormatException("Invalid Service Bus connection string: Fully Qualified Namespace not found.");
    }

}
