using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;

public class ExpireFundsActivities(
    IAccountService accountService,
    IExpireFundsService expireFundsService,
    ILogger<ExpireFundsActivities> logger)
{
    [Function(nameof(GetAccountsPageActivity))]
    public Task<List<Accounts>> GetAccountsPageActivity([ActivityTrigger] GetAccountsRequest input) =>
        accountService.GetAccountsAsync(input);

    [Function(nameof(ProcessAccountExpireFundsActivity))]
    public async Task<ProcessAccountExpireFundsResult> ProcessAccountExpireFundsActivity(
        [ActivityTrigger] ProcessAccountExpireFundsInput input)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ProcessAccountExpireFundsActivity started for AccountId {AccountId}.",
            input.CorrelationId,
            input.AccountId);

        try
        {
            var response = await expireFundsService.ExpireFundsAsync(input.AccountId, input.CorrelationId);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ProcessAccountExpireFundsActivity succeeded for AccountId {AccountId}. FundsExpired {FundsExpired}.",
                input.CorrelationId,
                input.AccountId,
                response.FundsExpired);

            return new ProcessAccountExpireFundsResult
            {
                AccountId = input.AccountId,
                Success = true,
                FundsExpired = response.FundsExpired
            };
        }
        catch (Exception exception) when (ExpireFundsTransientErrorDetector.IsTransient(exception))
        {
            logger.LogWarning(
                exception,
                "[CorrelationId: {CorrelationId}] ProcessAccountExpireFundsActivity failed with a transient error for AccountId {AccountId}. Propagating for Durable Functions retry handling.",
                input.CorrelationId,
                input.AccountId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] ProcessAccountExpireFundsActivity failed with a non-transient error for AccountId {AccountId}.",
                input.CorrelationId,
                input.AccountId);

            return new ProcessAccountExpireFundsResult
            {
                AccountId = input.AccountId,
                Success = false,
                ErrorMessage = exception.Message
            };
        }
    }
}
