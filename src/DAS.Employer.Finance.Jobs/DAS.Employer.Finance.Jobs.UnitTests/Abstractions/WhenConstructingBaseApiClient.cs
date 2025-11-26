using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using System;
using System.Net.Http;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;

public class WhenConstructingBaseApiClient
{
    private class TestBaseApiClient : BaseApiClient
    {
        public TestBaseApiClient(HttpClient httpClient, IAzureClientCredentialHelper credentialHelper, ILogger logger, IApiConfiguration configuration)
            : base(httpClient, credentialHelper, logger, configuration)
        {
        }
    }

    private HttpClient _validHttpClient;
    private Mock<IAzureClientCredentialHelper> _validCredentialHelper;
    private Mock<ILogger> _validLogger;
    private TestApiConfiguration _validConfiguration;

    [SetUp]
    public void SetUp()
    {
        _validHttpClient = new HttpClient();
        _validCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        _validLogger = new Mock<ILogger>();
        _validConfiguration = new TestApiConfiguration
        {
            Url = "https://test.local",
            Identifier = "test-id"
        };
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_HttpClient_Is_Null()
    {
        // Act & Assert
        var act = () => new TestBaseApiClient(null!, _validCredentialHelper.Object, _validLogger.Object, _validConfiguration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_CredentialHelper_Is_Null()
    {
        // Act & Assert
        var act = () => new TestBaseApiClient(_validHttpClient, null!, _validLogger.Object, _validConfiguration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialHelper");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, null!, _validConfiguration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Act & Assert
        var act = () => new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, _validLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Configuration_Url_Is_Null()
    {
        // Arrange
        var invalidConfiguration = new TestApiConfiguration
        {
            Url = null!,
            Identifier = "test-id"
        };

        // Act & Assert
        var act = () => new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, _validLogger.Object, invalidConfiguration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("Url");
    }

    [Test]
    public void Then_Throws_ArgumentNullException_When_Configuration_Identifier_Is_Null()
    {
        // Arrange
        var invalidConfiguration = new TestApiConfiguration
        {
            Url = "https://test.local",
            Identifier = null!
        };

        // Act & Assert
        var act = () => new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, _validLogger.Object, invalidConfiguration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("Identifier");
    }  

    [Test]
    public void Then_Sets_BaseAddress_When_Constructed_With_Valid_Configuration()
    {
        // Act
        var client = new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, _validLogger.Object, _validConfiguration);

        // Assert
        _validHttpClient.BaseAddress.Should().NotBeNull();
        _validHttpClient.BaseAddress!.AbsoluteUri.Should().Be("https://test.local/");
    }

    [Test]
    public void Then_Sets_Accept_Header_When_Constructed()
    {
        // Act
        var client = new TestBaseApiClient(_validHttpClient, _validCredentialHelper.Object, _validLogger.Object, _validConfiguration);

        // Assert
        _validHttpClient.DefaultRequestHeaders.Accept.Should().NotBeEmpty();
        _validHttpClient.DefaultRequestHeaders.Accept.ToString().Should().Contain("application/json");
    }
}

