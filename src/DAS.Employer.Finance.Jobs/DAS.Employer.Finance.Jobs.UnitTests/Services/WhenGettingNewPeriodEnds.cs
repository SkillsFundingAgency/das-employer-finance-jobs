using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenGettingNewPeriodEnds
{
    private Mock<IFinanceApiClient> _mockFinanceApiClient;
    private Mock<IProviderEventsApiClient> _mockProviderEventsApiClient;
    private Mock<ILogger<PeriodEndService>> _mockLogger;
    private PeriodEndService _periodEndService;

    [SetUp]
    public void SetUp()
    {
        _mockFinanceApiClient = new Mock<IFinanceApiClient>();
        _mockProviderEventsApiClient = new Mock<IProviderEventsApiClient>();
        _mockLogger = new Mock<ILogger<PeriodEndService>>();
        _periodEndService = new PeriodEndService(_mockFinanceApiClient.Object, _mockProviderEventsApiClient.Object, _mockLogger.Object);
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

        _mockProviderEventsApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient.Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(financePeriodEnds);

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().PeriodEndId.Should().Be("PE-002");

        _mockProviderEventsApiClient.Verify(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()), Times.Once);

        _mockFinanceApiClient.Verify(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>()), Times.Once);
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

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(financePeriodEnds);

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

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>())).ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task And_Payment_Period_Ends_Is_Null_Then_Returns_Empty_List()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        _mockProviderEventsApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync((List<PaymentPeriodEnd>)null);

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

        _mockProviderEventsApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

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

        _mockProviderEventsApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ReturnsAsync(paymentPeriodEnds);

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

        _mockProviderEventsApiClient.Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>())).ThrowsAsync(expectedException);

        // Act & Assert
        try
        {
            await _periodEndService.GetNewPeriodEndsAsync(correlationId);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Be("API Error");
        }
    }

    [Test]
    public async Task And_Finance_API_Throws_Exception_Then_Throws_Exception()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Finance API Error");

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()))
            .ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        try
        {
            await _periodEndService.GetNewPeriodEndsAsync(correlationId);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Be("Finance API Error");
        }
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

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()))
            .ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>()))
            .ReturnsAsync(new List<PeriodEnd>());

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

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()))
            .ReturnsAsync(paymentPeriodEnds);

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>()))
            .ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _periodEndService.GetNewPeriodEndsAsync(correlationId);

        // Assert 
        result.Should().NotBeNull();
        result.Count.Should().Be(1);

        // Verify logging: starting, retrieved and found messages are logged
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting to retrieve period ends from external APIs")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved") && v.ToString().Contains("period ends from Provider Events API")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Found") && v.ToString().Contains("new period ends to process")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Test]
    public async Task Then_Logs_Error_When_ProviderEventsApi_Throws()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Provider API failure");

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        try
        {
            await _periodEndService.GetNewPeriodEndsAsync(correlationId);
            Assert.Fail("Expected exception not thrown");
        }
        catch (InvalidOperationException)
        {
            // Verify that error was logged for Provider Events API failure
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving period ends from Provider Events API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }

    [Test]
    public async Task Then_Logs_Error_When_FinanceApi_Throws()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedException = new InvalidOperationException("Finance API failure");

        _mockProviderEventsApiClient
            .Setup(x => x.Get<List<PaymentPeriodEnd>>(It.IsAny<GetPaymentPeriodEndsRequest>()))
            .ReturnsAsync(new List<PaymentPeriodEnd>());

        _mockFinanceApiClient
            .Setup(x => x.Get<List<PeriodEnd>>(It.IsAny<GetFinancePeriodEndsRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        try
        {
            await _periodEndService.GetNewPeriodEndsAsync(correlationId);
            Assert.Fail("Expected exception not thrown");
        }
        catch (InvalidOperationException)
        {
            // Verify that error was logged for Finance API failure
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving period ends from Finance API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}

