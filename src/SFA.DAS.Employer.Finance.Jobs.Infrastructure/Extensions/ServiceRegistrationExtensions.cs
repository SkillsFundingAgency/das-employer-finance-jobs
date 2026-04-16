using System.Diagnostics.CodeAnalysis;
using HMRC.ESFA.Levy.Api.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    public static void AddServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();
        services.AddSingleton<IHmrcClock, HmrcClock>();
        services.AddSingleton<IHmrcRequestThrottle, HmrcRequestThrottle>();
        services.AddSingleton<IHmrcTokenProvider, HmrcTokenProvider>();

        services.AddSingleton<IApprenticeshipLevyApiClient>(provider =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(provider.GetRequiredService<HmrcConfiguration>().BaseUrl)
            };

            return new ApprenticeshipLevyApiClient(client);
        });

        services.AddSingleton<IHmrcClient, HmrcClient>();

        services.AddTransient(typeof(IInternalApiClient<>), typeof(InternalApiClient<>));
        services.AddTransient<IProviderPaymentApiClient<ProviderEventsApiConfiguration>, ProviderPaymentApiClient>();
        services.AddTransient<IFinanceApiClient<FinanceApiConfiguration>, FinanceApiClient>();

        services.AddScoped<IPeriodEndService, PeriodEndService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEnglishFractionsService, EnglishFractionsService>();
        services.AddScoped<IEnglishFractionsPersistenceService, EnglishFractionsPersistenceService>();
        services.AddScoped<IAccountPaymentsImportService, AccountPaymentsImportService>();
    }
}
