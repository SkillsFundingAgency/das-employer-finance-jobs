using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.EmployerFinance.Messages.Events;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class RefreshPaymentDataCompletedEventPublisher(
    RefreshPaymentDataCompletedEventOptions options,
    IConfiguration configuration,
    ILogger<RefreshPaymentDataCompletedEventPublisher> logger) : IRefreshPaymentDataCompletedEventPublisher, IAsyncDisposable
{
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;

    public async Task Publish(RefreshPaymentDataCompletedEvent refreshPaymentDataCompletedEvent, string correlationId, CancellationToken cancellationToken = default)
    {
        var topicName = options.GetTopicName();
        var payload = JsonSerializer.Serialize(refreshPaymentDataCompletedEvent);
        var message = new ServiceBusMessage(BinaryData.FromString(payload))
        {
            ContentType = "application/json",
            CorrelationId = correlationId,
            MessageId = $"{refreshPaymentDataCompletedEvent.AccountId}-{refreshPaymentDataCompletedEvent.PeriodEnd}-{refreshPaymentDataCompletedEvent.Created:O}-{refreshPaymentDataCompletedEvent.PaymentsProcessed}",
            Subject = nameof(RefreshPaymentDataCompletedEvent)
        };

        var messageType = typeof(RefreshPaymentDataCompletedEvent).FullName!;
        message.ApplicationProperties["NServiceBus.EnclosedMessageTypes"] = messageType;
        message.ApplicationProperties["NServiceBus.MessageIntent"] = "Publish";
        message.ApplicationProperties["NServiceBus.ContentType"] = "application/json";

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Publishing RefreshPaymentDataCompletedEvent to topic {TopicName} for AccountId {AccountId}, PeriodEnd {PeriodEnd}, PaymentsProcessed {PaymentsProcessed}.",
            correlationId,
            topicName,
            refreshPaymentDataCompletedEvent.AccountId,
            refreshPaymentDataCompletedEvent.PeriodEnd,
            refreshPaymentDataCompletedEvent.PaymentsProcessed);

        await GetSender().SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null)
        {
            await _sender.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }

    private ServiceBusSender GetSender()
    {
        _sender ??= GetClient().CreateSender(options.GetTopicName());
        return _sender;
    }

    private ServiceBusClient GetClient()
    {
        if (_client != null)
        {
            return _client;
        }

        var connectionString = FirstNonEmpty(options.ConnectionString, configuration["AzureWebJobsServiceBus"]);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _client = new ServiceBusClient(connectionString);
            return _client;
        }

        var fullyQualifiedNamespace = FirstNonEmpty(
            options.FullyQualifiedNamespace,
            configuration["AzureWebJobsServiceBus:fullyQualifiedNamespace"],
            configuration["AzureWebJobsServiceBus__fullyQualifiedNamespace"]);

        if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            throw new InvalidOperationException("No Azure Service Bus connection string or fully qualified namespace has been configured for RefreshPaymentDataCompletedEvent publishing.");
        }

        _client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        return _client;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
