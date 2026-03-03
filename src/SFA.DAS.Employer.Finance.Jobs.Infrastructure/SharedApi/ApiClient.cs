using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi
{
    [ExcludeFromCodeCoverage]
    public abstract class ApiClient<T> : GetApiClient<T>, IApiClient<T> where T : IApiConfiguration
    {
        public static readonly JsonSerializerOptions JsonSerializationOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public ApiClient(IHttpClientFactory httpClientFactory, T apiConfiguration) : base(httpClientFactory, apiConfiguration)
        {
        }

        public async Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IPostApiRequest request, bool includeResponse = true)
        {
            return await PostWithResponseCode<object, TResponse>(request, includeResponse);
        }

        public async Task<ApiResponse<TResponse>> PostWithResponseCode<TData, TResponse>(IPostApiRequest<TData> request, bool includeResponse = true)
        {
            var stringContent = request.Data != null ? new StringContent(JsonSerializer.Serialize(request.Data), Encoding.UTF8, "application/json") : null;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, request.PostUrl);
            requestMessage.AddVersion(request.Version);
            requestMessage.Content = stringContent;
            await AddAuthenticationHeader(requestMessage);
            requestMessage.AddCorrelationId();

            var response = await HttpClient.SendAsync(requestMessage).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var errorContent = "";
            var responseBody = (TResponse)default!;

            if (IsNot200RangeResponseCode(response.StatusCode))
            {
                errorContent = json;
                HandleException(response, json);
            }
            else if (includeResponse)
            {
                responseBody = JsonSerializer.Deserialize<TResponse>(json, JsonSerializationOptions);
            }

            var postWithResponseCode = new ApiResponse<TResponse>(responseBody!, response.StatusCode, errorContent);

            return postWithResponseCode;
        }
        public virtual string HandleException(HttpResponseMessage response, string json)
        {
            return json;
        }
        private static bool IsNot200RangeResponseCode(HttpStatusCode statusCode)
        {
            return !((int)statusCode >= 200 && (int)statusCode <= 299);
        }
    }
}


