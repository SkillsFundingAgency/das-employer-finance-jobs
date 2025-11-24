using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

namespace SFA.DAS.Employer.Finance.Jobs.AppStart;

public static class ServiceRegistrationExtensions
{
    public static void AddServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {        

        services.AddHttpClient();       

        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();        

        services.AddScoped<IFinanceApiClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            var credentialHelper = sp.GetRequiredService<IAzureClientCredentialHelper>();

            var config = configuration.GetSection("FinanceApiConfiguration").Get<FinanceApiConfiguration>();

            var logger = sp.GetRequiredService<ILogger<FinanceApiClient>>();

            return new FinanceApiClient(httpClientFactory, credentialHelper, config, logger);

        });       

        services.AddScoped<IProviderEventsApiClient>(sp =>
        {
            
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            var credentialHelper = sp.GetRequiredService<IAzureClientCredentialHelper>();

            var config = configuration.GetSection("ProviderEventsApiConfiguration").Get<ProviderEventsApiConfiguration>();

            var logger = sp.GetRequiredService<ILogger<ProviderEventsApiClient>>();

            return new ProviderEventsApiClient(httpClientFactory, credentialHelper, config, logger);

        });    

        services.AddScoped<IPeriodEndService, PeriodEndService>();
        
    }
}
