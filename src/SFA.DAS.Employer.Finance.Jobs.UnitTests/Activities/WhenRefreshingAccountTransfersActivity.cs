using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenRefreshingAccountTransfersActivity
{
    private Mock<ILogger<AccountTransferActivities>> _loggerMock;
    private Mock<IAccountTransfersService> _accountTransfersServiceMock;
    private AccountTransferActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<AccountTransferActivities>>();
        _accountTransfersServiceMock = new Mock<IAccountTransfersService>();
        _activity = new AccountTransferActivities(_loggerMock.Object, _accountTransfersServiceMock.Object);
    }

    [Test]
    public async Task Then_Returns_The_Service_Result()
    {
        var input = new RefreshAccountTransfersInput
        {
            AccountId = 12345,
            PeriodEndRef = "2526-R03",
            CorrelationId = "correlation-id"
        };

        _accountTransfersServiceMock
            .Setup(service => service.RefreshAccountTransfers(input))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 2,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _activity.RefreshAccountTransfersActivity(input);

        result.Status.Should().Be("Succeeded");
        result.TransfersProcessed.Should().Be(2);
        _accountTransfersServiceMock.Verify(service => service.RefreshAccountTransfers(input), Times.Once);
    }
}
