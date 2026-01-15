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
        [TestCase(typeof(IInternalApiClient<FinanceApiConfiguration>))]
        [TestCase(typeof(IProviderPaymentApiClient<ProviderPaymentApiConfiguration>))]
        [TestCase(typeof(IFinanceApiClient<FinanceApiConfiguration>))]
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
            services.Configure<FinanceApiConfiguration>(configuration.GetSection(nameof(FinanceApiConfiguration)));
            services.AddSingleton(cfg => cfg.GetService<IOptions<FinanceApiConfiguration>>().Value);

            services.Configure<ProviderPaymentApiConfiguration>(configuration.GetSection(nameof(ProviderPaymentApiConfiguration)));
            services.AddSingleton(cfg => cfg.GetService<IOptions<ProviderPaymentApiConfiguration>>().Value);

            services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();
            services.AddTransient(typeof(IInternalApiClient<>), typeof(InternalApiClient<>));

            services.AddTransient<IProviderPaymentApiClient<ProviderPaymentApiConfiguration>, ProviderPaymentApiClient>();
            services.AddTransient<IFinanceApiClient<FinanceApiConfiguration>, FinanceApiClient>();
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
                    new("FinanceApiConfiguration:Url", "https://test.com/"),
                    new("FinanceApiConfiguration:Identifier","https://test.com/"),
                    new("ProviderPaymentApiConfiguration:Url", "https://test.com/"),
                    new("ProviderPaymentApiConfiguration:Identifier","https://test.com/")
                }
            };
            var provider = new MemoryConfigurationProvider(configSource);

            return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
        }
    }
}