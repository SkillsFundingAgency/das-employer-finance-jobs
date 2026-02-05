using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments;

public class ProcessAccountPaymentsFunction
{
    private readonly ILogger<ProcessAccountPaymentsFunction> _logger;

    public ProcessAccountPaymentsFunction(ILogger<ProcessAccountPaymentsFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessAccountPaymentsFunction))]
    public async Task Run(
        [ServiceBusTrigger("SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments", Connection = "AzureWebJobsServiceBus")]
       ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}