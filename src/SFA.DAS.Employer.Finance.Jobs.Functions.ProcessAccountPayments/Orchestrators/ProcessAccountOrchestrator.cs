using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
public class ProcessAccountOrchestrator(
    ILogger<ProcessAccountOrchestrator> logger,
    IRefreshPaymentDataService refreshPaymentDataService,
    IPaymentTransactionLinesService paymentTransactionLinesService)
{
    [Function("ProcessAccountOrchestrator")]
    public async Task<AccountProcessingResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessAccountInput>();
        var correlationId = input?.CorrelationId ?? Guid.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator started", correlationId);

        var result = new AccountProcessingResult
        {
            AccountId = input?.AccountId ?? 0,
            Success = true,
            PaymentsProcessed = 0,
            TransfersProcessed = 0
        };
        try
        {
            //calling RefreshPaymentDataActivity
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling activity RefreshPaymentDataActivity", correlationId);
            var refreshPaymentDataActivityInput = new RefreshPaymentDataInput
            {
                AccountId = input.AccountId,
                PeriodEnd = input.PeriodEndRef,
                CorrelationId = correlationId,
                IdempotencyKey = input.IdempotencyKey,
            };
            var refreshPaymentsResult = await context.CallActivityAsync<RefreshPaymentDataResult>(nameof(RefreshPaymentDataActivity), refreshPaymentDataActivityInput);

            //calling CreatePaymentTransactionLinesActivity
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling activity CreatePaymentTransactionLinesActivity", correlationId);
            var createTransactionLinesActivityInput = new CreatePaymentTransactionLinesInput
            {
                AccountId = input.AccountId,
                PeriodEnd = input.PeriodEndRef,
                CorrelationId = correlationId,
                PaymentDetails = refreshPaymentsResult.PaymentDetails,
                IdempotencyKey = input.IdempotencyKey
            };
            var createTransactionLinesResult = await context.CallActivityAsync<CreatePaymentTransactionLinesResult>(nameof(CreatePaymentTransactionLinesActivity), createTransactionLinesActivityInput);

            result.PaymentsProcessed = createTransactionLinesResult.TransactionsCreated;
            result.Success = true;

            logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator completed successfully.", correlationId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator failed: {ErrorMessage}", correlationId, ex.Message);
        }
        return result;
    }

    [Function("RefreshPaymentDataActivity")]
    public async Task<RefreshPaymentDataResult> RefreshPaymentDataActivity([ActivityTrigger] RefreshPaymentDataInput input)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] RefreshPaymentDataActivity started", input.CorrelationId);
        return await refreshPaymentDataService.RefreshPaymentData(input);
    }

    [Function("CreatePaymentTransactionLinesActivity")]
    public async Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLinesActivity([ActivityTrigger] CreatePaymentTransactionLinesInput input)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] CreatePaymentTransactionLinesActivity started", input.CorrelationId);
        return await paymentTransactionLinesService.CreatePaymentTransactionLines(input);
    }
}