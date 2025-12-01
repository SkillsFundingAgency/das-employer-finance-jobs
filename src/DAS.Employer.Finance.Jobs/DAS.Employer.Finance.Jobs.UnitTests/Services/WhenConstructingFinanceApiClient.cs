using System;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenConstructingFinanceApiClient
{
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<IAzureClientCredentialHelper> _mockCredentialHelper;
    private FinanceApiConfiguration _validConfiguration;
    private Mock<ILogger<FinanceApiClient>> _mockLogger;
    private HttpClient _httpClient;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new HttpClient();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        
        _mockCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        _validConfiguration = new FinanceApiConfiguration
        {
            Url = "https://finance-api.test.local",
            Identifier = "finance-api-id"
        };
        _mockLogger = new Mock<ILogger<FinanceApiClient>>();
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_HttpClientFactory_Is_Null()
    {
        // Act & Assert
        var act = () => new FinanceApiClient(null!, _mockCredentialHelper.Object, _validConfiguration, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_CredentialHelper_Is_Null()
    {
        // Act & Assert
        var act = () => new FinanceApiClient(_mockHttpClientFactory.Object, null!, _validConfiguration, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialHelper");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Act & Assert
        var act = () => new FinanceApiClient(_mockHttpClientFactory.Object, _mockCredentialHelper.Object, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new FinanceApiClient(_mockHttpClientFactory.Object, _mockCredentialHelper.Object, _validConfiguration, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Test]
    public void Then_Constructs_Successfully_When_All_Parameters_Are_Valid()
    {
        // Act
        var act = () => new FinanceApiClient(_mockHttpClientFactory.Object, _mockCredentialHelper.Object, _validConfiguration, _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void Then_Calls_HttpClientFactory_CreateClient_When_Constructed()
    {
        // Act
        var client = new FinanceApiClient(_mockHttpClientFactory.Object, _mockCredentialHelper.Object, _validConfiguration, _mockLogger.Object);

        // Assert
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void Then_Sets_BaseAddress_From_Configuration()
    {
        // Act
        var client = new FinanceApiClient(_mockHttpClientFactory.Object, _mockCredentialHelper.Object, _validConfiguration, _mockLogger.Object);

        // Assert
        _httpClient.BaseAddress.Should().NotBeNull();
        _httpClient.BaseAddress!.AbsoluteUri.Should().Be("https://finance-api.test.local/");
    }
}