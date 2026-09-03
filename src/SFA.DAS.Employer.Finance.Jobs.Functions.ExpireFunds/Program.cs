using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
#if DEBUG
            options.DeveloperMode = true;
#endif
        });

        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddExpireFundsServiceRegistration(configuration);
    })
    .Build();

await host.RunAsync();
