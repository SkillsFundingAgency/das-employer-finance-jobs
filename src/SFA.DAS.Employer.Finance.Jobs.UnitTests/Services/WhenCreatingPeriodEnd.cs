using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenCreatingPeriodEnd
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClient;
    private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _providerPaymentApiClient;
    private Mock<ILogger<PeriodEndService>> _logger;
    private PeriodEndService _periodEndService;

    [SetUp]
    public void SetUp()
    {
        _financeApiClient = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _providerPaymentApiClient = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
        _logger = new Mock<ILogger<PeriodEndService>>();
        _periodEndService = new PeriodEndService(_financeApiClient.Object, _providerPaymentApiClient.Object, _logger.Object);
    }

    [Test]
    public async Task Then_Posts_To_Finance_Api_And_Returns_Created_Period_End()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-202401",
            CalendarPeriodYear = 2024,
            CalendarPeriodMonth = 1,
            AccountDataValidAt = DateTime.UtcNow.AddDays(-1),
            CommitmentDataValidAt = DateTime.UtcNow.AddDays(-1)
        };
        var createdPeriodEnd = new PeriodEnd
        {
            Id = 123,
            PeriodEndId = "PE-202401",
            CalendarPeriodYear = 2024,
            CalendarPeriodMonth = 1
        };

        _financeApiClient
            .Setup(x => x.Post<PeriodEnd>(It.Is<CreatePeriodEndRequest>(r => r.Data == input)))
            .ReturnsAsync(createdPeriodEnd);

        // Act
        var result = await _periodEndService.CreatePeriodEndAsync(input, correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(123);
        result.PeriodEndId.Should().Be("PE-202401");
        _financeApiClient.VerifyAll();
        _financeApiClient.VerifyNoOtherCalls();
    }

    [Test]
    public async Task And_Finance_Api_Throws_Then_Throws_Exception()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var input = new PeriodEnd { PeriodEndId = "PE-ERR" };
        var expectedException = new InvalidOperationException("Finance API Error");

        _financeApiClient
            .Setup(x => x.Post<PeriodEnd>(It.IsAny<CreatePeriodEndRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _periodEndService.CreatePeriodEndAsync(input, correlationId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Finance API Error");
    }
}
