using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace API.Services
{
    public class ApiException : Exception
    {
        public HttpStatusCode ReturnCode { get; }

        public ApiException(HttpStatusCode returnCode)
            : base($"API Error with status code: {returnCode}")
        {
            ReturnCode = returnCode;
        }
    }

    public class ApiClient
    {
        private readonly string _baseUri;
        private readonly HttpClient _httpClient;

        protected ApiClient(string baseUri, Dictionary<string, string>? headers = null)
        {
            _baseUri = baseUri;
            _httpClient = new HttpClient();
            ApplyHeaders(headers);
        }

        protected async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? routeParams = null) =>
            await SendAsync<T>(HttpMethod.Get, endpoint, null, queryParams, routeParams);

        protected async Task<T?> PostAsync<T>(string endpoint, object? body, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? routeParams = null) =>
            await SendAsync<T>(HttpMethod.Post, endpoint, body, queryParams, routeParams);

        protected async Task<T?> PutAsync<T>(string endpoint, object? body, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? routeParams = null) =>
            await SendAsync<T>(HttpMethod.Put, endpoint, body, queryParams, routeParams);

        protected async Task<T?> DeleteAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? routeParams = null) =>
            await SendAsync<T>(HttpMethod.Delete, endpoint, null, queryParams, routeParams);

        private async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object? body, Dictionary<string, string>? queryParams, Dictionary<string, string>? routeParams)
        {
            var uri = BuildUri(endpoint, queryParams, routeParams);
            var request = new HttpRequestMessage(method, uri);
            
            if (body != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        private void ApplyHeaders(Dictionary<string, string>? headers)
        {
            if (headers == null) return;
            foreach (var header in headers)
            {
                if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                    _httpClient.DefaultRequestHeaders.Remove(header.Key);
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        private string BuildUri(string endpoint, Dictionary<string, string>? queryParams, Dictionary<string, string>? routeParams)
        {
            if (routeParams != null)
            {
                foreach (var param in routeParams)
                {
                    endpoint = endpoint.Replace($"{{{param.Key}}}", param.Value);
                }
            }

            var uriBuilder = new UriBuilder(new Uri(_baseUri + endpoint));

            if (queryParams != null)
            {
                var query = new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result; // This is still a potential blocking call. Consider refactoring if possible.
                uriBuilder.Query = query;
            }

            return uriBuilder.ToString();
        }
    }
}
