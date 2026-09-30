using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenImportingAccountExistingPaymentIds
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _providerPaymentApiClientMock;
    private Mock<ILogger<AccountPaymentsImportService>> _loggerMock;
    private AccountPaymentsImportService _service;

    [SetUp]
    public void SetUp()
    {
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _providerPaymentApiClientMock = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
        _loggerMock = new Mock<ILogger<AccountPaymentsImportService>>();
        _service = new AccountPaymentsImportService(
            _financeApiClientMock.Object,
            _providerPaymentApiClientMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Then_Pages_Existing_Payment_Ids_Until_TotalPages()
    {
        var page1Id = Guid.NewGuid().ToString();
        var page2Id = Guid.NewGuid().ToString();

        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<GetAccountPaymentIdsResponse>(
                It.Is<GetExistingPaymentIdsRequest>(request => request.GetUrl.Contains("pageNumber=1"))))
            .ReturnsAsync(new ApiResponse<GetAccountPaymentIdsResponse>(
                new GetAccountPaymentIdsResponse
                {
                    PaymentIds = [page1Id],
                    TotalPages = 2,
                    PageNumber = 1,
                    PageSize = 10000
                },
                HttpStatusCode.OK,
                null));
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<GetAccountPaymentIdsResponse>(
                It.Is<GetExistingPaymentIdsRequest>(request => request.GetUrl.Contains("pageNumber=2"))))
            .ReturnsAsync(new ApiResponse<GetAccountPaymentIdsResponse>(
                new GetAccountPaymentIdsResponse
                {
                    PaymentIds = [page2Id],
                    TotalPages = 2,
                    PageNumber = 2,
                    PageSize = 10000
                },
                HttpStatusCode.OK,
                null));

        var result = await _service.ImportAccountExistingPaymentIdsAsync(14331, "correlation-id");

        result.Status.Should().Be("Succeeded");
        result.PaymentIds.Should().BeEquivalentTo([page1Id, page2Id]);
        _financeApiClientMock.Verify(
            client => client.GetWithResponseCode<GetAccountPaymentIdsResponse>(It.IsAny<GetExistingPaymentIdsRequest>()),
            Times.Exactly(2));
    }
}
