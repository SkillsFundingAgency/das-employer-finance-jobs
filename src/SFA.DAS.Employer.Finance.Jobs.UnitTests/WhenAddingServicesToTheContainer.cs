using System;
using System.Collections.Generic;
using System.Net.Http;
using HMRC.ESFA.Levy.Api.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
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
    [TestCase(typeof(IHmrcRequestThrottle))]
    [TestCase(typeof(IHmrcTokenProvider))]
    [TestCase(typeof(IPeriodEndService))]
    [TestCase(typeof(IEnglishFractionsService))]
    [TestCase(typeof(IEnglishFractionsPersistenceService))]
    [TestCase(typeof(IAccountPaymentsImportService))]
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
        services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceApiConfiguration>>().Value);

        services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderEventsApiConfiguration>>().Value);

        services.Configure<HmrcConfiguration>(configuration.GetSection("Hmrc"));
        services.AddSingleton(cfg => cfg.GetService<IOptions<HmrcConfiguration>>().Value);

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
        services.AddScoped<IEnglishFractionsService, EnglishFractionsService>();
        services.AddScoped<IEnglishFractionsPersistenceService, EnglishFractionsPersistenceService>();
        services.AddScoped<IAccountPaymentsImportService, AccountPaymentsImportService>();
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
                new KeyValuePair<string, string>("Hmrc:Scope", "read:apprenticeship-levy")
            ]
        };
        var provider = new MemoryConfigurationProvider(configSource);

        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}
