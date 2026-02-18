using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Messages.Commands;


[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.Functions", routing =>
    {
        routing.RouteToEndpoint(typeof(ImportAccountPaymentsCommand), "SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments");
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration);
    })    
    .Build();
     await host.RunAsync();