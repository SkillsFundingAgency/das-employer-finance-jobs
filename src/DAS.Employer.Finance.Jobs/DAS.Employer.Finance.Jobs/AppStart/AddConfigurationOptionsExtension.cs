using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.AppStart;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
    
        services.Configure<FinanceApiConfiguration>(configuration.GetSection("FinanceApiConfiguration"));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceApiConfiguration>>().Value);
       
        services.Configure<PaymentApiConfiguration>(configuration.GetSection("ProviderEventsApiConfiguration"));
        services.AddSingleton(cfg => cfg.GetService<IOptions<PaymentApiConfiguration>>().Value);
    }
}