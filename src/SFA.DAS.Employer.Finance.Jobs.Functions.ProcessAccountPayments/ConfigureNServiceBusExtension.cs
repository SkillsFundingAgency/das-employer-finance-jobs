using Microsoft.Extensions.Hosting;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments;

public static class ConfigureNServiceBusExtension
{
    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder, string endpointName)
    {
        hostBuilder.UseNServiceBus((config, endpointConfiguration) =>
        {
            endpointConfiguration.LogDiagnostics();
            endpointConfiguration.AdvancedConfiguration.EnableInstallers();
            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo($"{endpointName}-error");
            endpointConfiguration.AdvancedConfiguration.Conventions()
                .DefiningCommandsAs(IsCommand)
                .DefiningMessagesAs(IsMessage)
                .DefiningEventsAs(IsEvent);

            var license = config["NServiceBus_License"] ?? config["NServiceBusLicense"];
            if (!string.IsNullOrWhiteSpace(license))
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