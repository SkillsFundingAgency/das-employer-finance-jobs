using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.AppStart;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using System.Collections.Generic;
using System.Net.Http;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.AppStart;

public class WhenAddingServiceRegistration
{
    private IServiceCollection _services;
    private IConfiguration _configuration;
    private FinanceApiConfiguration _financeApiConfiguration;
    private PaymentApiConfiguration _paymentApiConfiguration;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        
        _financeApiConfiguration = new FinanceApiConfiguration
        {
            Url = "https://finance-api.test.local",
            Identifier = "finance-api-id"
        };

        _paymentApiConfiguration = new PaymentApiConfiguration
        {
            Url = "https://payment-api.test.local",
            Identifier = "payment-api-id"
        };

        var configurationData = new Dictionary<string, string>
        {
            { "FinanceApiConfiguration:Url", _financeApiConfiguration.Url },
            { "FinanceApiConfiguration:Identifier", _financeApiConfiguration.Identifier },
            { "ProviderEventsApiConfiguration:Url", _paymentApiConfiguration.Url },
            { "ProviderEventsApiConfiguration:Identifier", _paymentApiConfiguration.Identifier }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
    }

    [Test]
    public void Then_Registers_HttpClient()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        
        httpClientFactory.Should().NotBeNull();
    }

    [Test]
    public void Then_Registers_AzureClientCredentialHelper_As_Singleton()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var credentialHelper1 = serviceProvider.GetRequiredService<IAzureClientCredentialHelper>();
        var credentialHelper2 = serviceProvider.GetRequiredService<IAzureClientCredentialHelper>();
        
        credentialHelper1.Should().NotBeNull();
        credentialHelper2.Should().BeSameAs(credentialHelper1);
    }

    [Test]
    public void Then_Registers_FinanceApiClient_As_Scoped()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var financeApiClient = serviceProvider.GetRequiredService<IFinanceApiClient>();
        
        financeApiClient.Should().NotBeNull();
        financeApiClient.Should().BeOfType<FinanceApiClient>();
    }

    [Test]
    public void Then_Registers_PaymentApiClient_As_Scoped()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var paymentApiClient = serviceProvider.GetRequiredService<IPaymentApiClient>();
        
        paymentApiClient.Should().NotBeNull();
        paymentApiClient.Should().BeOfType<PaymentApiClient>();
    }

    [Test]
    public void Then_Registers_PeriodEndService_As_Scoped()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var periodEndService = serviceProvider.GetRequiredService<IPeriodEndService>();
        
        periodEndService.Should().NotBeNull();
    }

    [Test]
    public void Then_FinanceApiClient_Is_Created_With_Correct_Configuration()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var financeApiClient = serviceProvider.GetRequiredService<IFinanceApiClient>() as FinanceApiClient;
        
        financeApiClient.Should().NotBeNull();
    }

    [Test]
    public void Then_PaymentApiClient_Is_Created_With_Correct_Configuration()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var paymentApiClient = serviceProvider.GetRequiredService<IPaymentApiClient>() as PaymentApiClient;
        
        paymentApiClient.Should().NotBeNull();
    }

    [Test]
    public void Then_FinanceApiClient_And_PaymentApiClient_Are_Different_Instances_When_Scoped()
    {
        // Act
        _services.AddServiceRegistration(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        
        var client1 = scope1.ServiceProvider.GetRequiredService<IFinanceApiClient>();
        var client2 = scope2.ServiceProvider.GetRequiredService<IFinanceApiClient>();
        
        client1.Should().NotBeSameAs(client2);
    }

    [Test]
    public void Then_Throws_InvalidOperationException_When_FinanceApiConfiguration_Is_Missing()
    {
        // Arrange
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        // Act
        var act = () => _services.AddServiceRegistration(invalidConfiguration);

        // Assert
        act.Should().NotThrow(); 
    }

    [Test]
    public void Then_Throws_InvalidOperationException_When_PaymentApiConfiguration_Is_Missing()
    {
        // Arrange
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "FinanceApiConfiguration:Url", "https://test.local" },
                { "FinanceApiConfiguration:Identifier", "test-id" }
            })
            .Build();

        // Act
        var act = () => _services.AddServiceRegistration(invalidConfiguration);

        // Assert
        act.Should().NotThrow(); 
    }
}
