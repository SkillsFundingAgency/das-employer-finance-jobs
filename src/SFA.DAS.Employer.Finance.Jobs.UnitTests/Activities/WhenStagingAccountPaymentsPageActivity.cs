using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenStagingAccountPaymentsPageActivity
{
    private Mock<ILogger<AccountPaymentPageStagingActivities>> _loggerMock;
    private Mock<IAccountPaymentPageStagingService> _serviceMock;
    private AccountPaymentPageStagingActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<AccountPaymentPageStagingActivities>>();
        _serviceMock = new Mock<IAccountPaymentPageStagingService>();
        _activity = new AccountPaymentPageStagingActivities(_loggerMock.Object, _serviceMock.Object);
    }

    [Test]
    public async Task Then_Delegates_To_Page_Staging_Service()
    {
        var input = new StageAccountPaymentsPageInput
        {
            AccountId = 14331,
            PeriodEndRef = "2526-R01",
            CorrelationId = "correlation-id",
            PageNumber = 2
        };
        var expected = new StageAccountPaymentsPageResult
        {
            PageNumber = 2,
            TotalPages = 3,
            ItemCount = 10,
            Status = "Succeeded"
        };
        _serviceMock
            .Setup(service => service.StageAccountPaymentsPageAsync(input, CancellationToken.None))
            .ReturnsAsync(expected);

        var result = await _activity.StageAccountPaymentsPageActivity(input);

        result.Should().BeSameAs(expected);
        _serviceMock.Verify(service => service.StageAccountPaymentsPageAsync(input, CancellationToken.None), Times.Once);
    }
}
