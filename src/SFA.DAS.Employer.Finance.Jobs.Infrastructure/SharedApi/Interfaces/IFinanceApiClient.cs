namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IFinanceApiClient<T> : IInternalApiClient<T>
    {
        Task Post<TBody>(string url, TBody body);
    }
}