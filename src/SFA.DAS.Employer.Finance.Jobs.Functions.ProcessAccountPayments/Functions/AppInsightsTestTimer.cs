using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Functions;

public class AppInsightsTestTimer(ILogger<AppInsightsTestTimer> logger)
{
    [Function("AppInsightsTestTimer")]
    public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation("PAP App Insights test: timer fired at {UtcNow:O}", DateTime.UtcNow);
    }
}
