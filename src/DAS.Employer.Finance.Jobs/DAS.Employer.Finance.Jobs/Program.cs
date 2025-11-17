using Microsoft.Extensions.Hosting;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .UseNServiceBus()
    .Build();

await host.RunAsync();