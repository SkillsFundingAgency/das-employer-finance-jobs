using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Encoding;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services
{
    public class PaymentTransactionLinesService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    IEncodingService encodingService,
    ILogger<PeriodEndService> logger) : IPaymentTransactionLinesService
    {
        public async Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLines(CreatePaymentTransactionLinesInput input)
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] CreatePaymentTransactionLinesActivity started", input.CorrelationId);

            var newTransactionLines = BuildPaymentTransactionLines(input, input.CorrelationId);

            var existingTransactionLines = await GetExistingTransactionLinesAsync(input.AccountId, input.PeriodEnd, input.CorrelationId);
            var transactionLinesToCreate = new List<PaymentTransactionLine>();
            if (existingTransactionLines != null && existingTransactionLines.Any())
            {
                transactionLinesToCreate = newTransactionLines
                    .Where(newTransactionLine => !existingTransactionLines.Any(existingTransactionLine =>
                        IsSameTransactionLine(existingTransactionLine, newTransactionLine)))
                    .ToList();
            }
            else
            {
                transactionLinesToCreate = newTransactionLines;
            }
            if (!transactionLinesToCreate.Any())
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] No new transaction lines to create, returning empty result.", input.CorrelationId);
                return new CreatePaymentTransactionLinesResult
                {
                    TransactionsCreated = 0,
                    Transactions = new List<PaymentTransactionLine>(),
                    Status = "Succeeded",
                    Message = $"Successfully created 0 transaction lines in Finance API"
                };
            }

            var transactionsCreated = await PostTransactionLinesToStaging(transactionLinesToCreate, input.CorrelationId);
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully created {count} transaction lines in Finance API", input.CorrelationId, transactionsCreated);
            return new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = transactionsCreated,
                Transactions = transactionLinesToCreate,
                Status = "Succeeded",
                Message = $"Successfully created {transactionsCreated} transaction lines in Finance API"
            };
        }

        private static bool IsSameTransactionLine(PaymentTransactionLine existingTransactionLine, PaymentTransactionLine newTransactionLine)
        {
            return existingTransactionLine.TransactionType == newTransactionLine.TransactionType
                   && existingTransactionLine.AccountId == newTransactionLine.AccountId
                   && existingTransactionLine.PeriodEnd == newTransactionLine.PeriodEnd
                   && existingTransactionLine.Ukprn == newTransactionLine.Ukprn
                   && existingTransactionLine.Amount == newTransactionLine.Amount
                   && existingTransactionLine.SfaCoInvestmentAmount == newTransactionLine.SfaCoInvestmentAmount
                   && existingTransactionLine.EmployerCoInvestmentAmount == newTransactionLine.EmployerCoInvestmentAmount
                   && existingTransactionLine.TransactionDate == newTransactionLine.TransactionDate;
        }

        private List<PaymentTransactionLine> BuildPaymentTransactionLines(CreatePaymentTransactionLinesInput input, string correlationId)
        {
            try
            {
                var paymentGroups = input.PaymentDetails.GroupBy(p => new
                {
                    p.Ukprn,
                    p.CollectionPeriod.Id,
                    p.CollectionPeriod.Month,
                    p.CollectionPeriod.Year
                });
                if (paymentGroups != null && paymentGroups.Any())
                {
                    var transactionLines = paymentGroups.Select(group =>
                    {
                        return new PaymentTransactionLine
                        {
                            AccountId = input.AccountId,
                            Ukprn = group.Key.Ukprn,
                            TransactionType = 3,
                            // Financial Rules: Multiply by -1 for Debits
                            Amount = RoundTransactionAmount(-group
                                .Where(p => (int)p.FundingSource == 1 || (int)p.FundingSource == 5)
                                .Sum(p => p.Amount)),

                            SfaCoInvestmentAmount = RoundTransactionAmount(-group
                                .Where(p => (int)p.FundingSource == 2)
                                .Sum(p => p.Amount)),

                            EmployerCoInvestmentAmount = RoundTransactionAmount(-group
                                .Where(p => (int)p.FundingSource == 3)
                                .Sum(p => p.Amount)),

                            TransactionDate = group.Max(p => p.EvidenceSubmittedOn),
                            PeriodEnd = input.PeriodEnd,
                            DateCreated = DateTime.UtcNow,
                        };
                    }).ToList();
                    return transactionLines;
                }
                return new List<PaymentTransactionLine>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CorrelationId: {CorrelationId}]  Exception in creating transaction lines: {Message} ", correlationId, ex.Message);
                throw;
            }
        }

        private async Task<List<PaymentTransactionLine>> GetExistingTransactionLinesAsync(long accountId, string periodEnd, string correlationId)
        {
            try
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing existing transactions for AccountId:{accountId}, PeriodEnd: {periodEnd}", correlationId, accountId, periodEnd);
                var hashedAccountId = encodingService.Encode(accountId, EncodingType.AccountId);

                var request = new GetExistinTransactionLinesRequest(hashedAccountId, periodEnd);

                var response = await financeApiClient.GetWithResponseCode<List<PaymentTransactionLine>>(request);
                if (response == null)
                {
                    logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Finance API. Assuming no existing transactions for AccountId:{accountId}, PeriodEnd: {periodEnd}", correlationId, accountId, periodEnd);
                    return new List<PaymentTransactionLine>();
                }
                if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogWarning("[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} with error: {ErrorContent}. Assuming no existing transactions for AccountId:{accountId}, PeriodEnd: {periodEnd}", correlationId, response.StatusCode, response.ErrorContent, accountId, periodEnd);
                    return new List<PaymentTransactionLine>();
                }
                var transactionLines = response?.Body;
                logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} existing transactions from Finance API", correlationId, transactionLines?.Count ?? 0);

                return transactionLines ?? new List<PaymentTransactionLine>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving existing transactions from Finance API: {ErrorMessage}", correlationId, ex.Message);
                throw;
            }
        }

        private static decimal RoundTransactionAmount(decimal amount)
        {
            return Math.Round(amount, 4, MidpointRounding.AwayFromZero);
        }

        private async Task<int> PostTransactionLinesToStaging(List<PaymentTransactionLine> newTransactionLines, string correlationId)
        {
            try
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to create transaction lines", correlationId);
                var transactionLinesRequestModel = new TransactionLineStagingRequest
                {
                    TransactionLines = newTransactionLines
                };
                var request = new PostTransactionLinesToStagingRequest(transactionLinesRequestModel);
                var response = await financeApiClient.PostWithResponseCode<PostTransactionLinesToStagingResponse>(request);
                if (response == null)
                {
                    const string message = "No response received from Finance API while staging transaction lines.";
                    logger.LogError(
                        new InvalidOperationException(message),
                        "[CorrelationId: {CorrelationId}] {ErrorMessage}",
                        correlationId,
                        message);
                    throw new InvalidOperationException(message);
                }
                if (response != null && ((int)response.StatusCode < 200 || (int)response.StatusCode > 299))
                {
                    var message = $"Finance API returned {response.StatusCode} while staging transaction lines. Error: {response.ErrorContent}";
                    logger.LogError(
                        new InvalidOperationException(message),
                        "[CorrelationId: {CorrelationId}] {ErrorMessage}",
                        correlationId,
                        message);
                    throw new InvalidOperationException(message);
                }
                var createTransactionsResponse = response?.Body;
                logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully created {Count} transaction lines.", correlationId, createTransactionsResponse?.InsertedCount ?? 0);

                return createTransactionsResponse?.InsertedCount ?? 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error in creating transaction lines in Finance API: {ErrorMessage}", correlationId, ex.Message);
                throw;
            }
        }
    }
}
