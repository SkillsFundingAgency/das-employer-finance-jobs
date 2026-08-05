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
public class PersistEnglishFractionCalculationDateActivityTests
{
    private Mock<IEnglishFractionCalculationDatePersistenceService> _persistenceService = null!;
    private Mock<ILogger<PersistEnglishFractionCalculationDateActivity>> _logger = null!;
    private PersistEnglishFractionCalculationDateActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _persistenceService = new Mock<IEnglishFractionCalculationDatePersistenceService>();
        _logger = new Mock<ILogger<PersistEnglishFractionCalculationDateActivity>>();
        _activity = new PersistEnglishFractionCalculationDateActivity(_persistenceService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Persists_Calculation_Date_When_Required()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-123",
            UpdateRequired = true,
            HmrcLatestUpdateDate = new DateTime(2026, 4, 14)
        };

        var expected = new EnglishFractionCalculationDatePersistenceResult
        {
            CorrelationId = "corr-123",
            UpdateRequired = true,
            DateCalculated = new DateTime(2026, 4, 14),
            Persisted = true
        };

        _persistenceService.Setup(x => x.PersistCalculationDateAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("Processing English fraction calculation date persistence");
        _logger.VerifyLogContains("Persisted: True");
    }

    [Test]
    public async Task Run_Logs_When_A_Write_Is_Skipped()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-456",
            UpdateRequired = false,
            HmrcLatestUpdateDate = new DateTime(2026, 4, 14)
        };

        var expected = new EnglishFractionCalculationDatePersistenceResult
        {
            CorrelationId = "corr-456",
            UpdateRequired = false,
            DateCalculated = new DateTime(2026, 4, 14),
            Skipped = true,
            AlreadyPersistedForRunDate = false
        };

        _persistenceService.Setup(x => x.PersistCalculationDateAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("UpdateRequired: False");
        _logger.VerifyLogContains("Skipped: True");
    }

    [Test]
    public async Task Run_Logs_When_The_Run_Date_Has_Already_Been_Persisted()
    {
        var input = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-789",
            UpdateRequired = true,
            HmrcLatestUpdateDate = new DateTime(2026, 4, 14)
        };

        var expected = new EnglishFractionCalculationDatePersistenceResult
        {
            CorrelationId = "corr-789",
            UpdateRequired = true,
            DateCalculated = new DateTime(2026, 4, 14),
            Skipped = true,
            AlreadyPersistedForRunDate = true
        };

        _persistenceService.Setup(x => x.PersistCalculationDateAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("AlreadyPersistedForRunDate: True");
    }
}
