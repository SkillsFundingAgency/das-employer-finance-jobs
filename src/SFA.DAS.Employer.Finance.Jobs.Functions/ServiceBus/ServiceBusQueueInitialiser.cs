using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ServiceBus;

public class ServiceBusQueueInitialiser(ServiceBusAdministrationClient adminClient, ILogger<ServiceBusQueueInitialiser> logger) : IHostedService
{
    private static readonly string[] QueuesToCreate =
    [
        QueueConstants.FundingProjectionQueue
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var queueName in QueuesToCreate)
        {
            if (await adminClient.QueueExistsAsync(queueName, cancellationToken))
                continue;

            var options = new CreateQueueOptions(queueName)
            {
                LockDuration = TimeSpan.FromMinutes(5),
                DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                DeadLetteringOnMessageExpiration = true,
                MaxDeliveryCount = 5,
                RequiresDuplicateDetection = false,
                RequiresSession = false
            };

            try
            {
                await adminClient.CreateQueueAsync(options, cancellationToken);
                logger.LogInformation("Created Service Bus queue '{QueueName}'", queueName);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                // Created by another instance of the function app in the meantime
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
