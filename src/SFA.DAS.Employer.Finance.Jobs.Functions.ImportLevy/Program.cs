using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddDasLogging();
        SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.AddConfigurationOptionsExtension.AddConfigurationOptions(services, configuration);
        SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions.ServiceRegistrationExtensions.AddServiceRegistration(services, configuration);
        services.AddScoped<ILevyDeclarationNormalizer, LevyDeclarationNormalizer>();
        services.AddScoped<IRetryDelay, RetryDelay>();
        services.AddScoped<IRetryService, RetryService>();
    })
    .Build();

await host.RunAsync();
