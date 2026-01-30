using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Commands;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Handler
{
    public class WhenCallingImportAccountPaymentsCommandHandlerTests
    {
        private Mock<IProcessAccountOrchestrationStarter> _starterMock;
        private ImportAccountPaymentsCommandHandler _handler;
        [SetUp]
        public void SetUp()
        {
            _starterMock = new Mock<IProcessAccountOrchestrationStarter>();
            _handler = new ImportAccountPaymentsCommandHandler(Mock.Of<ILogger<ImportAccountPaymentsCommandHandler>>(), _starterMock.Object);
            
        }

        [Test]
        public async Task Then_Start_Orchestration_When_no_Exisiting_Instance()
        {
            // Arrange
              _starterMock.Setup(s => s.GetInstanceAsyc(It.IsAny<string>()))
                .ReturnsAsync((OrchestrationMetadata)null);
            var command = new ImportAccountPaymentsCommand
            {
                AccountId = 12345,
                PeriodEndRef = "2023-08"
            };

            // Act
            await _handler.Handle(command, Mock.Of<IMessageHandlerContext>());

            // Assert
            _starterMock.Verify(s => s.StartAsyc(
                "ProcessAccountOrchestrator",
                "ProcessAccountOrchestrator-Singleton",
                It.Is<ProcessAccountInput>(input =>
                    input.AccountId == command.AccountId &&
                    input.PeriodEndRef == command.PeriodEndRef &&                  
                    !string.IsNullOrEmpty(input.CorrelationId) &&                    
                    input.IdempotencyKey == "ProcessAccountOrchestrator-Singleton" &&                   
                    (DateTime.UtcNow - input.TriggeredAt) < TimeSpan.FromSeconds(5)
                ),
                It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
        [Test]
        public async Task Then_Does_Not_Start_Orchestration_When_Instance_Is_Running()
        {
            // Arrange
            var runningInstance = new OrchestrationMetadata("ProcessAccountOrchestrator","ProcessAccountOrchestrator-Singleton")
            {
                RuntimeStatus = OrchestrationRuntimeStatus.Running
            };

            _starterMock.Setup(s => s.GetInstanceAsyc(It.IsAny<string>()))
                .ReturnsAsync(runningInstance);

            var command = new ImportAccountPaymentsCommand
            {
                AccountId = 54321,
                PeriodEndRef = "2023-09"
            };

            // Act
            await _handler.Handle(command, Mock.Of<IMessageHandlerContext>());

            // Assert 
            _starterMock.Verify(s => s.StartAsyc(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProcessAccountInput>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}