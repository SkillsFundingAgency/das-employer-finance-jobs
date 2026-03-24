using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessPeriodEndInput
{
    public string PeriodId { get; set; }
    public DateTime AccountDataValidAt { get; set; }
    public DateTime CommitmentDataValidAt { get; set; }
}
