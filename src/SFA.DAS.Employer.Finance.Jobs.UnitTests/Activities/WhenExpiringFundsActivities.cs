using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenExpiringFundsActivities
{
    private Mock<IAccountService> _accountServiceMock;
    private Mock<IExpireFundsService> _expireFundsServiceMock;
    private Mock<ILogger<ExpireFundsActivities>> _loggerMock;
    private ExpireFundsActivities _activities;

    [SetUp]
    public void SetUp()
    {
        _accountServiceMock = new Mock<IAccountService>();
        _expireFundsServiceMock = new Mock<IExpireFundsService>();
        _loggerMock = new Mock<ILogger<ExpireFundsActivities>>();
        _activities = new ExpireFundsActivities(
            _accountServiceMock.Object,
            _expireFundsServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Then_The_Requested_Account_Page_Is_Returned()
    {
        var request = new GetAccountsRequest
        {
            Page = 2,
            PageSize = 100,
            CorrelationId = "correlation-id"
        };
        var accounts = new List<Accounts>
        {
            new() { Id = 12345, Name = "Test account" }
        };

        _accountServiceMock
            .Setup(service => service.GetAccountsAsync(request))
            .ReturnsAsync(accounts);

        var result = await _activities.GetAccountsPageActivity(request);

        result.Should().BeSameAs(accounts);
        _accountServiceMock.VerifyAll();
    }

    [Test]
    public async Task Then_A_Successful_Expiry_Response_Is_Mapped_For_The_Orchestrator()
    {
        var input = new ProcessAccountExpireFundsInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id"
        };
        _expireFundsServiceMock
            .Setup(service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId))
            .ReturnsAsync(new ExpireFundsResponse
            {
                AccountId = input.AccountId,
                CorrelationId = input.CorrelationId,
                FundsExpired = true
            });

        var result = await _activities.ProcessAccountExpireFundsActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Success.Should().BeTrue();
        result.FundsExpired.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        _expireFundsServiceMock.VerifyAll();
    }

    [Test]
    public async Task Then_An_Account_Failure_Is_Returned_Without_Stopping_The_Orchestration()
    {
        var input = new ProcessAccountExpireFundsInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id"
        };
        _expireFundsServiceMock
            .Setup(service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId))
            .ThrowsAsync(new InvalidOperationException("Finance API failed"));

        var result = await _activities.ProcessAccountExpireFundsActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Success.Should().BeFalse();
        result.FundsExpired.Should().BeFalse();
        result.ErrorMessage.Should().Be("Finance API failed");
        _loggerMock.VerifyLogContains(LogLevel.Error, "AccountId 12345");
    }
}
