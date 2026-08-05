using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenGettingEnglishFractions
{
    private Mock<IHmrcClient> _hmrcClient = null!;
    private Mock<ILogger<EnglishFractionsService>> _logger = null!;
    private EnglishFractionsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _hmrcClient = new Mock<IHmrcClient>();
        _logger = new Mock<ILogger<EnglishFractionsService>>();
        _service = new EnglishFractionsService(_hmrcClient.Object, _logger.Object);
    }

    [Test]
    public async Task Then_Fractions_Are_Fetched_When_An_Update_Is_Required()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            LastStoredFractionCalculatedDate = new DateTime(2026, 3, 20)
        };

        _hmrcClient.Setup(x => x.GetLastEnglishFractionUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTime(2026, 4, 1));

        _hmrcClient.Setup(x => x.GetEnglishFractionsAsync("123/AB456", new DateTime(2026, 3, 19), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnglishFractionDeclarations
            {
                Empref = "123/AB456",
                FractionCalculations =
                [
                    new FractionCalculation
                    {
                        CalculatedAt = new DateTime(2026, 3, 25),
                        Fractions = [ new Fraction { Value = "0.45" } ]
                    },
                    new FractionCalculation
                    {
                        CalculatedAt = new DateTime(2026, 4, 1),
                        Fractions = [ new Fraction { Value = "0.50" } ]
                    }
                ]
            });

        var result = await _service.GetEnglishFractionsAsync(input);

        result.UpdateRequired.Should().BeTrue();
        result.RequestedFrom.Should().Be(new DateTime(2026, 3, 19));
        result.HmrcLatestUpdateDate.Should().Be(new DateTime(2026, 4, 1));
        result.Fractions.Should().HaveCount(2);
        result.Fractions.Select(x => x.Amount).Should().Equal(0.45m, 0.50m);
    }

    [Test]
    public async Task Then_No_Fraction_Fetch_Occurs_When_No_Update_Is_Required()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-456",
            EmployerReference = "123/AB456",
            LastStoredFractionCalculatedDate = new DateTime(2026, 4, 1)
        };

        _hmrcClient.Setup(x => x.GetLastEnglishFractionUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTime(2026, 4, 1));

        var result = await _service.GetEnglishFractionsAsync(input);

        result.UpdateRequired.Should().BeFalse();
        result.Fractions.Should().BeEmpty();

        _hmrcClient.Verify(x => x.GetEnglishFractionsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Then_Invalid_Fraction_Values_Are_Ignored()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-789",
            EmployerReference = "123/AB456"
        };

        _hmrcClient.Setup(x => x.GetLastEnglishFractionUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTime(2026, 4, 1));

        _hmrcClient.Setup(x => x.GetEnglishFractionsAsync("123/AB456", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnglishFractionDeclarations
            {
                Empref = "123/AB456",
                FractionCalculations =
                [
                    new FractionCalculation
                    {
                        CalculatedAt = new DateTime(2026, 4, 1),
                        Fractions =
                        [
                            new Fraction { Value = "not-a-number" },
                            new Fraction { Value = "0.75" }
                        ]
                    }
                ]
            });

        var result = await _service.GetEnglishFractionsAsync(input);

        result.UpdateRequired.Should().BeTrue();
        result.Fractions.Should().ContainSingle();
        result.Fractions.Single().Amount.Should().Be(0.75m);
    }

    [Test]
    public void Then_An_Employer_Reference_Is_Required()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-missing-ref",
            EmployerReference = ""
        };

        var action = () => _service.GetEnglishFractionsAsync(input);

        action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Employer reference must be provided*");
    }

    [Test]
    public async Task Then_Empty_Hmrc_Employer_References_Fall_Back_To_The_Request_Employer_Reference()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-fallback-ref",
            EmployerReference = "123/AB456"
        };

        _hmrcClient.Setup(x => x.GetLastEnglishFractionUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTime(2026, 4, 1));

        _hmrcClient.Setup(x => x.GetEnglishFractionsAsync("123/AB456", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnglishFractionDeclarations
            {
                Empref = "",
                FractionCalculations =
                [
                    new FractionCalculation
                    {
                        CalculatedAt = new DateTime(2026, 4, 1),
                        Fractions = [ new Fraction { Value = "0.60" } ]
                    }
                ]
            });

        var result = await _service.GetEnglishFractionsAsync(input);

        result.Fractions.Should().ContainSingle();
        result.Fractions.Single().EmployerReference.Should().Be("123/AB456");
    }
}
