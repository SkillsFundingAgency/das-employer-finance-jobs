using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Handler;

public class ProcessFinanceCommandHandler : IHandleMessages<ProcessFinanceCommand>
{
    private readonly ILogger _logger;

    public ProcessFinanceCommandHandler(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessFinanceCommandHandler>();
    }

    public Task Handle(ProcessFinanceCommand message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "Received ProcessFinanceCommand. JobId: {JobId}, QueuedAt: {QueuedAt}, Source: {Source}",
            message.JobId,
            message.QueuedAt,
            message.Source);

        // TODO:  processing logic will go here.
        return Task.CompletedTask;
    }
}
