namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEnglishFractionCalculationDateWriteTracker
{
    bool TryStartWrite(string correlationId, DateTime calculationDate);
    void MarkWriteSucceeded(string correlationId, DateTime calculationDate);
    void MarkWriteFailed(string correlationId, DateTime calculationDate);
}
