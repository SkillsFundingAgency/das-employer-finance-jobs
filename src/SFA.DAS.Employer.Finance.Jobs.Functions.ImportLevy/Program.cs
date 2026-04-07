using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.ConfigurationExtensions.BuildDasConfiguration(builder))
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddDasLogging();
        SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.AddConfigurationOptionsExtension.AddConfigurationOptions(services, configuration);
        SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.ServiceRegistrationExtensions.AddServiceRegistration(services, configuration);
    })
    .Build();

await host.RunAsync();
