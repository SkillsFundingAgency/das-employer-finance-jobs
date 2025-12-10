using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization to match the API client expectations
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure CORS to allow all origins for local development
app.UseCors();

// Provider Payment API endpoint: api/periodends
app.MapGet("/api/periodends", () =>
{
    var paymentPeriodEnds = new[]
    {
        new PaymentPeriodEnd
        {
            Id = "2324-R06",
            CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 1 },
            ReferenceData = new ReferenceData
            {
                AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
            },
            CompletionDateTime = DateTime.UtcNow.AddDays(-1),
            Links = new Links { PaymentsForPeriod = "https://api.example.com/payments/period/2324-R06" }
        },
        new PaymentPeriodEnd
        {
            Id = "2324-R07",
            CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 2 },
            ReferenceData = new ReferenceData
            {
                AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
            },
            CompletionDateTime = DateTime.UtcNow.AddDays(-1),
            Links = new Links { PaymentsForPeriod = "https://api.example.com/payments/period/2324-R07" }
        },
        new PaymentPeriodEnd
        {
            Id = "2324-R08",
            CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 3 },
            ReferenceData = new ReferenceData
            {
                AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
            },
            CompletionDateTime = DateTime.UtcNow,
            Links = new Links { PaymentsForPeriod = "https://api.example.com/payments/period/2324-R08" }
        },
        new PaymentPeriodEnd
        {
            Id = "2425-R01",
            CalendarPeriod = new CalendarPeriod { Year = 2024, Month = 8 },
            ReferenceData = new ReferenceData
            {
                AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5)
            },
            CompletionDateTime = DateTime.UtcNow,
            Links = new Links { PaymentsForPeriod = "https://api.example.com/payments/period/2425-R01" }
        }
    };

    return Results.Ok(paymentPeriodEnds);
});

// Finance API endpoint: api/period-ends
app.MapGet("/api/period-ends", () =>
{
    // Return some existing period ends to test filtering
    // 2324-R06 and 2324-R07 already exist, so 2324-R08 and 2425-R01 will be identified as "new"
    var financePeriodEnds = new[]
    {
        new PeriodEnd
        {
            Id = 1,
            PeriodEndId = "2324-R06",
            CalendarPeriodMonth = 1,
            CalendarPeriodYear = 2024,
            AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
            CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5),
            CompletionDateTime = DateTime.UtcNow.AddDays(-1),
            PaymentsForPeriod = "https://api.example.com/payments/period/2324-R06"
        },
        new PeriodEnd
        {
            Id = 2,
            PeriodEndId = "2324-R07",
            CalendarPeriodMonth = 2,
            CalendarPeriodYear = 2024,
            AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
            CommitmentDataValidAt = DateTime.UtcNow.AddDays(-5),
            CompletionDateTime = DateTime.UtcNow.AddDays(-1),
            PaymentsForPeriod = "https://api.example.com/payments/period/2324-R07"
        }
    };

    return Results.Ok(financePeriodEnds);
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run("http://localhost:5001");

// Models matching the API response structure
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

public class PeriodEnd
{
    public int Id { get; set; }
    public string PeriodEndId { get; set; } = string.Empty;
    public int CalendarPeriodMonth { get; set; }
    public int CalendarPeriodYear { get; set; }
    public DateTime? AccountDataValidAt { get; set; }
    public DateTime? CommitmentDataValidAt { get; set; }
    public DateTime? CompletionDateTime { get; set; }
    public string PaymentsForPeriod { get; set; } = string.Empty;
}

