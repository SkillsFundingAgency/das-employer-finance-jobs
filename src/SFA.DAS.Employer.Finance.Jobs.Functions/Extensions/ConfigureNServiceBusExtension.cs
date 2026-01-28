using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Extensions;

public static class ConfigureNServiceBusExtension
{
    private const string EndpointName = "SFA.DAS.Employer.Finance.Jobs.Functions";
    private const string ErrorEndpointName = $"{EndpointName}-error";

    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseNServiceBus((config, endpointConfiguration) =>
        {
            endpointConfiguration.Transport.SubscriptionRuleNamingConvention = AzureRuleNameShortener.Shorten;
            
            endpointConfiguration.AdvancedConfiguration.EnableInstallers();
            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo(ErrorEndpointName);
            endpointConfiguration.AdvancedConfiguration.Conventions()
                .DefiningCommandsAs(IsCommand)
                .DefiningMessagesAs(IsMessage)
                .DefiningEventsAs(IsEvent);

            var license = config["NServiceBus_License"];
            if (!string.IsNullOrEmpty(license))
            {
                var decodedLicence = WebUtility.HtmlDecode(license);
                endpointConfiguration.AdvancedConfiguration.License(decodedLicence);
            }

#if DEBUG
            var transport = endpointConfiguration.AdvancedConfiguration.UseTransport<LearningTransport>();
            transport.StorageDirectory(Path.Combine(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("src")),
                @"src\.learningtransport"));
#endif
        });

        return hostBuilder;
    }

    private static bool IsMessage(Type t) => t is IMessage || IsDasMessage(t, "Messages");

    private static bool IsEvent(Type t) => t is IEvent || IsDasMessage(t, "Messages.Events");

    private static bool IsCommand(Type t) => t is ICommand || IsDasMessage(t, "Messages.Commands");

    private static bool IsDasMessage(Type t, string namespaceSuffix)
        => t.Namespace != null &&
           t.Namespace.StartsWith("SFA.DAS") &&
           t.Namespace.EndsWith(namespaceSuffix);
}

internal static class AzureRuleNameShortener
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
