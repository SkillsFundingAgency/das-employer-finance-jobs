using System.Net;
using System.Threading;
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
public class WhenCreatingPaymentMetadata
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<ICommitmentsApiClient> _commitmentsApiClientMock;
    private Mock<IEmployerFinanceOuterApiClient> _outerApiClientMock;
    private Mock<ILogger<PaymentMetadataService>> _loggerMock;
    private PaymentMetadataService _service;

    [SetUp]
    public void SetUp()
    {
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _commitmentsApiClientMock = new Mock<ICommitmentsApiClient>();
        _outerApiClientMock = new Mock<IEmployerFinanceOuterApiClient>();
        _loggerMock = new Mock<ILogger<PaymentMetadataService>>();

        _service = new PaymentMetadataService(
            _financeApiClientMock.Object,
            _commitmentsApiClientMock.Object,
            _outerApiClientMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Then_Maps_Standard_Payment_Metadata()
    {
        var paymentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var startDate = new DateTime(2024, 8, 1);
        const long cohortId = 54321;
        var payment = CreatePayment(paymentId);
        payment.StandardCode = 123;

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(9876))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                NINumber = "AB123456C",
                StartDate = startDate,
                CohortId = cohortId
            });

        _outerApiClientMock
            .Setup(client => client.GetProvider(10000494))
            .ReturnsAsync(new ProviderDetails
            {
                Ukprn = 10000494,
                Name = "Test Provider"
            });

        _outerApiClientMock
            .Setup(client => client.GetStandards())
            .ReturnsAsync(new StandardsResponse
            {
                Standards =
                [
                    new StandardResponse
                    {
                        Id = "123",
                        Title = "Software developer",
                        Level = 4,
                        LearningType = "Apprenticeship"
                    }
                ]
            });

        var result = await _service.BuildPaymentMetadata(12345, payment, correlationId);

        result.PaymentId.Should().Be(paymentId);
        result.ProviderName.Should().Be("Test Provider");
        result.StandardCode.Should().Be(123);
        result.FrameworkCode.Should().BeNull();
        result.ProgrammeType.Should().BeNull();
        result.PathwayCode.Should().BeNull();
        result.ApprenticeName.Should().Be("Ada Lovelace");
        result.ApprenticeNINumber.Should().Be("AB123456C");
        result.ApprenticeshipCourseStartDate.Should().Be(startDate);
        result.CohortId.Should().Be(cohortId);
        result.ApprenticeshipCourseName.Should().Be("Software developer");
        result.ApprenticeshipCourseLevel.Should().Be(4);
        result.CourseCode.Should().BeNull();
        result.LearningType.Should().Be("Apprenticeship");
        result.CorrelationId.Should().Be(correlationId);
    }

    [Test]
    public async Task Then_Maps_Framework_Payment_Metadata()
    {
        var paymentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var payment = CreatePayment(paymentId);
        payment.FrameworkCode = 10;
        payment.ProgrammeType = 20;
        payment.PathwayCode = 30;

        _outerApiClientMock
            .Setup(client => client.GetProvider(10000494))
            .ReturnsAsync(new ProviderDetails
            {
                Ukprn = 10000494,
                Name = "Framework Provider"
            });

        _outerApiClientMock
            .Setup(client => client.GetFrameworks())
            .ReturnsAsync(new FrameworksResponse
            {
                Frameworks =
                [
                    new FrameworkResponse
                    {
                        FrameworkCode = 10,
                        ProgType = 20,
                        PathwayCode = 30,
                        FrameworkName = "Engineering framework",
                        PathwayName = "Mechanical pathway",
                        Level = 3
                    }
                ]
            });

        var result = await _service.BuildPaymentMetadata(12345, payment, correlationId);

        result.ProviderName.Should().Be("Framework Provider");
        result.FrameworkCode.Should().Be(10);
        result.ProgrammeType.Should().Be(20);
        result.PathwayCode.Should().Be(30);
        result.ApprenticeshipCourseName.Should().Be("Engineering framework");
        result.PathwayName.Should().Be("Mechanical pathway");
        result.ApprenticeshipCourseLevel.Should().Be(3);
        result.CourseCode.Should().BeNull();
        result.LearningType.Should().Be("Apprenticeship");
    }

    [Test]
    public async Task Then_Posts_Metadata_To_Finance_For_Each_Payment()
    {
        var paymentId = Guid.NewGuid();
        const long cohortId = 98765;
        var payment = CreatePayment(paymentId);
        payment.StandardCode = 123;

        _outerApiClientMock
            .Setup(client => client.GetProvider(payment.Ukprn))
            .ReturnsAsync(new ProviderDetails { Ukprn = payment.Ukprn, Name = "Test Provider" });

        _outerApiClientMock
            .Setup(client => client.GetStandards())
            .ReturnsAsync(new StandardsResponse
            {
                Standards =
                [
                    new StandardResponse
                    {
                        Id = "123",
                        Title = "Software developer",
                        Level = 4,
                        LearningType = "Apprenticeship"
                    }
                ]
            });

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(payment.ApprenticeshipId!.Value))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                NINumber = "AB123456C",
                StartDate = new DateTime(2024, 8, 1),
                CohortId = cohortId
            });

        _financeApiClientMock
            .Setup(client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(
                It.Is<PutPaymentMetadataToStagingRequest>(request =>
                    request.GetUrl == $"api/payments/{paymentId}/metadata/staging"
                    && ((PaymentMetadataStaging)request.Data).PaymentId == paymentId
                    && ((PaymentMetadataStaging)request.Data).ProviderName == "Test Provider"
                    && ((PaymentMetadataStaging)request.Data).ApprenticeshipCourseName == "Software developer"
                    && ((PaymentMetadataStaging)request.Data).LearningType == "Apprenticeship"
                    && ((PaymentMetadataStaging)request.Data).CourseCode == null
                    && ((PaymentMetadataStaging)request.Data).CohortId == cohortId
                    && ((PaymentMetadataStaging)request.Data).ApprenticeNINumber == "AB123456C")))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse { Upserted = true, MetadataId = 1 },
                HttpStatusCode.OK,
                null));

        var result = await _service.CreatePaymentMetadata(new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = Guid.NewGuid().ToString(),
            PaymentDetails = [payment]
        }, CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.MetadataCreated.Should().Be(1);
        _financeApiClientMock.Verify(
            client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(It.IsAny<PutPaymentMetadataToStagingRequest>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Posts_App_Unit_Metadata_To_Finance()
    {
        var paymentId = Guid.NewGuid();
        const long cohortId = 45678;
        var payment = CreatePayment(paymentId);
        payment.CourseCode = "ST0001";

        _outerApiClientMock
            .Setup(client => client.GetProvider(payment.Ukprn))
            .ReturnsAsync(new ProviderDetails { Ukprn = payment.Ukprn, Name = "Unit Provider" });

        _outerApiClientMock
            .Setup(client => client.GetStandards())
            .ReturnsAsync(new StandardsResponse
            {
                Standards =
                [
                    new StandardResponse
                    {
                        Id = "ST0001",
                        Title = "Software development app unit",
                        Level = 5,
                        LearningType = "ApprenticeshipUnit"
                    }
                ]
            });

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(payment.ApprenticeshipId!.Value))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "Grace",
                LastName = "Hopper",
                NINumber = "QQ123456C",
                StartDate = new DateTime(2025, 2, 1),
                CohortId = cohortId
            });

        _financeApiClientMock
            .Setup(client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(
                It.Is<PutPaymentMetadataToStagingRequest>(request =>
                    request.GetUrl == $"api/payments/{paymentId}/metadata/staging"
                    && ((PaymentMetadataStaging)request.Data).PaymentId == paymentId
                    && ((PaymentMetadataStaging)request.Data).ApprenticeshipCourseName == "Software development app unit"
                    && ((PaymentMetadataStaging)request.Data).ApprenticeshipCourseLevel == 5
                    && ((PaymentMetadataStaging)request.Data).LearningType == "ApprenticeshipUnit"
                    && ((PaymentMetadataStaging)request.Data).CourseCode == "ST0001"
                    && ((PaymentMetadataStaging)request.Data).CohortId == cohortId)))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse { Upserted = true, MetadataId = 2 },
                HttpStatusCode.OK,
                null));

        var result = await _service.CreatePaymentMetadata(new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = Guid.NewGuid().ToString(),
            PaymentDetails = [payment]
        }, CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.MetadataCreated.Should().Be(1);
    }

    [Test]
    public async Task Then_Posts_Metadata_With_Default_LearningType_When_Course_Code_Is_Absent()
    {
        var paymentId = Guid.NewGuid();
        var payment = CreatePayment(paymentId);

        _outerApiClientMock
            .Setup(client => client.GetProvider(payment.Ukprn))
            .ReturnsAsync(new ProviderDetails { Ukprn = payment.Ukprn, Name = "No Course Provider" });

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(payment.ApprenticeshipId!.Value))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "No",
                LastName = "Course",
                NINumber = "AA123456C",
                StartDate = new DateTime(2025, 3, 1)
            });

        _financeApiClientMock
            .Setup(client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(
                It.Is<PutPaymentMetadataToStagingRequest>(request =>
                    request.GetUrl == $"api/payments/{paymentId}/metadata/staging"
                    && ((PaymentMetadataStaging)request.Data).PaymentId == paymentId
                    && ((PaymentMetadataStaging)request.Data).ApprenticeshipCourseName == null
                    && ((PaymentMetadataStaging)request.Data).ApprenticeshipCourseLevel == null
                    && ((PaymentMetadataStaging)request.Data).LearningType == "Apprenticeship"
                    && ((PaymentMetadataStaging)request.Data).CourseCode == null
                    && ((PaymentMetadataStaging)request.Data).CohortId == null)))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse { Upserted = true, MetadataId = 3 },
                HttpStatusCode.OK,
                null));

        var result = await _service.CreatePaymentMetadata(new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = Guid.NewGuid().ToString(),
            PaymentDetails = [payment]
        }, CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.MetadataCreated.Should().Be(1);
        _outerApiClientMock.Verify(client => client.GetStandards(), Times.Never);
        _outerApiClientMock.Verify(client => client.GetFrameworks(), Times.Never);
    }

    [Test]
    public async Task Then_Returns_PartiallySucceeded_When_A_Metadata_Put_Fails()
    {
        var successfulPayment = CreatePayment(Guid.NewGuid());
        successfulPayment.StandardCode = 123;
        var failedPayment = CreatePayment(Guid.NewGuid());
        failedPayment.StandardCode = 123;

        _outerApiClientMock
            .Setup(client => client.GetProvider(It.IsAny<long>()))
            .ReturnsAsync((long ukprn) => new ProviderDetails { Ukprn = ukprn, Name = "Test Provider" });

        _outerApiClientMock
            .Setup(client => client.GetStandards())
            .ReturnsAsync(new StandardsResponse
            {
                Standards =
                [
                    new StandardResponse
                    {
                        Id = "123",
                        Title = "Software developer",
                        Level = 4,
                        LearningType = "Apprenticeship"
                    }
                ]
            });

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(It.IsAny<long>()))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                NINumber = "AB123456C",
                StartDate = new DateTime(2024, 8, 1),
                CohortId = 11111
            });

        _financeApiClientMock
            .SetupSequence(client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(It.IsAny<PutPaymentMetadataToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse { Upserted = true, MetadataId = 1 },
                HttpStatusCode.OK,
                null))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse(),
                HttpStatusCode.BadRequest,
                "invalid"));

        var result = await _service.CreatePaymentMetadata(new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = Guid.NewGuid().ToString(),
            PaymentDetails = [successfulPayment, failedPayment]
        }, CancellationToken.None);

        result.Status.Should().Be("PartiallySucceeded");
        result.MetadataCreated.Should().Be(1);
        result.Message.Should().Be("Created 1 payment metadata staging rows. Failed 1.");
    }

    [Test]
    public async Task Then_Returns_Failed_When_Metadata_Put_Fails()
    {
        var payment = CreatePayment(Guid.NewGuid());

        _outerApiClientMock
            .Setup(client => client.GetProvider(payment.Ukprn))
            .ReturnsAsync(new ProviderDetails { Ukprn = payment.Ukprn, Name = "Test Provider" });

        _commitmentsApiClientMock
            .Setup(client => client.GetApprenticeship(payment.ApprenticeshipId!.Value))
            .ReturnsAsync(new ApprenticeshipDetails
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                NINumber = "AB123456C",
                StartDate = new DateTime(2024, 8, 1)
            });

        _financeApiClientMock
            .Setup(client => client.PutWithResponseCode<PaymentMetadataStagingResponse>(It.IsAny<PutPaymentMetadataToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PaymentMetadataStagingResponse>(
                new PaymentMetadataStagingResponse(),
                HttpStatusCode.InternalServerError,
                "finance unavailable"));

        var result = await _service.CreatePaymentMetadata(new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = Guid.NewGuid().ToString(),
            PaymentDetails = [payment]
        }, CancellationToken.None);

        result.Status.Should().Be("Failed");
        result.MetadataCreated.Should().Be(0);
        result.Message.Should().Be("Created 0 payment metadata staging rows. Failed 1.");
    }

    private static Payment CreatePayment(Guid paymentId)
    {
        return new Payment
        {
            Id = paymentId.ToString(),
            Ukprn = 10000494,
            Uln = 9908090305,
            EmployerAccountId = "12345",
            ApprenticeshipId = 9876,
            FundingSource = FundingSource.Levy,
            Amount = 100,
            EvidenceSubmittedOn = new DateTime(2025, 1, 1),
            CollectionPeriod = new NamedCalendarPeriod
            {
                Id = "2526-R01",
                Month = 8,
                Year = 2025
            },
            DeliveryPeriod = new SFA.DAS.Provider.Events.Api.Types.CalendarPeriod
            {
                Month = 8,
                Year = 2025
            }
        };
    }
}
