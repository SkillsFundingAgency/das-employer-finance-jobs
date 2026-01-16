using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests
{
    public class WhenAddingServicesToTheContainer
    {
        [TestCase(typeof(IAzureClientCredentialHelper))]
        [TestCase(typeof(IInternalApiClient<FinanceInnerApiConfiguration>))]
        [TestCase(typeof(IProviderPaymentApiClient<ProviderEventsApiConfiguration>))]
        [TestCase(typeof(IFinanceApiClient<FinanceInnerApiConfiguration>))]
        [TestCase(typeof(IPeriodEndService))]     
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
            services.Configure<FinanceInnerApiConfiguration>(configuration.GetSection(nameof(FinanceInnerApiConfiguration)));
            services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceInnerApiConfiguration>>().Value);

            services.Configure<ProviderEventsApiConfiguration>(configuration.GetSection(nameof(ProviderEventsApiConfiguration)));
            services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderEventsApiConfiguration>>().Value);

            services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();
            services.AddTransient(typeof(IInternalApiClient<>), typeof(InternalApiClient<>));

            services.AddTransient<IProviderPaymentApiClient<ProviderEventsApiConfiguration>, ProviderPaymentApiClient>();
            services.AddTransient<IFinanceApiClient<FinanceInnerApiConfiguration>, FinanceApiClient>();
            services.AddScoped<IPeriodEndService, PeriodEndService>();         
        }
        private static IConfigurationRoot GenerateConfiguration()
        {
            var configSource = new MemoryConfigurationSource
            {
                InitialData = new List<KeyValuePair<string, string>>
                {                 
                    new("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated"),
                    new("AzureWebJobsServiceBus", "abc"),
                    new("NServiceBus_License", "test"),
                    new("FinanceInnerApiConfiguration:Url", "https://test.com/"),
                    new("FinanceInnerApiConfiguration:Identifier","https://test.com/"),
                    new("ProviderEventsApiConfiguration:Url", "https://test.com/"),
                    new("ProviderEventsApiConfiguration:Identifier","https://test.com/")
                }
            };
            var provider = new MemoryConfigurationProvider(configSource);

            return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
        }
    }
}