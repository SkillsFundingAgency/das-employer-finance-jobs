using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetAccountPayeSchemesActivityTests
{
    private Mock<IAccountService> _accountService = null!;
    private Mock<ILogger<GetAccountPayeSchemesActivity>> _logger = null!;
    private GetAccountPayeSchemesActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = new Mock<IAccountService>();
        _logger = new Mock<ILogger<GetAccountPayeSchemesActivity>>();
        _activity = new GetAccountPayeSchemesActivity(_accountService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Returns_Paye_Schemes_For_Account()
    {
        var input = new GetAccountPayeSchemesActivityInput
        {
            CorrelationId = "corr-123",
            AccountId = 999
        };

        _accountService
            .Setup(x => x.GetPayeSchemesAsync(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(new List<PayeScheme>
            {
                new() { Reference = "123/AB456" },
                new() { Reference = "123/CD789" }
            });

        var result = await _activity.Run(input);

        result.Select(x => x.Reference).Should().Equal("123/AB456", "123/CD789");
        _accountService.Verify(
            x => x.GetPayeSchemesAsync(It.Is<GetAccountPayeSchemesRequest>(r =>
                r.AccountId == 999 &&
                r.Source == "government-gateway")),
            Times.Once);
    }

    [Test]
    public async Task Run_Returns_Empty_List_When_Account_Has_No_Paye_Schemes()
    {
        var input = new GetAccountPayeSchemesActivityInput
        {
            CorrelationId = "corr-456",
            AccountId = 123
        };

        _accountService
            .Setup(x => x.GetPayeSchemesAsync(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(new List<PayeScheme>());

        var result = await _activity.Run(input);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Run_Retries_And_Succeeds_After_Transient_Failure()
    {
        var input = new GetAccountPayeSchemesActivityInput
        {
            CorrelationId = "corr-789",
            AccountId = 77
        };

        _accountService
            .SetupSequence(x => x.GetPayeSchemesAsync(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ThrowsAsync(new Exception("temporary failure"))
            .ReturnsAsync(new List<PayeScheme> { new() { Reference = "123/AB456" } });

        var result = await _activity.Run(input);

        result.Should().ContainSingle(x => x.Reference == "123/AB456");
        _accountService.Verify(
            x => x.GetPayeSchemesAsync(It.Is<GetAccountPayeSchemesRequest>(r => r.AccountId == 77)),
            Times.Exactly(2));
    }

    [Test]
    public async Task Run_Uses_The_Supplied_CorrelationId_When_It_Is_A_Guid()
    {
        var correlationId = Guid.NewGuid();
        var input = new GetAccountPayeSchemesActivityInput
        {
            CorrelationId = correlationId.ToString(),
            AccountId = 55
        };

        _accountService
            .Setup(x => x.GetPayeSchemesAsync(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(new List<PayeScheme>());

        await _activity.Run(input);

        _accountService.Verify(
            x => x.GetPayeSchemesAsync(It.Is<GetAccountPayeSchemesRequest>(r =>
                r.AccountId == 55 &&
                r.CorrelationId == correlationId)),
            Times.Once);
    }

    [Test]
    public void Run_Throws_When_All_Retries_Are_Exhausted()
    {
        var input = new GetAccountPayeSchemesActivityInput
        {
            CorrelationId = "corr-999",
            AccountId = 444
        };

        _accountService
            .Setup(x => x.GetPayeSchemesAsync(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run(input);

        act.Should().ThrowAsync<Exception>().Wait();

        _accountService.Verify(
            x => x.GetPayeSchemesAsync(It.Is<GetAccountPayeSchemesRequest>(r => r.AccountId == 444)),
            Times.Exactly(3));
    }
}
