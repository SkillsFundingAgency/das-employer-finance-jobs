using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Functions;

public class FinanceLogTestTimer
{
    private readonly ILogger _logger;

    public FinanceLogTestTimer(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FinanceLogTestTimer>();
    }

    [Function("FinanceLogTestTimer")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}