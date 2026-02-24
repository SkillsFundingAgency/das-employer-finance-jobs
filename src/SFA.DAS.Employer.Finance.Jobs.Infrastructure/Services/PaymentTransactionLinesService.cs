using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services
{
    public class PaymentTransactionLinesService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ILogger<PeriodEndService> logger) : IPaymentTransactionLinesService
    {
        public async Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLines(CreatePaymentTransactionLinesInput input)
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] CreatePaymentTransactionLinesActivity started", input.CorrelationId);

            //build payment transaction lines
            var newTransactionLines = BuildPaymentTransactionLines(input);

            var existingTransactionLines = await GetExistingTransactionLinesAsync(input.AccountId, input.PeriodEnd, input.CorrelationId);
            var transactionLinesToCreate = new List<PaymentTransactionLine>();
            if (existingTransactionLines != null && existingTransactionLines.Any())
            {
                //filtering the transactions
                transactionLinesToCreate = newTransactionLines.Where(n => !existingTransactionLines.Any(e => e.TransactionType == 3 &&
                                                                                       e.AccountId == n.AccountId &&
                                                                                       e.Ukprn == n.Ukprn &&
                                                                                       e.PeriodEnd == n.PeriodEnd)
                                                                     ).ToList();
            }
            else
            {
                transactionLinesToCreate = newTransactionLines;
            }
            //if no transctions return empty result
            if (!transactionLinesToCreate.Any())
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] No new transaction lines to create, returning empty result.", input.CorrelationId);
                return new CreatePaymentTransactionLinesResult();
            }

            //posting new transaction lines to Finance API
            var transactionsCreated = await PostTransactionLinesToStaging(transactionLinesToCreate, input.CorrelationId);
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully created {count} transaction lines in Finance API", input.CorrelationId, transactionsCreated);
            return new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = transactionsCreated,
                Transactions = transactionLinesToCreate
            };
        }

        private List<PaymentTransactionLine> BuildPaymentTransactionLines(CreatePaymentTransactionLinesInput input)
        {

            var transactionLines = input.PaymentDetails
                .GroupBy(p => new
                {
                    p.EmployerAccountId,
                    p.Ukprn,
                    p.DeliveryPeriod
                })
                .Select(group =>
                {
                    var representative = group.First();

                    return new PaymentTransactionLine
                    {
                        AccountId = group.Key.EmployerAccountId,
                        Ukprn = group.Key.Ukprn,
                        PeriodEnd = $"{group.Key.DeliveryPeriod.Month}-{group.Key.DeliveryPeriod.Year}",

                        // Financial Rules: Multiply by -1 for Debits
                        Amount = -group
                            .Where(p => (int)p.FundingSource == 1 || (int)p.FundingSource == 5)
                            .Sum(p => p.Amount),

                        SfaCoInvestmentAmount = -group
                            .Where(p => (int)p.FundingSource == 2)
                            .Sum(p => p.Amount),

                        EmployerCoInvestmentAmount = -group
                            .Where(p => (int)p.FundingSource == 3)
                            .Sum(p => p.Amount),

                        // Rule: Max(PeriodEnd.CompletionDateTime)
                        TransactionDate = group.Max(p => p.EvidenceSubmittedOn),
                        CollectionPeriod = $"{representative.CollectionPeriod.Year}-R{representative.CollectionPeriod.Month:D2}",
                        ApprenticeCount = group.Select(x => x.Uln).Distinct().Count(),
                        PaymentIds = group.Select(x => x.Id).Where(id => id != null).ToList()!
                    };
                })
                .ToList();
            return transactionLines;
        }

        private async Task<List<PaymentTransactionLine>> GetExistingTransactionLinesAsync(long accountId, string periodEnd, string correlationId)
        {
            try
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing existing transactions for AccountId:{accountId}, PeriodEnd: {periodEnd}", correlationId, accountId, periodEnd);

                var request = new GetExistinTransactionLinesRequest(accountId, periodEnd);

                var retryPolicy = GetExistingTransactionLinesFromFinanceApiRetryPolicy(correlationId);
                var response = await retryPolicy.ExecuteAsync(() => financeApiClient.GetWithResponseCode<List<PaymentTransactionLine>>(request));
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
        private async Task<int> PostTransactionLinesToStaging(List<PaymentTransactionLine> newTransactionLines, string correlationId)
        {
            try
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to create transaction lines", correlationId);

                var retryPolicy = GetPostPaymentsToStagingRetryPolicy(correlationId);

                var request = new PostTransactionLinesToStagingRequest<List<PaymentTransactionLine>>(newTransactionLines);
                var response = await retryPolicy.ExecuteAsync(() => financeApiClient.PostWithResponseCode<List<PaymentTransactionLine>, PostTransactionLinesToStagingResponse>(request, false));
                if (response == null)
                {
                    logger.LogWarning("[CorrelationId: {CorrelationId}] No response received from Finance API. Assuming no transaction lines created.", correlationId);
                    return 0;
                }
                if (response != null && response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogWarning("[CorrelationId: {CorrelationId}] Finance API returned {StatusCode} with error: {ErrorContent}. Assuming no transaction lines created.", correlationId, response.StatusCode, response.ErrorContent);
                    return 0;
                }
                var createTransactionsResponse = response?.Body;
                logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully created {Count} transaction lines.", correlationId, createTransactionsResponse?.TransactionsCreated ?? 0);

                return createTransactionsResponse?.TransactionsCreated ?? 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error in creating transaction lines in Finance API: {ErrorMessage}", correlationId, ex.Message);
                throw;
            }
        }
        #region Retry Policies

        private AsyncRetryPolicy<ApiResponse<List<PaymentTransactionLine>>> GetExistingTransactionLinesFromFinanceApiRetryPolicy(string correlationId)
        {
            return Policy.Handle<HttpRequestException>()
                        .Or<TaskCanceledException>()
                        .OrResult<ApiResponse<List<PaymentTransactionLine>>>(r => r == null || (int)r.StatusCode >= 500)
                        .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(2),
                            onRetry: (outcome, timespan, retryCount, context) =>
                            {
                                logger.LogWarning(
                                    "[CorrelationId: {CorrelationId}] Retry {RetryCount} getting transaction lines",
                                    correlationId,
                                    retryCount);
                            });
        }
        private AsyncRetryPolicy<ApiResponse<PostTransactionLinesToStagingResponse>> GetPostPaymentsToStagingRetryPolicy(string correlationId)
        {
            return Policy.Handle<HttpRequestException>()
                         .Or<TaskCanceledException>()
                         .OrResult<ApiResponse<PostTransactionLinesToStagingResponse>>(response => response == null || (int)response.StatusCode >= 500)
                         .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: retry => TimeSpan.FromSeconds(2),
                                            onRetry: (outcome, delay, retryCount, context) =>
                                            {
                                                logger.LogWarning(
                                                    "[CorrelationId: {CorrelationId}] Retry {RetryCount} calling Finance API. Reason: {Reason}",
                                                    correlationId,
                                                    retryCount,
                                                    outcome.Exception?.Message ??
                                                    outcome.Result?.ErrorContent);
                                            });
        }
        #endregion
    }
}
