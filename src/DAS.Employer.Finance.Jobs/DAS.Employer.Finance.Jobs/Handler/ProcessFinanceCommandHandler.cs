using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Handler;

public class ProcessFinanceCommandHandler(ILogger<ProcessFinanceCommandHandler> logger) : IHandleMessages<ProcessFinanceCommand>
{
    // TODO : this will change when actual processing logic is added.
    public Task Handle(ProcessFinanceCommand message, IMessageHandlerContext context)
    {
        
        logger.LogInformation(
            "Received ProcessFinanceCommand. JobId: {JobId}, QueuedAt: {QueuedAt}, Source: {Source}",
            message.JobId,
            message.QueuedAt,
            message.Source);

        // TODO:  processing logic will go here.
        return Task.CompletedTask;
    }
}
