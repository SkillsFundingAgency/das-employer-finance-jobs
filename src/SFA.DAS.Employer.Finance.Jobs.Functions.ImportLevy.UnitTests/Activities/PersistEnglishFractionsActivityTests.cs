using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class PersistEnglishFractionsActivityTests
{
    private Mock<IEnglishFractionsPersistenceService> _persistenceService = null!;
    private Mock<ILogger<PersistEnglishFractionsActivity>> _logger = null!;
    private PersistEnglishFractionsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _persistenceService = new Mock<IEnglishFractionsPersistenceService>();
        _logger = new Mock<ILogger<PersistEnglishFractionsActivity>>();
        _activity = new PersistEnglishFractionsActivity(_persistenceService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Persists_Fractions_When_Update_Is_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            UpdateRequired = true,
            Fractions =
            [
                new EnglishFraction
                {
                    EmployerReference = "123/AB456",
                    DateCalculated = new DateTime(2026, 4, 10),
                    Amount = 0.70m
                }
            ]
        };

        var expected = new EnglishFractionsPersistenceResult
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            UpdateRequired = true,
            Stored = 1,
            Ignored = 0,
            Skipped = false
        };

        _persistenceService.Setup(x => x.PersistEnglishFractionsAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("Processing English fractions persistence");
        _logger.VerifyLogContains("Stored: 1, Ignored: 0");
    }

    [Test]
    public async Task Run_Logs_Skipped_When_Update_Is_Not_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-456",
            EmployerReference = "123/AB456",
            UpdateRequired = false
        };

        var expected = new EnglishFractionsPersistenceResult
        {
            CorrelationId = "corr-456",
            EmployerReference = "123/AB456",
            UpdateRequired = false,
            Stored = 0,
            Ignored = 0,
            Skipped = true
        };

        _persistenceService.Setup(x => x.PersistEnglishFractionsAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("UpdateRequired: False");
        _logger.VerifyLogContains("Skipped: True");
    }
}
