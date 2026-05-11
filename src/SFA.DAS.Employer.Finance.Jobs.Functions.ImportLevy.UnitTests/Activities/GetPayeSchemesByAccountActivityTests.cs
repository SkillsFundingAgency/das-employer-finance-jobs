using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetPayeSchemesByAccountActivityTests
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApi = null!;
    private Mock<ILogger<GetPayeSchemesByAccountActivity>> _logger = null!;
    private Mock<IRetryService> _retryService = null!;
    private GetPayeSchemesByAccountActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<GetPayeSchemesByAccountActivity>>();
        _retryService = new Mock<IRetryService>();
        _retryService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<List<PayeScheme>>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .Returns((Func<Task<ApiResponse<List<PayeScheme>>>> action, string _, string _, int _) => action());
        _activity = new GetPayeSchemesByAccountActivity(_financeApi.Object, _retryService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Returns_PayeSchemes_For_Account()
    {
        var request = new GetPayeSchemesByAccountActivityRequest(12345, "corr-123", "hmrc");
        var schemes = new List<PayeScheme>
        {
            new() { EmpRef = "123/AB12345" },
            new() { EmpRef = "123/CD67890" }
        };

        _financeApi
            .Setup(x => x.GetWithResponseCode<List<PayeScheme>>(It.IsAny<GetPayeSchemesByAccountRequest>()))
            .ReturnsAsync(CreateResponse(schemes));

        var result = await _activity.Run(request);

        result.Should().BeEquivalentTo(schemes);
        _financeApi.Verify(
            x => x.GetWithResponseCode<List<PayeScheme>>(It.Is<GetPayeSchemesByAccountRequest>(r =>
                r.GetUrl == "api/accounts/12345/paye-schemes?source=hmrc")),
            Times.Once);
    }

    [Test]
    public void Run_Throws_WhenRetryServiceThrows()
    {
        var request = new GetPayeSchemesByAccountActivityRequest(12345, "corr-123");

        _retryService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<List<PayeScheme>>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run(request);

        act.Should().ThrowAsync<Exception>().Wait();
    }

    private static ApiResponse<List<PayeScheme>> CreateResponse(List<PayeScheme> body)
    {
        return new ApiResponse<List<PayeScheme>>(body, HttpStatusCode.OK, string.Empty, new Dictionary<string, IEnumerable<string>>());
    }
}
