using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using ImageViewer.GatewayService.Configuration;

namespace ImageViewer.GatewayService.Services;

/// <summary>
/// HTTP 클라이언트 서비스 구현
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        GatewaySettings gatewaySettings)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 재시도 정책 설정 (단순화)
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                gatewaySettings.Retry.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(
                    Math.Pow(gatewaySettings.Retry.BaseDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "재시도 {RetryCount}/{MaxRetries} - {Delay}초 후 재시도. 이유: {Reason}",
                        retryCount,
                        gatewaySettings.Retry.MaxRetryAttempts,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                });
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetAsync(string serviceUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        var requestUri = $"{serviceUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddHeaders(request, headers);

            _logger.LogDebug("GET 요청: {RequestUri}", requestUri);
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("GET 응답: {StatusCode} from {RequestUri}", 
                response.StatusCode, requestUri);
            
            return response;
        });
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PostAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null)
    {
        var requestUri = $"{serviceUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content
            };
            AddHeaders(request, headers);

            _logger.LogDebug("POST 요청: {RequestUri}", requestUri);
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("POST 응답: {StatusCode} from {RequestUri}", 
                response.StatusCode, requestUri);
            
            return response;
        });
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PutAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null)
    {
        var requestUri = $"{serviceUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = content
            };
            AddHeaders(request, headers);

            _logger.LogDebug("PUT 요청: {RequestUri}", requestUri);
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("PUT 응답: {StatusCode} from {RequestUri}", 
                response.StatusCode, requestUri);
            
            return response;
        });
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DeleteAsync(string serviceUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        var requestUri = $"{serviceUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            AddHeaders(request, headers);

            _logger.LogDebug("DELETE 요청: {RequestUri}", requestUri);
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("DELETE 응답: {StatusCode} from {RequestUri}", 
                response.StatusCode, requestUri);
            
            return response;
        });
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PatchAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null)
    {
        var requestUri = $"{serviceUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = content
            };
            AddHeaders(request, headers);

            _logger.LogDebug("PATCH 요청: {RequestUri}", requestUri);
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("PATCH 응답: {StatusCode} from {RequestUri}", 
                response.StatusCode, requestUri);
            
            return response;
        });
    }

    /// <summary>
    /// 요청에 헤더를 추가합니다.
    /// </summary>
    /// <param name="request">HTTP 요청 메시지</param>
    /// <param name="headers">추가할 헤더들</param>
    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}