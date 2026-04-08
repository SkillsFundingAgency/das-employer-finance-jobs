using Microsoft.Extensions.Hosting;
using InfrastructureConfigurationExtensions = SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.ConfigurationExtensions;
using InfrastructureConfigurationOptionsExtension = SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.AddConfigurationOptionsExtension;
using InfrastructureServiceRegistrationExtensions = SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.ServiceRegistrationExtensions;
using ImportLevyLoggingExtensions = SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Extensions.LoggingExtensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => InfrastructureConfigurationExtensions.BuildDasConfiguration(builder))
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        ImportLevyLoggingExtensions.AddDasLogging(services);
        InfrastructureConfigurationOptionsExtension.AddConfigurationOptions(services, configuration);
        InfrastructureServiceRegistrationExtensions.AddServiceRegistration(services, configuration);
    })
    .Build();

await host.RunAsync();
