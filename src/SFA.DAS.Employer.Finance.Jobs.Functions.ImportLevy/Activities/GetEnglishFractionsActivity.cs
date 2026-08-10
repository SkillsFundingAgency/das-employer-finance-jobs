using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetEnglishFractionsActivity(
    IEnglishFractionsService englishFractionsService,
    ILogger<GetEnglishFractionsActivity> logger)
{
    [Function("GetEnglishFractionsActivity")]
    public async Task<EnglishFractionsFetchResult> Run([ActivityTrigger] GetEnglishFractionsActivityInput input)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Checking HMRC English fractions for PAYE {EmployerReference}",
            input.CorrelationId,
            input.EmployerReference);

        var result = await englishFractionsService.GetEnglishFractionsAsync(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] HMRC English fractions check for PAYE {EmployerReference} completed. UpdateRequired: {UpdateRequired}. Fractions returned: {FractionCount}",
            input.CorrelationId,
            input.EmployerReference,
            result.UpdateRequired,
            result.Fractions.Count);

        return result;
    }
}