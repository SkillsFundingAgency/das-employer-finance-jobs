using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenGettingPayeSchemes
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _mockFinanceApiClient;
    private Mock<ILogger<IAccountService>> _mockLogger;
    private AccountService _accountService;

    [SetUp]
    public void SetUp()
    {
        _mockFinanceApiClient = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _mockLogger = new Mock<ILogger<IAccountService>>();
        _accountService = new AccountService(_mockFinanceApiClient.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Then_Returns_Paye_Schemes_From_Direct_Array_Response()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 10, CorrelationId = Guid.NewGuid() };
        var response = ParseJson("""
            [
              { "empRef": "123/AB456", "name": "Scheme one" },
              { "ref": "123/CD789" }
            ]
            """);

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.Is<GetAccountPayeSchemesRequest>(r => r.AccountId == 10)))
            .ReturnsAsync(response);

        var result = await _accountService.GetPayeSchemesAsync(request);

        result.Select(x => x.Reference).Should().Equal("123/AB456", "123/CD789");
        result.First().Name.Should().Be("Scheme one");
    }

    [Test]
    public async Task Then_Returns_Paye_Schemes_From_Wrapped_Response()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 20, CorrelationId = Guid.NewGuid() };
        var response = ParseJson("""
            {
              "payeSchemes": [
                { "empRef": "222/XY123" },
                { "schemeReference": "222/XY999" }
              ]
            }
            """);

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(response);

        var result = await _accountService.GetPayeSchemesAsync(request);

        result.Select(x => x.Reference).Should().Equal("222/XY123", "222/XY999");
    }

    [Test]
    public async Task Then_Returns_Paye_Schemes_From_String_Array_Response()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 30, CorrelationId = Guid.NewGuid() };
        var response = ParseJson("""
            [
              "333/AA111",
              "333/BB222"
            ]
            """);

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(response);

        var result = await _accountService.GetPayeSchemesAsync(request);

        result.Select(x => x.Reference).Should().Equal("333/AA111", "333/BB222");
    }

    [Test]
    public async Task And_Response_Is_Empty_Then_Returns_Empty_List()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 40, CorrelationId = Guid.NewGuid() };

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(default(JsonElement));

        var result = await _accountService.GetPayeSchemesAsync(request);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task And_Response_Contains_Entries_Without_References_Then_Ignores_Them()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 50, CorrelationId = Guid.NewGuid() };
        var response = ParseJson("""
            [
              { "name": "Missing ref" },
              { "empRef": "555/CC333" }
            ]
            """);

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ReturnsAsync(response);

        var result = await _accountService.GetPayeSchemesAsync(request);

        result.Select(x => x.Reference).Should().Equal("555/CC333");
    }

    [Test]
    public async Task And_Finance_Api_Throws_Then_Throws_Exception()
    {
        var request = new GetAccountPayeSchemesRequest { AccountId = 60, CorrelationId = Guid.NewGuid() };
        var expectedException = new InvalidOperationException("Finance API Error");

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.IsAny<GetAccountPayeSchemesRequest>()))
            .ThrowsAsync(expectedException);

        var act = async () => await _accountService.GetPayeSchemesAsync(request);

        var exceptionAssertion = await act.Should().ThrowAsync<InvalidOperationException>();
        exceptionAssertion.WithMessage("Failed to get PAYE schemes for account 60.");
        exceptionAssertion.Which.InnerException.Should().BeSameAs(expectedException);
    }

    [Test]
    public async Task Then_Calls_Finance_Api_With_Correct_Account_And_Source()
    {
        var request = new GetAccountPayeSchemesRequest
        {
            AccountId = 70,
            Source = "government-gateway",
            CorrelationId = Guid.NewGuid()
        };

        _mockFinanceApiClient
            .Setup(x => x.Get<JsonElement>(It.Is<GetAccountPayeSchemesRequest>(r =>
                r.AccountId == 70 &&
                r.Source == "government-gateway" &&
                r.GetUrl == "api/accounts/70/paye-schemes?source=government-gateway")))
            .ReturnsAsync(ParseJson("[]"));

        await _accountService.GetPayeSchemesAsync(request);

        _mockFinanceApiClient.Verify(
            x => x.Get<JsonElement>(It.Is<GetAccountPayeSchemesRequest>(r =>
                r.AccountId == 70 &&
                r.Source == "government-gateway" &&
                r.GetUrl == "api/accounts/70/paye-schemes?source=government-gateway")),
            Times.Once);
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
