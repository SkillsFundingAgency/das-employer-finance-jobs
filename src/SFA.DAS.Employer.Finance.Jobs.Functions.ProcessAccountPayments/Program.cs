using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.PAP")]

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.PAP")
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
#if DEBUG
            options.DeveloperMode = true;
#endif
        });
        services.ConfigureFunctionsApplicationInsights();
        
        // DurableTask services removed temporarily to focus on NServiceBus
        // services.AddDurableTaskClient(builder => builder.UseGrpc());
        // services.AddSingleton<IProcessAccountOrchestrationStarter, ProcessAccountOrchestrationStarter>();
        
        // Explicitly register the message handler
        services.AddTransient<ImportAccountPaymentsCommandHandler>();
        
        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration);
    })    
    .Build();

await host.RunAsync();

