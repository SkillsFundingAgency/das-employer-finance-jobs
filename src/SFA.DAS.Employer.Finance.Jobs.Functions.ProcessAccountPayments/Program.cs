using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.StartupExtensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddDurableTaskClient();
    })
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments")
    .ConfigureServices((context, services) =>
    {
        // services.AddLearnerDataServices(context.Configuration);
        services.AddDasLogging();

        //services
        //    .AddApplicationInsightsTelemetryWorkerService()
        //    .ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();

var builder = FunctionsApplication.CreateBuilder(args);


builder.ConfigureFunctionsWebApplication();

builder.Build().Run();
