using System;
using System.Collections.Generic;
using System.Net.Http;
using HMRC.ESFA.Levy.Api.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NServiceBus;
using NUnit.Framework;
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

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests;

public class WhenAddingServicesToTheContainer
{
    [TestCase(typeof(IAzureClientCredentialHelper))]
    [TestCase(typeof(IInternalApiClient<FinanceApiConfiguration>))]
    [TestCase(typeof(IProviderPaymentApiClient<ProviderEventsApiConfiguration>))]
    [TestCase(typeof(IFinanceApiClient<FinanceApiConfiguration>))]
    [TestCase(typeof(IApprenticeshipLevyApiClient))]
    [TestCase(typeof(IHmrcClient))]
    [TestCase(typeof(IHmrcRateLimiter))]
    [TestCase(typeof(IHmrcTokenProvider))]
    [TestCase(typeof(IEnglishFractionCalculationDateWriteTracker))]
    [TestCase(typeof(IPeriodEndService))]
    [TestCase(typeof(IEnglishFractionsService))]
    [TestCase(typeof(IEnglishFractionsPersistenceService))]
    [TestCase(typeof(IEnglishFractionCalculationDatePersistenceService))]
    [TestCase(typeof(IExpireFundsService))]
    [TestCase(typeof(IAccountService))]
    [TestCase(typeof(IAccountPaymentsImportService))]
    [TestCase(typeof(IRefreshPaymentDataCompletedEventPublisher))]
    [TestCase(typeof(IAccountTransfersService))]
    [TestCase(typeof(ITransferStagedToOperationalService))]
    public void Then_The_Dependencies_Are_Correctly_Resolved_For_Services(Type toResolve)
    {
        var serviceCollection = new ServiceCollection();
        SetupServiceCollection(serviceCollection);
        var provider = serviceCollection.BuildServiceProvider();

        var type = provider.GetService(toResolve);
        type.Should().NotBeNull();
    }

    private static void SetupServiceCollection(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddOptions();

        var configuration = GenerateConfiguration();
        services.AddSingleton<IConfiguration>(configuration);

        services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<FinanceApiConfiguration>>().Value);

        services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ProviderEventsApiConfiguration>>().Value);

        services.Configure<ImportPaymentsOptions>(configuration.GetSection(nameof(ImportPaymentsOptions)));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ImportPaymentsOptions>>().Value);

        services.Configure<HmrcConfiguration>(configuration.GetSection("Hmrc"));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<HmrcConfiguration>>().Value);
        services.Configure<LevyImportResilienceOptions>(configuration.GetSection(LevyImportResilienceOptions.SectionName));
        services.Configure<ImportLevyProcessingOptions>(configuration.GetSection(ImportLevyProcessingOptions.SectionName));

        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();
        services.AddSingleton<IHmrcClock, HmrcClock>();
        services.AddSingleton<IHmrcTokenProvider, HmrcTokenProvider>();
        services.AddSingleton<IEnglishFractionCalculationDateWriteTracker, EnglishFractionCalculationDateWriteTracker>();
        services.AddSingleton<IHmrcRateLimiter>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<LevyImportResilienceOptions>>().Value;
            return new SlidingWindowHmrcRateLimiter(options.MaxRequestsPerWindow, TimeSpan.FromSeconds(options.WindowSeconds));
        });
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

        services.AddSingleton(new Mock<IMessageSession>().Object);
        services.AddTransient<IProviderPaymentApiClient<ProviderEventsApiConfiguration>, ProviderPaymentApiClient>();
        services.AddTransient<IFinanceApiClient<FinanceApiConfiguration>, FinanceApiClient>();
        services.AddScoped<IPeriodEndService, PeriodEndService>();
        services.AddScoped<IEnglishFractionsService, EnglishFractionsService>();
        services.AddScoped<IEnglishFractionsPersistenceService, EnglishFractionsPersistenceService>();
        services.AddScoped<IEnglishFractionCalculationDatePersistenceService, EnglishFractionCalculationDatePersistenceService>();
        services.AddScoped<IExpireFundsService, ExpireFundsService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountPaymentsImportService, AccountPaymentsImportService>();
        services.AddSingleton<IRefreshPaymentDataCompletedEventPublisher, RefreshPaymentDataCompletedEventPublisher>();
        services.AddScoped<IAccountTransfersService, AccountTransfersService>();
        services.AddScoped<ITransferStagedToOperationalService, TransferStagedToOperationalService>();
    }

    private static IConfigurationRoot GenerateConfiguration()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData =
            [
                new KeyValuePair<string, string>("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated"),
                new KeyValuePair<string, string>("AzureWebJobsServiceBus", "abc"),
                new KeyValuePair<string, string>("FinanceApiConfiguration:Url", "https://test.com/"),
                new KeyValuePair<string, string>("FinanceApiConfiguration:Identifier", "https://test.com/"),
                new KeyValuePair<string, string>("ProviderEventsApiConfiguration:Url", "https://test.com/"),
                new KeyValuePair<string, string>("ProviderEventsApiConfiguration:Identifier", "https://test.com/"),
                new KeyValuePair<string, string>("Hmrc:BaseUrl", "https://hmrc.test/"),
                new KeyValuePair<string, string>("Hmrc:ClientId", "client-id"),
                new KeyValuePair<string, string>("Hmrc:ClientSecret", "client-secret"),
                new KeyValuePair<string, string>("Hmrc:Scope", "read:apprenticeship-levy"),
                new KeyValuePair<string, string>("LevyImportResilience:MaxRequestsPerWindow", "6"),
                new KeyValuePair<string, string>("LevyImportResilience:WindowSeconds", "2")
            ]
        };
        var provider = new MemoryConfigurationProvider(configSource);

        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}
