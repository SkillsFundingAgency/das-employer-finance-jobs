using HMRC.ESFA.Levy.Api.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.ActiveDirectory;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;
using SFA.DAS.TokenService.Api.Client;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    public static void AddServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {        

        services.AddHttpClient();
     
        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();

        services.AddTransient(typeof(IInternalApiClient<>), typeof(InternalApiClient<>));

        services.AddTransient<IProviderPaymentApiClient<ProviderEventsApiConfiguration>, ProviderPaymentApiClient>();

        services.AddTransient<IFinanceApiClient<FinanceApiConfiguration>, FinanceApiClient>();

        services.AddScoped<IPeriodEndService, PeriodEndService>();

        services.AddScoped<IAccountService, AccountService>();

        services.AddScoped<IAccountPaymentsImportService, AccountPaymentsImportService>();

        services.AddHmrcServices(configuration);
    }
    private static IServiceCollection AddHmrcServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HmrcConfiguration>(configuration.GetSection(ConfigurationKeys.Hmrc));
        services.AddSingleton<IHmrcConfiguration>(cfg => cfg.GetService<IOptions<HmrcConfiguration>>()!.Value);

        services.AddTransient<IApprenticeshipLevyApiClient>(serviceProvider =>
        {
            var client = new HttpClient { BaseAddress = new Uri(serviceProvider.GetService<IHmrcConfiguration>()!.BaseUrl) };
            return new ApprenticeshipLevyApiClient(client);
        });

        //Note: in configuration section the related services name must exist....
        services.Configure<TokenServiceApiClientConfiguration>(configuration.GetSection(ConfigurationKeys.TokenServiceApi));
        services.AddSingleton<ITokenServiceApiClientConfiguration>(cfg => cfg.GetService<IOptions<TokenServiceApiClientConfiguration>>()!.Value);
        services.AddSingleton<ITokenServiceApiClient>(_ => new TokenServiceApiClient(_.GetService<ITokenServiceApiClientConfiguration>()));

        services.AddSingleton<IHmrcService, HmrcService>();

        services.AddSingleton<IAzureAdAuthenticationService, AzureAdAuthenticationService>();

        services.AddOptions<LevyImportResilienceOptions>().Bind(configuration.GetSection(LevyImportResilienceOptions.SectionName));
        services.AddSingleton<IHmrcRateLimiter>(serviceProvider =>
        {
            var resilience = serviceProvider.GetRequiredService<IOptions<LevyImportResilienceOptions>>().Value;
            return new SlidingWindowHmrcRateLimiter(
                resilience.MaxRequestsPerWindow,
                TimeSpan.FromSeconds(resilience.WindowSeconds));
        });

        return services;
    }
}
