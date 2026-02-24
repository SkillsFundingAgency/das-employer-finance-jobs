using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions
{
    public class PaymentTransactionLine
    {
        // Grouping Keys
        public string AccountId { get; set; }
        public long Ukprn { get; set; }
        public string PeriodEnd { get; set; }
        public int TransactionType { get; set; } = 3;
        public decimal Amount { get; set; } // FundingSource 1 & 5
        public decimal SfaCoInvestmentAmount { get; set; } // FundingSource 2
        public decimal EmployerCoInvestmentAmount { get; set; } // FundingSource 3
        public decimal TotalAmount => Amount + SfaCoInvestmentAmount + EmployerCoInvestmentAmount;
        public DateTime TransactionDate { get; set; }
        public string CollectionPeriod { get; set; } // Formatted Month/Year
        public int ApprenticeCount { get; set; }
        public List<string> PaymentIds { get; set; } = new();
    }
}
