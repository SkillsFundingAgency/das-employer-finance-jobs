using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.PAP")]

Console.WriteLine("=== ProcessAccountPayments Function App Starting ===");

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

Console.WriteLine("=== ProcessAccountPayments Function App Built Successfully ===");
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ProcessAccountPayments Function App starting up...");

await host.RunAsync();

