namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses
{
    public abstract class Response
    {
        public bool IsValid { get; set; }
        public Exception Exception { get; set; }
    }
}
