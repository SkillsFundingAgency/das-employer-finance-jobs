namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses
{
    public class PostTransactionLinesToStagingResponse
    {
        public int TransactionsCreated { get; set; }
        public string? Message { get; set; }
    }
}
