using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenProcessingTransferStagedToOperationalActivity
{
    private Mock<ILogger<TransferStagedToOperationalActivities>> _loggerMock;
    private Mock<ITransferStagedToOperationalService> _transferStagedToOperationalServiceMock;
    private TransferStagedToOperationalActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<TransferStagedToOperationalActivities>>();
        _transferStagedToOperationalServiceMock = new Mock<ITransferStagedToOperationalService>();
        _activity = new TransferStagedToOperationalActivities(
            _loggerMock.Object,
            _transferStagedToOperationalServiceMock.Object);
    }

    [Test]
    public async Task Then_Returns_Service_Result()
    {
        var input = new TransferStagedToOperationalInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id"
        };
        var expectedResult = new TransferStagedToOperationalResult
        {
            TransfersProcessed = 0,
            Status = "Skipped",
            Message = "disabled"
        };

        _transferStagedToOperationalServiceMock
            .Setup(service => service.Process(input))
            .ReturnsAsync(expectedResult);

        var result = await _activity.TransferStagedToOperationalActivity(input);

        result.Should().Be(expectedResult);
        _transferStagedToOperationalServiceMock.Verify(service => service.Process(input), Times.Once);
    }

    [Test]
    public void Then_Throws_When_Input_Is_Null()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _activity.TransferStagedToOperationalActivity(null));
    }
}
