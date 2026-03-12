using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder.BuildDasConfiguration())
  
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        
        services.ConfigureFunctionsApplicationInsights();

       
        services.AddDasLogging();
        services.AddDasDataProtection(configuration);
        services.AddConfigurationOptions(configuration);
        services.AddServiceRegistration(configuration);
    })
    .Build();