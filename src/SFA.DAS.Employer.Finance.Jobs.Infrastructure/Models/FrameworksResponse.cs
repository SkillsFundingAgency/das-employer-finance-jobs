namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class FrameworksResponse
{
    public List<FrameworkResponse> Frameworks { get; set; } = [];
}

public class FrameworkResponse
{
    public string? FrameworkName { get; set; }
    public string? PathwayName { get; set; }
    public int Level { get; set; }
    public int FrameworkCode { get; set; }
    public int ProgType { get; set; }
    public int PathwayCode { get; set; }
}
