using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class PaymentMetadataActivities(
    ILogger<PaymentMetadataActivities> logger,
    IServiceProvider serviceProvider)
{
    [Function(nameof(CreatePaymentMetadataActivity))]
    public async Task<CreatePaymentMetadataResult> CreatePaymentMetadataActivity([ActivityTrigger] CreatePaymentMetadataInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] CreatePaymentMetadataActivity starting for AccountId {AccountId}. Payments: {PaymentCount}",
            input.CorrelationId,
            input.AccountId,
            input.PaymentDetails.Count);

        CreatePaymentMetadataResult result;
        try
        {
            var paymentMetadataService = serviceProvider.GetRequiredService<IPaymentMetadataService>();
            result = await paymentMetadataService.CreatePaymentMetadata(input, CancellationToken.None);
        }
        catch (Exception ex) when (ex is UriFormatException || ex.InnerException is UriFormatException)
        {
            logger.LogWarning(
                "[CorrelationId: {CorrelationId}] CreatePaymentMetadataActivity could not start for AccountId {AccountId} because required API configuration is invalid. Message: {Message}",
                input.CorrelationId,
                input.AccountId,
                ex.Message);

            result = new CreatePaymentMetadataResult
            {
                Status = "Failed",
                Message = ex.Message
            };
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] CreatePaymentMetadataActivity completed for AccountId {AccountId}. Status: {Status}. Message: {Message}",
            input.CorrelationId,
            input.AccountId,
            result.Status,
            result.Message);

        return result;
    }
}
