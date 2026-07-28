using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class PaymentTransactionLineActivities(
                ILogger<PaymentTransactionLineActivities> logger,
                IPaymentTransactionLinesService paymentTransactionLinesService)
{
    [Function(nameof(CreatePaymentTransactionLinesActivity))]
    public async Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLinesActivity([ActivityTrigger] CreatePaymentTransactionLinesInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation("[CorrelationId: {CorrelationId}] CreatePaymentTransactionLinesActivity starting.", input.CorrelationId);

        var result = await paymentTransactionLinesService.CreatePaymentTransactionLines(input);

        logger.LogInformation("[CorrelationId: {CorrelationId}] CreatePaymentTransactionLinesActivity completed for AccountId: {AccountId} Status: {Status} Message: {Message}",
                input.CorrelationId,
                input.AccountId,
                result.Status,
                result.Message);

        return result;
    }

    [Function(nameof(CreateTransferTransactionLinesActivity))]
    public async Task<CreateTransferTransactionLinesResult> CreateTransferTransactionLinesActivity([ActivityTrigger] CreateTransferTransactionLinesInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation("[CorrelationId: {CorrelationId}] CreateTransferTransactionLinesActivity starting.", input.CorrelationId);

        var result = await paymentTransactionLinesService.CreateTransferTransactionLines(input);

        logger.LogInformation("[CorrelationId: {CorrelationId}] CreateTransferTransactionLinesActivity completed for PeriodEnd: {PeriodEnd} Status: {Status} Message: {Message}",
            input.CorrelationId,
            input.PeriodEnd,
            result.Status,
            result.Message);

        return result;
    }
}
