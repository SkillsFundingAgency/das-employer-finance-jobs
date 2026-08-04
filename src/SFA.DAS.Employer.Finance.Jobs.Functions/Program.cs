using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Setup Application Insights (following das-recruit-jobs pattern)
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
#if DEBUG
            options.DeveloperMode = true;
#endif
        });

        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration, NServiceBusExtensions.PaymentsEndpointName);
    })
    .Build();
await host.RunAsync();
