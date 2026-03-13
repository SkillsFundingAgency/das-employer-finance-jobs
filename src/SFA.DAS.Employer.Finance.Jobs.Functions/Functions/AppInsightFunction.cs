using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions;

public class AppInsightFunction
{
    private readonly ILogger _logger;

    public AppInsightFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AppInsightFunction>();
    }

    [Function("AppInsightFunction")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Log in app insight as test from Muhammed: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Log in app insight, Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}