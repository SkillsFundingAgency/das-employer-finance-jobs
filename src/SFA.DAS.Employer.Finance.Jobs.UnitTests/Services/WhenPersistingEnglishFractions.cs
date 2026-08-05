using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenPersistingEnglishFractions
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClient = null!;
    private Mock<ILogger<EnglishFractionsPersistenceService>> _logger = null!;
    private EnglishFractionsPersistenceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApiClient = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<EnglishFractionsPersistenceService>>();
        _service = new EnglishFractionsPersistenceService(_financeApiClient.Object, _logger.Object);
    }

    [Test]
    public async Task Then_Fractions_Are_Persisted_When_Update_Is_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            UpdateRequired = true,
            HmrcLatestUpdateDate = new DateTime(2026, 4, 10),
            Fractions =
            [
                new EnglishFraction
                {
                    EmployerReference = "123/AB456",
                    DateCalculated = new DateTime(2026, 4, 8),
                    Amount = 0.60m
                },
                new EnglishFraction
                {
                    EmployerReference = "123/AB456",
                    DateCalculated = new DateTime(2026, 4, 10),
                    Amount = 0.75m
                }
            ]
        };

        _financeApiClient
            .Setup(x => x.Post<PersistEnglishFractionsResponse>(It.Is<PersistEnglishFractionsRequest>(request =>
                RequestMatches(request, input))))
            .ReturnsAsync(new PersistEnglishFractionsResponse
            {
                Stored = 2,
                Ignored = 0
            });

        var result = await _service.PersistEnglishFractionsAsync(input);

        result.Stored.Should().Be(2);
        result.Ignored.Should().Be(0);
        result.Skipped.Should().BeFalse();
        _financeApiClient.VerifyAll();
        _logger.VerifyLogContains(LogLevel.Information, "Persisting 2 English fractions");
        _logger.VerifyLogContains(LogLevel.Information, "Stored: 2, Ignored: 0");
    }

    [Test]
    public async Task Then_No_Call_Is_Made_When_Update_Is_Not_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-456",
            EmployerReference = "123/AB456",
            UpdateRequired = false,
            HmrcLatestUpdateDate = new DateTime(2026, 4, 10),
            Fractions =
            [
                new EnglishFraction
                {
                    EmployerReference = "123/AB456",
                    DateCalculated = new DateTime(2026, 4, 10),
                    Amount = 0.75m
                }
            ]
        };

        var result = await _service.PersistEnglishFractionsAsync(input);

        result.Stored.Should().Be(0);
        result.Ignored.Should().Be(0);
        result.Skipped.Should().BeTrue();
        _financeApiClient.Verify(x => x.Post<PersistEnglishFractionsResponse>(It.IsAny<PersistEnglishFractionsRequest>()), Times.Never);
        _logger.VerifyLogContains(LogLevel.Information, "Skipping English fractions persistence");
    }

    [Test]
    public void Then_An_Employer_Reference_Is_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-789",
            EmployerReference = ""
        };

        var action = () => _service.PersistEnglishFractionsAsync(input);

        action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Employer reference must be provided*");
    }

    private static bool RequestMatches(PersistEnglishFractionsRequest request, EnglishFractionsFetchResult input)
    {
        request.GetUrl.Should().Be("api/english-fractions");
        request.Data.Should().BeOfType<PersistEnglishFractionsRequestData>();

        var payload = (PersistEnglishFractionsRequestData)request.Data!;
        payload.EmpRef.Should().Be(input.EmployerReference);
        payload.UpdateRequired.Should().BeTrue();
        payload.DateCalculated.Should().Be(input.HmrcLatestUpdateDate);
        payload.Fractions.Should().HaveCount(2);
        payload.Fractions[0].Amount.Should().Be(0.60m);
        payload.Fractions[1].Amount.Should().Be(0.75m);

        return true;
    }
}
