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

    [Test]
    public void Then_GetCoursesSearchUrl_Is_Correct()
    {
        var request = new GetCoursesSearchRequest();

        request.GetUrl.Should().Be("api/courses/search?filter=Active&orderby=Score");
    }

    [Test]
    public void Then_GetCoursesFrameworksUrl_Is_Correct()
    {
        var request = new GetCoursesFrameworksRequest();

        request.GetUrl.Should().Be("api/courses/frameworks");
    }

    [Test]
    public void Then_GetRoatpProviderUrl_Is_Correct()
    {
        var request = new GetRoatpProviderRequest(10012345);

        request.GetUrl.Should().Be("api/providers/10012345");
    }
}
