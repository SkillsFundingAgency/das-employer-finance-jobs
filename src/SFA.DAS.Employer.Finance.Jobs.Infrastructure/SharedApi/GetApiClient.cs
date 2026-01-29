using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi
{
    public abstract class GetApiClient<T> : IGetApiClient<T> where T : IApiConfiguration
    {
        protected readonly HttpClient HttpClient;
        protected readonly T Configuration;

        public GetApiClient(
            IHttpClientFactory httpClientFactory,
            T apiConfiguration)
        {
            HttpClient = httpClientFactory.CreateClient();
            HttpClient.BaseAddress = new Uri(apiConfiguration.Url);
            Configuration = apiConfiguration;
        }

        public async Task<TResponse> Get<TResponse>(IApiRequest request)
        {
            var result = await GetWithResponseCode<TResponse>(request);

            if (IsNot200RangeResponseCode(result.StatusCode))
            {
                return default;
            }

            return result.Body;
        }     

        public async Task<HttpStatusCode> GetResponseCode(IApiRequest request)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request.GetUrl);
            httpRequestMessage.AddVersion(request.Version);
            await AddAuthenticationHeader(httpRequestMessage);

            var response = await HttpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

            return response.StatusCode;
        }

        public async Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IApiRequest request)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request.GetUrl);
            httpRequestMessage.AddVersion(request.Version);
            await AddAuthenticationHeader(httpRequestMessage);

            var response = await HttpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var errorContent = "";
            var responseBody = (TResponse)default;

            if (IsNot200RangeResponseCode(response.StatusCode))
            {
                errorContent = json;
            }
            else if (string.IsNullOrWhiteSpace(json))
            {
                // 204 No Content from a potential returned null
                // Will throw if attempts to deserialise but didn't
                // feel right making it part of the error if branch
                // even if there is no content.
            }
            else
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter());
                responseBody = JsonSerializer.Deserialize<TResponse>(json, options);
            }

            var getWithResponseCode = new ApiResponse<TResponse>(responseBody, response.StatusCode, errorContent, GetHeaders(response));

            return getWithResponseCode;
        }

        public async Task<TResponse> Post<TResponse>(IApiRequest request)
        {
            var result = await PostWithResponseCode<TResponse>(request);

            if (IsNot200RangeResponseCode(result.StatusCode))
            {
                return default;
            }

            return result.Body;
        }

        public async Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, request.GetUrl);

            httpRequestMessage.AddVersion(request.Version);

            var json = JsonSerializer.Serialize(request.Data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            httpRequestMessage.Content =
                new StringContent(json, Encoding.UTF8, "application/json");

            await AddAuthenticationHeader(httpRequestMessage);

            var response = await HttpClient.SendAsync(httpRequestMessage)
                                           .ConfigureAwait(false);

            var responseJson = await response.Content
                                             .ReadAsStringAsync()
                                             .ConfigureAwait(false);

            var errorContent = string.Empty;
            var responseBody = default(TResponse);

            if (IsNot200RangeResponseCode(response.StatusCode))
            {
                errorContent = responseJson;
            }
            else if (!string.IsNullOrWhiteSpace(responseJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new JsonStringEnumConverter());

                responseBody = JsonSerializer.Deserialize<TResponse>(responseJson, options);
            }

            return new ApiResponse<TResponse>(
                responseBody,
                response.StatusCode,
                errorContent,
                GetHeaders(response));
        }



        private static bool IsNot200RangeResponseCode(HttpStatusCode statusCode)
        {
            return !((int)statusCode >= 200 && (int)statusCode <= 299);
        }

        protected abstract Task AddAuthenticationHeader(HttpRequestMessage httpRequestMessage);

        private static Dictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage == null || httpResponseMessage?.Headers == null || !httpResponseMessage.Headers.Any())
            {
                return new Dictionary<string, IEnumerable<string>>();
            }
            return httpResponseMessage.Headers.ToDictionary(h => h.Key, h => h.Value);
        }
    }
}