namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IImportPaymentsTelemetry
{
    void TrackAccountCompleted(
        string periodEndRef,
        string correlationId,
        long accountId,
        int paymentsProcessed,
        int transfersProcessed);

    void TrackAccountFailed(
        string periodEndRef,
        string correlationId,
        long accountId);
}
