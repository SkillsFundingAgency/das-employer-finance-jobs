using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetLevyAccountsActivityTests
{
    private Mock<IAccountService> _accountService = null!;
    private Mock<ILogger<GetLevyAccountsActivity>> _logger = null!;
    private GetLevyAccountsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = new Mock<IAccountService>();
        _logger = new Mock<ILogger<GetLevyAccountsActivity>>();

        _activity = new GetLevyAccountsActivity(_accountService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_RetrievesAllAccounts_AcrossMultiplePages()
    {
        var firstPage = Enumerable.Range(1, GetLevyAccountsActivity.DefaultPageSize)
            .Select(x => new Accounts { Id = x })
            .ToList();
        var secondPage = new List<Accounts>
        {
            new() { Id = 10001 },
            new() { Id = 10002 }
        };

        _accountService
            .SetupSequence(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync(firstPage)
            .ReturnsAsync(secondPage)
            .ReturnsAsync(new List<Accounts>());

        var result = await _activity.Run("corr-123");

        result.Should().HaveCount(10002);
        result.Take(3).Should().Equal(1, 2, 3);
        result.TakeLast(2).Should().Equal(10001, 10002);

        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r =>
                r.Page == 1 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r =>
                r.Page == 2 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r =>
                r.Page == 3 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
    }

    [Test]
    public async Task Run_StopsEnumeration_WhenFirstPageIsEmpty()
    {
        _accountService
            .Setup(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync(new List<Accounts>());

        var result = await _activity.Run("corr-123");

        result.Should().BeEmpty();

        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r =>
                r.Page == 1 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page > 1)),
            Times.Never);
    }

    [Test]
    public async Task Run_Retries_And_Succeeds_After_Transient_Failure()
    {
        _accountService
            .SetupSequence(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ThrowsAsync(new Exception("temporary failure"))
            .ReturnsAsync(new List<Accounts> { new() { Id = 1 }, new() { Id = 2 } })
            .ReturnsAsync(new List<Accounts>());

        var result = await _activity.Run("corr-123");

        result.Should().Equal(1, 2);

        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1)),
            Times.Exactly(2));
    }

    [Test]
    public void Run_Throws_When_All_Retries_Are_Exhausted()
    {
        _accountService
            .Setup(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run("corr-123");

        act.Should().ThrowAsync<Exception>().Wait();

        _accountService.Verify(
            x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1)),
            Times.Exactly(3));
    }
}
