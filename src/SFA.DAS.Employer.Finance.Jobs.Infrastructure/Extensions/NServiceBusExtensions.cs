using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class NServiceBusExtensions
{
    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder, string endpointName, Action<RoutingSettings>? configureRouting = null)
    {
        Console.WriteLine($"=== Configuring NServiceBus for endpoint: {endpointName} ===");
        
        hostBuilder.UseNServiceBus(endpointName, (config, endpointConfiguration) =>
        {
            Console.WriteLine($"=== NServiceBus Configuration Callback Started for: {endpointName} ===");
            endpointConfiguration.LogDiagnostics();
            endpointConfiguration.Transport.SubscriptionRuleNamingConvention = AzureRuleNameShortener.Shorten;

            endpointConfiguration.AdvancedConfiguration.EnableInstallers();
            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo($"{endpointName}-error");
            endpointConfiguration.AdvancedConfiguration.UseMessageConventions();

            // Configure routing if provided
            configureRouting?.Invoke(endpointConfiguration.Routing);

            var license = config["NServiceBus_License"];
            if (!string.IsNullOrEmpty(license))
            {
                var decodedLicence = WebUtility.HtmlDecode(license);
                endpointConfiguration.AdvancedConfiguration.License(decodedLicence);
            }

#if DEBUG
            Console.WriteLine("=== Using LearningTransport for DEBUG ===");
            var transport = endpointConfiguration.AdvancedConfiguration.UseTransport<LearningTransport>();
            transport.StorageDirectory(Path.Combine(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("src")),
                @"src\.learningtransport"));
#endif
            Console.WriteLine($"=== NServiceBus Configuration Completed for: {endpointName} ===");
        });

        Console.WriteLine($"=== NServiceBus Configuration Method Completed for: {endpointName} ===");
        return hostBuilder;
    }
}

public static class EndpointConfigurationExtensions
{
    public static EndpointConfiguration UseMessageConventions(this EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Conventions()
            .DefiningMessagesAs(IsMessage)
            .DefiningEventsAs(IsEvent)
            .DefiningCommandsAs(IsCommand);

        return endpointConfiguration;
    }

    public static bool IsMessage(Type t) => IsDasMessage(t, "Messages");

    public static bool IsEvent(Type t) => (t.FullName != null && t.FullName.EndsWith("Event")) || IsDasMessage(t, "Messages.Events");

    public static bool IsCommand(Type t) => (t.FullName != null && t.FullName.EndsWith("Command")) || IsDasMessage(t, "Messages.Commands");

    public static bool IsDasMessage(Type t, string namespaceSuffix)
        => t.Namespace != null &&
           t.Namespace.StartsWith("SFA.DAS") &&
           t.Namespace.EndsWith(namespaceSuffix);
}

public static class AzureRuleNameShortener
{
    private const int AzureServiceBusRuleNameMaxLength = 50;

    public static string Shorten(Type type)
    {
        var ruleName = type.FullName;
        if (ruleName!.Length <= AzureServiceBusRuleNameMaxLength)
        {
            return ruleName;
        }

        var bytes = System.Text.Encoding.Default.GetBytes(ruleName);
        var hash = MD5.HashData(bytes);
        return new Guid(hash).ToString();
    }
}