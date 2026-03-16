using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddServiceRegistration(configuration);
    })    
    .Build();
     await host.RunAsync();
