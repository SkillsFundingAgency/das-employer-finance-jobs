using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.PAP")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.PAP")
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        services.AddDurableTaskClient(builder => builder.UseGrpc());
        services.AddSingleton<IProcessAccountOrchestrationStarter, ProcessAccountOrchestrationStarter>();
        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration);
    })    
    .Build();
await host.RunAsync();

