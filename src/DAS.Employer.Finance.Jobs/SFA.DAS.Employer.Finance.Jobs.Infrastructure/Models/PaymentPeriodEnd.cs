using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PaymentPeriodEnd
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("CalendarPeriod")]
    public CalendarPeriod CalendarPeriod { get; set; } = new CalendarPeriod();

    [JsonPropertyName("ReferenceData")]
    public ReferenceData ReferenceData { get; set; } = new ReferenceData();

    [JsonPropertyName("CompletionDateTime")]
    public DateTime CompletionDateTime { get; set; }

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new Links();
}

public class CalendarPeriod
{
    [JsonPropertyName("Month")]
    public int Month { get; set; }

    [JsonPropertyName("Year")]
    public int Year { get; set; }
}

public class ReferenceData
{
    [JsonPropertyName("AccountDataValidAt")]
    public DateTime AccountDataValidAt { get; set; }

    [JsonPropertyName("CommitmentDataValidAt")]
    public DateTime CommitmentDataValidAt { get; set; }
}

public class Links
{
    [JsonPropertyName("PaymentsForPeriod")]
    public string PaymentsForPeriod { get; set; } = string.Empty;
}