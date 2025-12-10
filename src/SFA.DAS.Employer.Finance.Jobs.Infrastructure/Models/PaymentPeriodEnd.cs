using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
public class PaymentPeriodEnd{
    
    public string Id { get; set; } = string.Empty;
   
    public CalendarPeriod CalendarPeriod { get; set; } = new CalendarPeriod();
   
    public ReferenceData ReferenceData { get; set; } = new ReferenceData();
    
    public DateTime CompletionDateTime { get; set; }

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new Links();
}
public class CalendarPeriod
{   
    public int Month { get; set; }
   
    public int Year { get; set; }
}

public class ReferenceData
{
     public DateTime AccountDataValidAt { get; set; }
  
    public DateTime CommitmentDataValidAt { get; set; }
}

public class Links
{    
    public string PaymentsForPeriod { get; set; } = string.Empty;
}