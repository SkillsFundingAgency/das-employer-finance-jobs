namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IInternalApiConfiguration : IApiConfiguration
    {
        string Identifier { get; set; }
    }
}
