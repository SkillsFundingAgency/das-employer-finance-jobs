using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenTrackingImportPaymentsTelemetry
{
    private readonly List<ITelemetry> _sent = [];
    private TelemetryClient _telemetryClient = null!;
    private ImportPaymentsTelemetry _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sent.Clear();
        var configuration = new TelemetryConfiguration
        {
            TelemetryChannel = new StubTelemetryChannel(_sent),
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        };
        _telemetryClient = new TelemetryClient(configuration);
        _sut = new ImportPaymentsTelemetry(_telemetryClient);
    }

    [TearDown]
    public void TearDown()
    {
        _telemetryClient.Flush();
    }

    [Test]
    public void Then_Tracks_AccountCompleted_Event_And_Metrics()
    {
        _sut.TrackAccountCompleted("2425-R12", "corr-1", 14331, 5, 2);
        _telemetryClient.Flush();

        var completedEvent = _sent.OfType<EventTelemetry>()
            .Single(e => e.Name == ImportPaymentsTelemetry.AccountCompletedEventName);

        completedEvent.Properties["PeriodEndRef"].Should().Be("2425-R12");
        completedEvent.Properties["CorrelationId"].Should().Be("corr-1");
        completedEvent.Properties["AccountId"].Should().Be("14331");
        completedEvent.Metrics["PaymentsProcessed"].Should().Be(5);
        completedEvent.Metrics["TransfersProcessed"].Should().Be(2);

        _sent.OfType<MetricTelemetry>()
            .Any(m => m.Name == ImportPaymentsTelemetry.AccountsCompletedMetricName)
            .Should().BeTrue();
        _sent.OfType<MetricTelemetry>()
            .Any(m => m.Name == ImportPaymentsTelemetry.PaymentsProcessedMetricName)
            .Should().BeTrue();
        _sent.OfType<MetricTelemetry>()
            .Any(m => m.Name == ImportPaymentsTelemetry.TransfersProcessedMetricName)
            .Should().BeTrue();
    }

    [Test]
    public void Then_Tracks_AccountFailed_Event_And_Metric()
    {
        _sut.TrackAccountFailed("2425-R12", "corr-2", 99);
        _telemetryClient.Flush();

        var failedEvent = _sent.OfType<EventTelemetry>()
            .Single(e => e.Name == ImportPaymentsTelemetry.AccountFailedEventName);

        failedEvent.Properties["PeriodEndRef"].Should().Be("2425-R12");
        failedEvent.Properties["CorrelationId"].Should().Be("corr-2");
        failedEvent.Properties["AccountId"].Should().Be("99");

        _sent.OfType<MetricTelemetry>()
            .Any(m => m.Name == ImportPaymentsTelemetry.AccountsFailedMetricName)
            .Should().BeTrue();
    }

    private sealed class StubTelemetryChannel(List<ITelemetry> sent) : ITelemetryChannel
    {
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; } = string.Empty;

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void Send(ITelemetry item)
        {
            sent.Add(item);
        }
    }
}
