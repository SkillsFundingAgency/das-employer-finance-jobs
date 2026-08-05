using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenGettingAccounts
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
    public async Task Then_Returns_Accounts_From_Finance_Api()
    {
        // Arrange
        var request = new GetAccountsRequest { Page = 1, PageSize = 10000, CorrelationId = Guid.NewGuid().ToString() };
        var expectedAccounts = new List<Accounts>
        {
            new() { Id = 1, Name = "Account 1" },
            new() { Id = 2, Name = "Account 2" }
        };
        var response = new FinanceApiGetAccountsResponse { Accounts = expectedAccounts };

        _mockFinanceApiClient
            .Setup(x => x.Get<FinanceApiGetAccountsResponse>(It.Is<GetAccountsRequest>(r => r.Page == 1 && r.PageSize == 10000)))
            .ReturnsAsync(response);

        // Act
        var result = await _accountService.GetAccountsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedAccounts);
        _mockFinanceApiClient.Verify();
    }

    [Test]
    public async Task And_Response_Is_Null_Then_Returns_Empty_List()
    {
        // Arrange
        var request = new GetAccountsRequest { Page = 1, PageSize = 10000, CorrelationId = Guid.NewGuid().ToString() };

        _mockFinanceApiClient
            .Setup(x => x.Get<FinanceApiGetAccountsResponse>(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync((FinanceApiGetAccountsResponse)null);

        // Act
        var result = await _accountService.GetAccountsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task And_Response_Accounts_Is_Null_Then_Returns_Empty_List()
    {
        // Arrange
        var request = new GetAccountsRequest { Page = 1, PageSize = 10000, CorrelationId = Guid.NewGuid().ToString() };
        var response = new FinanceApiGetAccountsResponse { Accounts = null };

        _mockFinanceApiClient
            .Setup(x => x.Get<FinanceApiGetAccountsResponse>(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _accountService.GetAccountsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task And_Finance_Api_Throws_Then_Throws_Exception()
    {
        // Arrange
        var request = new GetAccountsRequest { Page = 1, PageSize = 10000, CorrelationId = Guid.NewGuid().ToString() };
        var expectedException = new InvalidOperationException("Finance API Error");

        _mockFinanceApiClient
            .Setup(x => x.Get<FinanceApiGetAccountsResponse>(It.IsAny<GetAccountsRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _accountService.GetAccountsAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Finance API Error");
    }

    [Test]
    public async Task Then_Calls_Finance_Api_With_Correct_Page_And_PageSize()
    {
        // Arrange
        var request = new GetAccountsRequest { Page = 3, PageSize = 5000, CorrelationId = Guid.NewGuid().ToString() };
        var response = new FinanceApiGetAccountsResponse { Accounts = new List<Accounts>() };

        _mockFinanceApiClient
            .Setup(x => x.Get<FinanceApiGetAccountsResponse>(It.Is<GetAccountsRequest>(r => r.Page == 3 && r.PageSize == 5000)))
            .ReturnsAsync(response);

        // Act
        await _accountService.GetAccountsAsync(request);

        // Assert
        _mockFinanceApiClient.Verify(
            x => x.Get<FinanceApiGetAccountsResponse>(It.Is<GetAccountsRequest>(r => r.Page == 3 && r.PageSize == 5000)),
            Times.Once);
    }
}
