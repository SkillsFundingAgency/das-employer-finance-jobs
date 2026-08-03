using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class TransferStagedToOperationalService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ImportPaymentsOptions importPaymentsOptions,
    ILogger<TransferStagedToOperationalService> logger) : ITransferStagedToOperationalService
{
    public async Task<TransferStagedToOperationalResult> Process(TransferStagedToOperationalInput input)
    {
        if (!importPaymentsOptions.TransferStagedToOperationalProcessingEnabled)
        {
            const string message = "Transfer staged-to-operational processing is disabled. Skipping operational transfer until the Finance API endpoint and TransferStagedToOperational procedure are complete.";
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] {Message} AccountId {AccountId}, PeriodEnd {PeriodEndRef}.",
                input.CorrelationId,
                message,
                input.AccountId,
                input.PeriodEndRef);

            return new TransferStagedToOperationalResult
            {
                TransfersProcessed = 0,
                Status = "Skipped",
                Message = message
            };
        }

        var requestModel = new TransferStagedToOperationalRequest
        {
            AccountId = input.AccountId,
            PeriodEnd = input.PeriodEndRef,
            CorrelationId = input.CorrelationId
        };

        var request = new PostTransferStagedToOperationalRequest(requestModel);
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Calling Finance API to transfer staged data to operational for AccountId {AccountId}, PeriodEnd {PeriodEndRef}.",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef);

        var response = await financeApiClient.PostWithResponseCode<PostTransferStagedToOperationalResponse>(request);
        if (response == null)
        {
            return FailedResult("No response received from Finance API while transferring staged data to operational.", input);
        }

        if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
        {
            return FailedResult(
                $"Finance API returned {response.StatusCode} while transferring staged data to operational. Error: {response.ErrorContent}",
                input);
        }

        var responseBody = response.Body;
        if (responseBody == null)
        {
            return FailedResult(
                $"Finance API returned {response.StatusCode} but no response body while transferring staged data to operational.",
                input);
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Finance API transferred {ProcessedCount} staged transfers to operational for AccountId {AccountId}, PeriodEnd {PeriodEndRef}.",
            input.CorrelationId,
            responseBody.ProcessedCount,
            input.AccountId,
            input.PeriodEndRef);

        return new TransferStagedToOperationalResult
        {
            TransfersProcessed = responseBody.ProcessedCount,
            Status = "Succeeded",
            Message = responseBody.Message ?? $"Successfully transferred {responseBody.ProcessedCount} staged transfers to operational."
        };
    }

    private TransferStagedToOperationalResult FailedResult(string message, TransferStagedToOperationalInput input)
    {
        logger.LogError(
            new InvalidOperationException(message),
            "[CorrelationId: {CorrelationId}] {ErrorMessage} AccountId {AccountId}, PeriodEnd {PeriodEndRef}.",
            input.CorrelationId,
            message,
            input.AccountId,
            input.PeriodEndRef);

        return new TransferStagedToOperationalResult
        {
            TransfersProcessed = 0,
            Status = "Failed",
            Message = message
        };
    }
}
