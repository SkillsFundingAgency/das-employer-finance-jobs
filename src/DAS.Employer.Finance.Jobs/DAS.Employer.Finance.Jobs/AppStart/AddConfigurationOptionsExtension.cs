using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.AppStart;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
    
        services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceApiConfiguration>>().Value);
       
        services.Configure<ProviderPaymentApiConfiguration>(configuration.GetSection(nameof(ProviderPaymentApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderPaymentApiConfiguration>>().Value);
    }
}