using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
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
        _loggerMock.VerifyLogContains(LogLevel.Information, "ProcessAccountExpireFundsActivity started");
        _loggerMock.VerifyLogContains(LogLevel.Information, "ProcessAccountExpireFundsActivity succeeded");
        _loggerMock.VerifyLogContains(LogLevel.Information, input.CorrelationId);
    }

    [Test]
    public async Task Then_A_Successful_Response_With_No_Expired_Funds_Is_Returned()
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
                FundsExpired = false
            });

        var result = await _activities.ProcessAccountExpireFundsActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Success.Should().BeTrue();
        result.FundsExpired.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        _expireFundsServiceMock.VerifyAll();
        _loggerMock.VerifyLogContains(LogLevel.Information, "FundsExpired False");
    }

    [Test]
    public async Task Then_A_Transient_Failure_Is_Propagated_For_Durable_Retry()
    {
        var input = new ProcessAccountExpireFundsInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id"
        };
        var transientException = new HttpRequestContentException(
            "Finance API unavailable",
            HttpStatusCode.ServiceUnavailable,
            "Try again");

        _expireFundsServiceMock
            .Setup(service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId))
            .ThrowsAsync(transientException);

        var action = () => _activities.ProcessAccountExpireFundsActivity(input);

        var exception = await action.Should().ThrowAsync<HttpRequestContentException>();
        exception.Which.Should().BeSameAs(transientException);
        _expireFundsServiceMock.Verify(
            service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId),
            Times.Once);
        _loggerMock.VerifyLogContains(LogLevel.Warning, "transient error");
        _loggerMock.VerifyLogContains(LogLevel.Warning, input.CorrelationId);
    }

    [Test]
    public async Task Then_A_NonTransient_Account_Failure_Is_Returned_Without_Retry()
    {
        var input = new ProcessAccountExpireFundsInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id"
        };
        var nonTransientException = new HttpRequestContentException(
            "Finance API rejected the request",
            HttpStatusCode.BadRequest,
            "Invalid account");

        _expireFundsServiceMock
            .Setup(service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId))
            .ThrowsAsync(nonTransientException);

        var result = await _activities.ProcessAccountExpireFundsActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Success.Should().BeFalse();
        result.FundsExpired.Should().BeFalse();
        result.ErrorMessage.Should().Be("Finance API rejected the request");
        _expireFundsServiceMock.Verify(
            service => service.ExpireFundsAsync(input.AccountId, input.CorrelationId),
            Times.Once);
        _loggerMock.VerifyLogContains(LogLevel.Error, "AccountId 12345");
        _loggerMock.VerifyLogContains(LogLevel.Error, input.CorrelationId);
    }
}
