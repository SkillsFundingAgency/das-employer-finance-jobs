namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreatePaymentMetadataResult
{
    public int MetadataCreated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
