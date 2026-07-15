using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class CoursesApiClient(
    IInternalApiClient<CoursesApiConfiguration> apiClient,
    ILogger<CoursesApiClient> logger) : ICoursesApiClient
{
    public async Task<StandardsResponse?> GetStandards()
    {
        try
        {
            var response = await apiClient.Get<CoursesSearchApiResponse>(new GetCoursesSearchRequest());
            if (response?.Courses == null)
            {
                return null;
            }

            return new StandardsResponse
            {
                Standards = response.Courses
                    .Select(MapStandard)
                    .Where(standard => standard != null)
                    .Select(standard => standard!)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get standards from Courses API.");
            return null;
        }
    }

    public async Task<FrameworksResponse?> GetFrameworks()
    {
        try
        {
            return await apiClient.Get<FrameworksResponse>(new GetCoursesFrameworksRequest());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get frameworks from Courses API.");
            return null;
        }
    }

    private static StandardResponse? MapStandard(CourseApiItem course)
    {
        if (!int.TryParse(course.LarsCode, out var larsCode))
        {
            return null;
        }

        return new StandardResponse
        {
            Id = larsCode,
            Title = course.Title,
            Level = course.Level,
            LearningType = course.LearningType
        };
    }
}
