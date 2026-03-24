using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddDasLogging();
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration);
    })
    .Build();

await host.RunAsync();
