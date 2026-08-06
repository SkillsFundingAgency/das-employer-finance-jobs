using System.Diagnostics.CodeAnalysis;
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
using SFA.DAS.Encoding;
using SFA.DAS.TokenService.Api.Client;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    public static void AddServiceRegistration(this IServiceCollection services, IConfiguration configuration, string nServiceBusEndpointName)
    {
        services.AddHttpClient();

        services.ConfigureNServiceBusForSend(configuration, nServiceBusEndpointName);

        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();
        services.AddSingleton<IHmrcClock, HmrcClock>();
        services.AddSingleton<IHmrcTokenProvider, HmrcTokenProvider>();
        services.AddSingleton<IEnglishFractionCalculationDateWriteTracker, EnglishFractionCalculationDateWriteTracker>();

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
        services.AddScoped<IEnglishFractionCalculationDatePersistenceService, EnglishFractionCalculationDatePersistenceService>();
        services.AddScoped<IAccountPaymentsImportService, AccountPaymentsImportService>();
        services.AddScoped<IAccountTransfersService, AccountTransfersService>();
        services.AddScoped<IRefreshPaymentDataService, RefreshPaymentDataService>();
        services.AddSingleton<IRefreshPaymentDataCompletedEventPublisher, RefreshPaymentDataCompletedEventPublisher>();
        services.AddScoped<IPaymentTransactionLinesService, PaymentTransactionLinesService>();
        services.AddScoped<ITransferStagedToOperationalService, TransferStagedToOperationalService>();
        services.AddScoped<ICommitmentsApiClient, CommitmentsApiClient>();
        services.AddScoped<ICoursesApiClient, CoursesApiClient>();
        services.AddScoped<IRoatpApiClient, RoatpApiClient>();
        services.AddScoped<IPaymentMetadataService, PaymentMetadataService>();
        services.AddScoped<IEncodingService, EncodingService>();

        services.AddLevyImportHmrcServices(configuration);
    }

    private static IServiceCollection AddLevyImportHmrcServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IHmrcConfiguration>(provider => provider.GetRequiredService<HmrcConfiguration>());

        services.Configure<TokenServiceApiClientConfiguration>(configuration.GetSection(ConfigurationKeys.TokenServiceApi));
        services.AddSingleton<ITokenServiceApiClientConfiguration>(provider =>
            provider.GetRequiredService<IOptions<TokenServiceApiClientConfiguration>>().Value);
        services.AddSingleton<ITokenServiceApiClient>(provider =>
            new TokenServiceApiClient(provider.GetRequiredService<ITokenServiceApiClientConfiguration>()));

        services.AddSingleton<IHmrcService, HmrcService>();
        services.AddSingleton<IAzureAdAuthenticationService, AzureAdAuthenticationService>();

        services.AddOptions<LevyImportResilienceOptions>()
            .Bind(configuration.GetSection(LevyImportResilienceOptions.SectionName));
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
