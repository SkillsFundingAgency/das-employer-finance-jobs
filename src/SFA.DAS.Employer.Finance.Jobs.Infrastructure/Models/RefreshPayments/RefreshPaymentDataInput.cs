using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments
{
    public class RefreshPaymentDataInput
    {
        public long AccountId { get; set; }
        public string PeriodEnd { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty; //Format: "account-{accountId}-period-{periodEnd}-payment-data"
    }
}
