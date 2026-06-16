using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.InnerAPI.Requests;

public class WhenBuildingGetApiRequestUrls
{
    [Test]
    public void Then_GetPaymentPeriodEndsUrl_Is_Correct()
    {
        //Arrange & Act
        var request = new GetPaymentPeriodEndsRequest();

       // Assert
        request.GetUrl.Should().Be("api/periodends");
    }

    [Test]
    public void Then_GetFinancePeriodEndsUrl_Is_Correct()
    {
        //Arrange & Act
        var request = new GetFinancePeriodEndsRequest();

       // Assert
        request.GetUrl.Should().Be("api/period-ends");
    }

    [Test]
    public void Then_GetAccountPaymentsUrl_Does_Not_Include_Ukprn_Filter()
    {
        //Arrange & Act
        var request = new GetAccountPaymentsRequest("2526-R03", 14331, 2);

        // Assert
        request.GetUrl.Should().Be("api/payments?page=2&periodId=2526-R03&employerAccountId=14331");
    }
}
