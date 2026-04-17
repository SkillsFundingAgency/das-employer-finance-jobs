using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenPersistingEnglishFractionCalculationDate
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClient = null!;
    private Mock<ILogger<EnglishFractionCalculationDatePersistenceService>> _logger = null!;
    private IEnglishFractionCalculationDateWriteTracker _writeTracker = null!;
    private EnglishFractionCalculationDatePersistenceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApiClient = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<EnglishFractionCalculationDatePersistenceService>>();
        _writeTracker = new EnglishFractionCalculationDateWriteTracker();
        _service = new EnglishFractionCalculationDatePersistenceService(_financeApiClient.Object, _writeTracker, _logger.Object);
    }

    [Test]
    public async Task Then_Calculation_Date_Is_Persisted_When_Update_Is_Required()
    {
        var input = CreateInput("corr-123", true, new DateTime(2026, 4, 14, 15, 30, 0));

        _financeApiClient
            .Setup(x => x.Post(
                "api/english-fraction-calculation-date",
                It.Is<PersistEnglishFractionCalculationDateRequestData>(request => request.DateCalculated == new DateTime(2026, 4, 14))))
            .Returns(Task.CompletedTask);

        var result = await _service.PersistCalculationDateAsync(input);

        result.Persisted.Should().BeTrue();
        result.Skipped.Should().BeFalse();
        result.AlreadyPersistedForRunDate.Should().BeFalse();

        _financeApiClient.VerifyAll();
        _logger.VerifyLogContains(LogLevel.Information, "Persisting English fraction calculation date");
        _logger.VerifyLogContains(LogLevel.Information, "Persisted English fraction calculation date");
    }

    [Test]
    public async Task Then_No_Call_Is_Made_When_No_Update_Is_Required()
    {
        var input = CreateInput("corr-123", false, new DateTime(2026, 4, 14));

        var result = await _service.PersistCalculationDateAsync(input);

        result.Persisted.Should().BeFalse();
        result.Skipped.Should().BeTrue();
        result.UpdateRequired.Should().BeFalse();

        _financeApiClient.Verify(
            x => x.Post(It.IsAny<string>(), It.IsAny<PersistEnglishFractionCalculationDateRequestData>()),
            Times.Never);
        _logger.VerifyLogContains(LogLevel.Information, "Skipping English fraction calculation date persistence");
    }

    [Test]
    public async Task Then_Only_One_Write_Occurs_Per_Run_And_Date()
    {
        var input = CreateInput("corr-123", true, new DateTime(2026, 4, 14, 8, 0, 0));

        _financeApiClient
            .Setup(x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()))
            .Returns(Task.CompletedTask);

        var firstResult = await _service.PersistCalculationDateAsync(input);
        var secondResult = await _service.PersistCalculationDateAsync(input);

        firstResult.Persisted.Should().BeTrue();
        secondResult.Persisted.Should().BeFalse();
        secondResult.Skipped.Should().BeTrue();
        secondResult.AlreadyPersistedForRunDate.Should().BeTrue();

        _financeApiClient.Verify(
            x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Different_Dates_Are_Written_Separately_For_The_Same_Run()
    {
        var firstInput = CreateInput("corr-123", true, new DateTime(2026, 4, 14));
        var secondInput = CreateInput("corr-123", true, new DateTime(2026, 4, 15));

        _financeApiClient
            .Setup(x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()))
            .Returns(Task.CompletedTask);

        await _service.PersistCalculationDateAsync(firstInput);
        await _service.PersistCalculationDateAsync(secondInput);

        _financeApiClient.Verify(
            x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task Then_A_Failed_Write_Can_Be_Retried_For_The_Same_Run_And_Date()
    {
        var input = CreateInput("corr-123", true, new DateTime(2026, 4, 14));

        _financeApiClient
            .SetupSequence(x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()))
            .ThrowsAsync(new InvalidOperationException("Finance API error"))
            .Returns(Task.CompletedTask);

        var firstAttempt = async () => await _service.PersistCalculationDateAsync(input);
        await firstAttempt.Should().ThrowAsync<InvalidOperationException>();

        var secondResult = await _service.PersistCalculationDateAsync(input);

        secondResult.Persisted.Should().BeTrue();
        _financeApiClient.Verify(
            x => x.Post("api/english-fraction-calculation-date", It.IsAny<PersistEnglishFractionCalculationDateRequestData>()),
            Times.Exactly(2));
    }

    [Test]
    public void Then_A_Calculation_Date_Is_Required_When_Update_Is_Required()
    {
        var input = CreateInput("corr-123", true, DateTime.MinValue);

        var action = () => _service.PersistCalculationDateAsync(input);

        action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Calculation date must be provided*");
    }

    private static EnglishFractionsFetchResult CreateInput(string correlationId, bool updateRequired, DateTime calculationDate)
    {
        return new EnglishFractionsFetchResult
        {
            CorrelationId = correlationId,
            UpdateRequired = updateRequired,
            HmrcLatestUpdateDate = calculationDate
        };
    }
}
