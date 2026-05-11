using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();

        services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceApiConfiguration>>().Value);

        services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderEventsApiConfiguration>>().Value);

        services.Configure<HmrcConfiguration>(configuration.GetSection("Hmrc"));
        services.AddSingleton(cfg => cfg.GetService<IOptions<HmrcConfiguration>>().Value);
    }
}
