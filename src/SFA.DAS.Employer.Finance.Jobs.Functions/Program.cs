using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;


[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
     .ConfigureServices((context, services) =>
     {
         var configuration = context.Configuration;
         services.AddConfigurationOptions(configuration);
         services.AddServiceRegistration(configuration);
         services.AddApplicationInsightsTelemetryWorkerService();
         services.ConfigureFunctionsApplicationInsights();

         services
       .AddLogging(builder =>
       {
           builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
           builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);

           builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
           builder.SetMinimumLevel(LogLevel.Trace);
           builder.AddConsole();

       });
     })
    .UseNServiceBus((config, endpointConfiguration) =>
     {
          endpointConfiguration.AdvancedConfiguration.EnableInstallers();
     })    
    .Build();
     await host.RunAsync();