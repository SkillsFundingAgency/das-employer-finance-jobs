using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;
public class WhenGettingNewPeriodEnds
{
    private Mock<IFinanceApiClient<FinanceInnerApiConfiguration>> _mockFinanceApiClient;
    private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _mockProviderPaymentApiClient;
    private Mock<ILogger<PeriodEndService>> _mockLogger;
    private PeriodEndService _periodEndService;

    [SetUp]
    public void SetUp()
    {
        _mockFinanceApiClient = new Mock<IFinanceApiClient<FinanceInnerApiConfiguration>>();
        _mockProviderPaymentApiClient = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
        _mockLogger = new Mock<ILogger<PeriodEndService>>();
        _periodEndService = new PeriodEndService(_mockFinanceApiClient.Object, _mockProviderPaymentApiClient.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Then_Returns_New_Period_Ends_Not_In_Finance_API()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
        {
            new PaymentPeriodEnd
            {
                Id = "PE-001",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 1 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                    CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-1" }
            },
            new PaymentPeriodEnd
            {
                Id = "PE-002",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 2 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                    CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-2" }
            }
        };

        var financePeriodEnds = new List<PeriodEnd>
        {
            new PeriodEnd { PeriodEndId = "PE-001" }
        };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(financePeriodEnds);

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().PeriodEndId.Should().Be("PE-002");

        _mockProviderPaymentApiClient.Verify();

        _mockFinanceApiClient.Verify();
    }

    [Test]
    public async Task And_All_Period_Ends_Already_Exist_Then_Returns_Empty_List()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
        {
            new PaymentPeriodEnd
            {
                Id = "PE-001",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 1 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow,
                    CommitmentDataValidAt = DateTime.UtcNow
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-1" }
            }
        };

        var financePeriodEnds = new List<PeriodEnd>
        {
            new PeriodEnd { PeriodEndId = "PE-001" }
        };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(financePeriodEnds);

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task And_No_Payment_Period_Ends_Then_Returns_Empty_List()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

   
    [Test]
    public async Task And_Finance_Period_Ends_Is_Null_Then_Returns_All_Payment_Period_Ends()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
        {
            new PaymentPeriodEnd
            {
                Id = "PE-001",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 1 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow,
                    CommitmentDataValidAt = DateTime.UtcNow
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-1" }
            }
        };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync((List<PeriodEnd>)null);

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().PeriodEndId.Should().Be("PE-001");
    }

    [Test]
    public async Task Then_Maps_PaymentPeriodEnd_To_PeriodEnd_Correctly()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var accountDataValidAt = DateTime.UtcNow.AddDays(-10);
        var commitmentDataValidAt = DateTime.UtcNow.AddDays(-5);
        var completionDateTime = DateTime.UtcNow;

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
        {
            new PaymentPeriodEnd
            {
                Id = "PE-001",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 3 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = accountDataValidAt,
                    CommitmentDataValidAt = commitmentDataValidAt
                },
                CompletionDateTime = completionDateTime,
                Links = new Links { PaymentsForPeriod = "test-link" }
            }
        };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);

        var periodEnd = result.First();
        periodEnd.PeriodEndId.Should().Be("PE-001");
        periodEnd.CalendarPeriodYear.Should().Be(2024);
        periodEnd.CalendarPeriodMonth.Should().Be(3);
        periodEnd.AccountDataValidAt.Should().Be(commitmentDataValidAt);
        periodEnd.CommitmentDataValidAt.Should().Be(accountDataValidAt);
        periodEnd.CompletionDateTime.Should().Be(completionDateTime);
        periodEnd.PaymentsForPeriod.Should().Be("test-link");
    }

    [Test]
    public async Task And_Provider_Events_API_Throws_Exception_Then_Throws_Exception()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("API Error");

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _periodEndService.GetNewPeriodEndsAsync(correlationId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("API Error");
    }

    [Test]
    public async Task And_Finance_API_Throws_Exception_Then_Throws_Exception()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Finance API Error");

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _periodEndService.GetNewPeriodEndsAsync(correlationId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Finance API Error");
    }

    [Test]
    public async Task And_Period_End_With_Empty_Id_Then_Filters_It_Out()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
        {
            new PaymentPeriodEnd
            {
                Id = "", // Empty ID
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 1 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow,
                    CommitmentDataValidAt = DateTime.UtcNow
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-1" }
            },
            new PaymentPeriodEnd
            {
                Id = "PE-001",
                CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 2 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = DateTime.UtcNow,
                    CommitmentDataValidAt = DateTime.UtcNow
                },
                CompletionDateTime = DateTime.UtcNow,
                Links = new Links { PaymentsForPeriod = "link-2" }
            }
        };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().PeriodEndId.Should().Be("PE-001");
    }
    [Test]
    public async Task Then_Logs_Information_Messages_For_Happy_Path()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        var paymentPeriodEnds = new List<PaymentPeriodEnd>
                                    {
                                        new PaymentPeriodEnd
                                        {
                                            Id = "PE-100",
                                            CalendarPeriod = new CalendarPeriod { Year = 2025, Month = 4 },
                                            ReferenceData = new ReferenceData
                                            {
                                                AccountDataValidAt = DateTime.UtcNow,
                                                CommitmentDataValidAt = DateTime.UtcNow
                                            },
                                            CompletionDateTime = DateTime.UtcNow,
                                            Links = new Links { PaymentsForPeriod = "payments-link" }
                                        }
                                    };

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(new List<PeriodEnd>());

        SetupExpectedInformationLogs();

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert 
        result.Should().NotBeNull();
        result.Count.Should().Be(1);

        // Verify all setups were called
        _mockLogger.Verify();
    }

    private void SetupExpectedInformationLogs()
    {
        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting to retrieve period ends from external APIs")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Calling Provider Events API to get period ends")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved") && v.ToString().Contains("period ends from payment period end API")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Calling Finance API to get existing period ends")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved") && v.ToString().Contains("period ends from Finance API")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved") && v.ToString().Contains("period ends from Provider Events API")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Filtered") && v.ToString().Contains("new period ends out of")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Found") && v.ToString().Contains("new period ends to process")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [Test]
    public async Task Then_Logs_Error_When_ProviderEventsApi_Throws()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Provider API failure");

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _periodEndService.GetNewPeriodEndsAsync(correlationId);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify 
        _mockLogger.Verify();
    }

    [Test]
    public async Task Then_Logs_Error_When_FinanceApi_Throws()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Finance API failure");

        _mockProviderPaymentApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ThrowsAsync(expectedException);

        _mockLogger.Setup(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving period ends from Finance API")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        // Act & Assert
        var act = async () => await _periodEndService.GetNewPeriodEndsAsync(correlationId);
        await act.Should().ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify();
    }
}