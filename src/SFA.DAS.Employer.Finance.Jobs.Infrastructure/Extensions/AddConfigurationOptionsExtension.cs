using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Encoding;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();

        services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<FinanceApiConfiguration>>().Value);

        services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<ProviderEventsApiConfiguration>>().Value);

        services.Configure<CommitmentsApiConfiguration>(configuration.GetSection(nameof(CommitmentsApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<CommitmentsApiConfiguration>>().Value);

        services.Configure<EmployerFinanceOuterApiConfiguration>(configuration.GetSection(nameof(EmployerFinanceOuterApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<EmployerFinanceOuterApiConfiguration>>().Value);

        services.Configure<ImportPaymentsOptions>(configuration.GetSection(nameof(ImportPaymentsOptions)));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<ImportPaymentsOptions>>().Value);

        var encodingConfig = new EncodingConfig { Encodings = [] };
        configuration.GetSection(nameof(encodingConfig.Encodings)).Bind(encodingConfig.Encodings);
        services.AddSingleton(encodingConfig);
    }
}
