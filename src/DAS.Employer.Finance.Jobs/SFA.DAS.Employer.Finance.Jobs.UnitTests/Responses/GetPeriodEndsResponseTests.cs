using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Responses
{
    [TestFixture]
    public class GetPeriodEndsResponseTests
    {
        [Test]
        public void When_Constructed_Then_PeriodEndsIsInitializedAsEmptyList()
        {
            // When
            var response = new GetPeriodEndsResponse();

            // Then
            response.PeriodEnds.Should().NotBeNull();
            response.PeriodEnds.Should().BeEmpty();
        }

        [Test]
        public void When_PeriodEndsIsSet_Then_PeriodEndsReturnsSetList()
        {
            // When
            var periodEnds = new List<PeriodEnd>
            {
                new PeriodEnd
                {
                    Id = 1,
                    PeriodEndId = "PE1",
                    CalendarPeriodMonth = 6,
                    CalendarPeriodYear = 2024,
                    AccountDataValidAt = DateTime.UtcNow,
                    CommitmentDataValidAt = DateTime.UtcNow.AddDays(-1),
                    CompletionDateTime = DateTime.UtcNow.AddDays(-2),
                    PaymentsForPeriod = "Payment1"
                }
            };

            var response = new GetPeriodEndsResponse
            {
                PeriodEnds = periodEnds
            };

            // Then
            response.PeriodEnds.Should().BeEquivalentTo(periodEnds);
        }

        [Test]
        public void When_AddingPeriodEnd_Then_PeriodEndsContainsNewItem()
        {
            // When
            var response = new GetPeriodEndsResponse();
            var periodEnd = new PeriodEnd
            {
                Id = 2,
                PeriodEndId = "PE2",
                CalendarPeriodMonth = 7,
                CalendarPeriodYear = 2025,
                AccountDataValidAt = DateTime.UtcNow,
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-1),
                CompletionDateTime = DateTime.UtcNow.AddDays(-2),
                PaymentsForPeriod = "Payment2"
            };
            response.PeriodEnds.Add(periodEnd);

            // Then
            response.PeriodEnds.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(periodEnd);
        }
    }
}
