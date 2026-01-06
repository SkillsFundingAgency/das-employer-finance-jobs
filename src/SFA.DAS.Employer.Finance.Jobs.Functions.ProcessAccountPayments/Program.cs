using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments")]

// Check configuration early to determine transport type
var tempConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var serviceBusConnectionString = tempConfig["Values:AzureWebJobsServiceBus"] ?? tempConfig["AzureWebJobsServiceBus"];
var useLearningTransport = string.IsNullOrWhiteSpace(serviceBusConnectionString) ||
                           serviceBusConnectionString.Equals("UseLearningTransport=true", StringComparison.OrdinalIgnoreCase);

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
     .ConfigureServices((context, services) =>
     {
         var configuration = context.Configuration;
         services.AddConfigurationOptions(configuration);
         services.AddServiceRegistration(configuration);
         services.AddApplicationInsightsTelemetryWorkerService();
         services.ConfigureFunctionsApplicationInsights();
     });

if (useLearningTransport)
{
    // Use LearningTransport - provide a dummy connection string to satisfy UseNServiceBus requirement
    // The connection string won't actually be used since we override with LearningTransport in the callback
    hostBuilder.UseNServiceBus(
        "SFA.DAS.Employer.Finance.Jobs.Functions",
        (config, endpointConfiguration) =>
        {
            // Override with LearningTransport
            var learningTransportDir = Path.Combine(
                Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("src", StringComparison.Ordinal)),
                "src", ".learningtransport");

            var transport = endpointConfiguration.AdvancedConfiguration.UseTransport<LearningTransport>();
            transport.StorageDirectory(learningTransportDir);
        });
}
else
{
    // Use Azure Service Bus with the provided connection string
    hostBuilder.UseNServiceBus((config, endpointConfiguration) =>
    {
        // Azure Service Bus will be configured automatically from AzureWebJobsServiceBus
    });
}
var host = hostBuilder

    .ConfigureServices((context, services) =>
    {
        services.AddDurableTaskClient();
        services.AddSingleton<IProcessAccountOrchestrationStarter, ProcessAccountOrchestrationStarter>();
        services.AddLogging(builder =>
        {
            builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);

            builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();

        })
         .AddApplicationInsightsTelemetryWorkerService()
         .ConfigureFunctionsApplicationInsights();
    })
    .Build();
await host.RunAsync();