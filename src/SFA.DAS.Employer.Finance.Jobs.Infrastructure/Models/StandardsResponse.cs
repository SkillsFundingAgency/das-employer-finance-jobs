namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class StandardsResponse
{
    public List<StandardResponse> Standards { get; set; } = [];
}

public class StandardResponse
{
    public int Id { get; set; }
    public int Level { get; set; }
    public string? Title { get; set; }
    public string? LearningType { get; set; }
}
