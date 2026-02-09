using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments")]

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
     .ConfigureServices((context, services) =>
     {
         var configuration = context.Configuration;       
         services.AddDurableTaskClient(builder => builder.UseGrpc());
         services.AddSingleton<IProcessAccountOrchestrationStarter, ProcessAccountOrchestrationStarter>();
         services.AddConfigurationOptions(configuration);
         services.AddServiceRegistration(configuration);
         services.AddApplicationInsightsTelemetryWorkerService();
         services.ConfigureFunctionsApplicationInsights();
         services.AddLogging(builder =>
         {
             builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
             builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
             builder.AddFilter<ApplicationInsightsLoggerProvider>("NServiceBus", LogLevel.Debug);
             builder.AddFilter("NServiceBus", LogLevel.Debug);
             builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
             builder.SetMinimumLevel(LogLevel.Trace);
             builder.AddConsole();
         })
         .AddApplicationInsightsTelemetryWorkerService()
         .ConfigureFunctionsApplicationInsights();
     })
    .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments");

    var host = hostBuilder.Build();
    await host.RunAsync();

