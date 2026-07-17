using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class NServiceBusExtensions
{
    public const string PaymentsEndpointName = "SFA.DAS.EmployerFinance.Jobs.Payments";
    public const string LevyEndpointName = "SFA.DAS.EmployerFinance.Jobs.Levy";

    public static void ConfigureNServiceBusForSend(this IServiceCollection services, IConfiguration configuration, string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
        endpointConfiguration.SendOnly();
        endpointConfiguration.SendFailedMessagesTo($"{endpointName}-errors");
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.Conventions().SetMessageConventions();
        endpointConfiguration.UseTransport(BuildTransport(configuration));

        var nServiceBusLicense = GetLicense(configuration);
        if (!string.IsNullOrWhiteSpace(nServiceBusLicense))
        {
            endpointConfiguration.License(nServiceBusLicense);
        }

        services.AddNServiceBusEndpoint(endpointConfiguration);
    }

    public static AzureServiceBusTransport BuildTransport(IConfiguration configuration)
    {
        var serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString) &&
            serviceBusConnectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            return new AzureServiceBusTransport(serviceBusConnectionString, TopicTopology.Default);
        }

        return new AzureServiceBusTransport(GetFullyQualifiedNamespace(configuration), new DefaultAzureCredential(), TopicTopology.Default);
    }

    public static string GetLicense(IConfiguration configuration) =>
        configuration["NServiceBusLicense"]
        ?? configuration["NServiceBus:License"]
        ?? configuration["EmployerFinanceJobsConfiguration:NServiceBusLicense"]
        ?? string.Empty;

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
            if (!serviceBusConnectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                return serviceBusConnectionString;
            }

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

    public static void SetMessageConventions(this ConventionsBuilder conventions)
    {
        conventions.DefiningMessagesAs(IsMessage);
        conventions.DefiningEventsAs(IsEvent);
        conventions.DefiningCommandsAs(IsCommand);
    }

    private static bool IsMessage(Type type) => IsSfaMessage(type, "Messages") || type.Name.EndsWith("Message");

    private static bool IsEvent(Type type) =>
        IsSfaMessage(type, "Messages.Events") || type.Name.EndsWith("Event");

    private static bool IsCommand(Type type) =>
        IsSfaMessage(type, "Messages.Commands") || type.Name.EndsWith("Command");

    private static bool IsSfaMessage(Type type, string namespaceSuffix) =>
        type.Namespace != null &&
        type.Namespace.StartsWith("SFA.DAS") &&
        type.Namespace.EndsWith(namespaceSuffix);
}
