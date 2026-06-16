namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class GetAccountPaymentIdsResponse
{
    public List<string> PaymentIds { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
