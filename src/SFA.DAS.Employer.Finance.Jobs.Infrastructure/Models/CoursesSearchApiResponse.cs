namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CoursesSearchApiResponse
{
    public List<CourseApiItem> Courses { get; set; } = [];
}

public class CourseApiItem
{
    public string? LarsCode { get; set; }
    public string? Title { get; set; }
    public int Level { get; set; }
    public string? LearningType { get; set; }
}
