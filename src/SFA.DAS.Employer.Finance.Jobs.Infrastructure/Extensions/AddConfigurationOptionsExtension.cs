using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
    
        services.Configure<FinanceInnerApiConfiguration>(configuration.GetSection(nameof(FinanceInnerApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceInnerApiConfiguration>>().Value);
       
        services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderEventsApiConfiguration>>().Value);
    }
}