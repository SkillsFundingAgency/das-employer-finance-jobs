using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class ImportPaymentsTelemetry(TelemetryClient telemetryClient) : IImportPaymentsTelemetry
{
    public const string AccountCompletedEventName = "ImportPayments.AccountCompleted";
    public const string AccountFailedEventName = "ImportPayments.AccountFailed";
    public const string AccountsCompletedMetricName = "ImportPayments.AccountsCompleted";
    public const string AccountsFailedMetricName = "ImportPayments.AccountsFailed";
    public const string PaymentsProcessedMetricName = "ImportPayments.PaymentsProcessed";
    public const string TransfersProcessedMetricName = "ImportPayments.TransfersProcessed";

    public void TrackAccountCompleted(
        string periodEndRef,
        string correlationId,
        long accountId,
        int paymentsProcessed,
        int transfersProcessed)
    {
        var periodEnd = periodEndRef ?? string.Empty;

        var telemetry = new EventTelemetry(AccountCompletedEventName);
        telemetry.Properties["PeriodEndRef"] = periodEnd;
        telemetry.Properties["CorrelationId"] = correlationId ?? string.Empty;
        telemetry.Properties["AccountId"] = accountId.ToString();
        telemetry.Metrics["PaymentsProcessed"] = paymentsProcessed;
        telemetry.Metrics["TransfersProcessed"] = transfersProcessed;
        telemetryClient.TrackEvent(telemetry);

        telemetryClient.GetMetric(AccountsCompletedMetricName, "PeriodEndRef").TrackValue(1, periodEnd);
        telemetryClient.GetMetric(PaymentsProcessedMetricName, "PeriodEndRef").TrackValue(paymentsProcessed, periodEnd);
        telemetryClient.GetMetric(TransfersProcessedMetricName, "PeriodEndRef").TrackValue(transfersProcessed, periodEnd);
    }

    public void TrackAccountFailed(
        string periodEndRef,
        string correlationId,
        long accountId)
    {
        var periodEnd = periodEndRef ?? string.Empty;

        var telemetry = new EventTelemetry(AccountFailedEventName);
        telemetry.Properties["PeriodEndRef"] = periodEnd;
        telemetry.Properties["CorrelationId"] = correlationId ?? string.Empty;
        telemetry.Properties["AccountId"] = accountId.ToString();
        telemetryClient.TrackEvent(telemetry);

        telemetryClient.GetMetric(AccountsFailedMetricName, "PeriodEndRef").TrackValue(1, periodEnd);
    }
}
