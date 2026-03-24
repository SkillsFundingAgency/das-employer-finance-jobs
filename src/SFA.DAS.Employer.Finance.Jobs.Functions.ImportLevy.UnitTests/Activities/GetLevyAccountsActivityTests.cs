using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetLevyAccountsActivityTests
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApi = null!;
    private Mock<ILogger<GetLevyAccountsActivity>> _logger = null!;
    private GetLevyAccountsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<GetLevyAccountsActivity>>();

        _activity = new GetLevyAccountsActivity(_financeApi.Object, _logger.Object);
    }

    [Test]
    public async Task Run_RetrievesAllAccounts_AcrossMultiplePages()
    {
        var firstPage = Enumerable.Range(1, GetLevyAccountsActivity.DefaultPageSize).Select(x => (long)x).ToList();
        var secondPage = new List<long> { 10001, 10002 };
        var emptyPage = new List<long>();

        _financeApi
            .SetupSequence(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetAccountsPageRequest>()))
            .ReturnsAsync(CreateResponse(firstPage))
            .ReturnsAsync(CreateResponse(secondPage))
            .ReturnsAsync(CreateResponse(emptyPage));

        var result = await _activity.Run("corr-123");

        result.Should().HaveCount(10002);
        result.Take(3).Should().Equal(1, 2, 3);
        result.TakeLast(2).Should().Equal(10001, 10002);

        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r =>
                r.PageNumber == 1 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r =>
                r.PageNumber == 2 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r =>
                r.PageNumber == 3 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
    }

    [Test]
    public async Task Run_StopsEnumeration_WhenFirstPageIsEmpty()
    {
        _financeApi
            .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetAccountsPageRequest>()))
            .ReturnsAsync(CreateResponse(new List<long>()));

        var result = await _activity.Run("corr-123");

        result.Should().BeEmpty();

        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r =>
                r.PageNumber == 1 && r.PageSize == GetLevyAccountsActivity.DefaultPageSize)),
            Times.Once);
        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r => r.PageNumber > 1)),
            Times.Never);
    }

    [Test]
    public async Task Run_Retries_And_Succeeds_After_Transient_Failure()
    {
        _financeApi
            .SetupSequence(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetAccountsPageRequest>()))
            .ThrowsAsync(new Exception("temporary failure"))
            .ReturnsAsync(CreateResponse(new List<long> { 1, 2 }))
            .ReturnsAsync(CreateResponse(new List<long>()));

        var result = await _activity.Run("corr-123");

        result.Should().Equal(1, 2);

        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r => r.PageNumber == 1)),
            Times.Exactly(2));
    }

    [Test]
    public void Run_Throws_When_All_Retries_Are_Exhausted()
    {
        _financeApi
            .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetAccountsPageRequest>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run("corr-123");

        act.Should().ThrowAsync<Exception>().Wait();

        _financeApi.Verify(
            x => x.GetWithResponseCode<List<long>>(It.Is<GetAccountsPageRequest>(r => r.PageNumber == 1)),
            Times.Exactly(3));
    }

    private static ApiResponse<List<long>> CreateResponse(List<long> body)
    {
        return new ApiResponse<List<long>>(body, HttpStatusCode.OK, string.Empty, new Dictionary<string, IEnumerable<string>>());
    }
}
