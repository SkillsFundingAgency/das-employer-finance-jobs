namespace SFA.DAS.EmployerFinance.Messages.Events;

public class RefreshPaymentDataCompletedEvent
{
    public long AccountId { get; set; }
    public string PeriodEnd { get; set; } = string.Empty;
    public bool PaymentsProcessed { get; set; }
    public DateTime Created { get; set; }
}
