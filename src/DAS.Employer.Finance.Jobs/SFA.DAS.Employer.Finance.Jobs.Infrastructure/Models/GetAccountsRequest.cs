using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class GetAccountsRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
}
