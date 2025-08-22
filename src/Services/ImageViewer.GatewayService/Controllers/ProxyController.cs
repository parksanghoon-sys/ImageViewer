using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImageViewer.GatewayService.Configuration;
using ImageViewer.GatewayService.Services;
using System.Text;

namespace ImageViewer.GatewayService.Controllers;

/// <summary>
/// API Gateway 프록시 컨트롤러
/// 모든 요청을 적절한 백엔드 서비스로 라우팅합니다.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly GatewaySettings _gatewaySettings;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        IHttpClientService httpClientService,
        GatewaySettings gatewaySettings,
        ILogger<ProxyController> logger)
    {
        _httpClientService = httpClientService;
        _gatewaySettings = gatewaySettings;
        _logger = logger;
    }

    /// <summary>
    /// 인증 서비스로 프록시
    /// </summary>
    /// <param name="endpoint">엔드포인트</param>
    /// <returns>프록시된 응답</returns>
    [HttpGet("auth/{*endpoint}")]
    [HttpPost("auth/{*endpoint}")]
    [HttpPut("auth/{*endpoint}")]
    [HttpDelete("auth/{*endpoint}")]
    [HttpPatch("auth/{*endpoint}")]
    public async Task<IActionResult> ProxyAuth(string endpoint)
    {
        return await ProxyRequest(_gatewaySettings.Services.AuthService, $"api/auth/{endpoint}");
    }

    /// <summary>
    /// 이미지 서비스로 프록시 (인증 필요)
    /// </summary>
    /// <param name="endpoint">엔드포인트</param>
    /// <returns>프록시된 응답</returns>
    [Authorize]
    [HttpGet("images/{*endpoint}")]
    [HttpPost("images/{*endpoint}")]
    [HttpPut("images/{*endpoint}")]
    [HttpDelete("images/{*endpoint}")]
    [HttpPatch("images/{*endpoint}")]
    public async Task<IActionResult> ProxyImages(string endpoint)
    {
        return await ProxyRequest(_gatewaySettings.Services.ImageService, $"api/images/{endpoint}");
    }

    /// <summary>
    /// 공유 서비스로 프록시 (인증 필요)
    /// </summary>
    /// <param name="endpoint">엔드포인트</param>
    /// <returns>프록시된 응답</returns>
    [Authorize]
    [HttpGet("share/{*endpoint}")]
    [HttpPost("share/{*endpoint}")]
    [HttpPut("share/{*endpoint}")]
    [HttpDelete("share/{*endpoint}")]
    [HttpPatch("share/{*endpoint}")]
    public async Task<IActionResult> ProxyShare(string endpoint)
    {
        return await ProxyRequest(_gatewaySettings.Services.ShareService, $"api/share/{endpoint}");
    }

    /// <summary>
    /// 알림 서비스로 프록시 (인증 필요)
    /// </summary>
    /// <param name="endpoint">엔드포인트</param>
    /// <returns>프록시된 응답</returns>
    [Authorize]
    [HttpGet("notifications/{*endpoint}")]
    [HttpPost("notifications/{*endpoint}")]
    [HttpPut("notifications/{*endpoint}")]
    [HttpDelete("notifications/{*endpoint}")]
    [HttpPatch("notifications/{*endpoint}")]
    public async Task<IActionResult> ProxyNotifications(string endpoint)
    {
        return await ProxyRequest(_gatewaySettings.Services.NotificationService, $"api/notifications/{endpoint}");
    }

    /// <summary>
    /// 헬스체크 엔드포인트
    /// </summary>
    /// <returns>Gateway 상태</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "ImageViewer.GatewayService",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// 백엔드 서비스들의 헬스체크
    /// </summary>
    /// <returns>모든 서비스의 상태</returns>
    [HttpGet("health/services")]
    public async Task<IActionResult> HealthCheckServices()
    {
        var healthChecks = new Dictionary<string, object>();

        try
        {
            // Auth Service 헬스체크
            var authResponse = await _httpClientService.GetAsync(
                _gatewaySettings.Services.AuthService, "health");
            healthChecks["authService"] = new
            {
                status = authResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                statusCode = (int)authResponse.StatusCode
            };
        }
        catch (Exception ex)
        {
            healthChecks["authService"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }

        try
        {
            // Image Service 헬스체크
            var imageResponse = await _httpClientService.GetAsync(
                _gatewaySettings.Services.ImageService, "health");
            healthChecks["imageService"] = new
            {
                status = imageResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                statusCode = (int)imageResponse.StatusCode
            };
        }
        catch (Exception ex)
        {
            healthChecks["imageService"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }

        return Ok(new
        {
            gateway = "healthy",
            services = healthChecks,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 실제 프록시 요청을 처리합니다.
    /// </summary>
    /// <param name="serviceUrl">대상 서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <returns>프록시된 응답</returns>
    private async Task<IActionResult> ProxyRequest(string serviceUrl, string endpoint)
    {
        try
        {
            // 요청 헤더 복사 (Authorization 포함)
            var headers = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                if (header.Key.StartsWith("Authorization") ||
                    header.Key.StartsWith("Content-Type") ||
                    header.Key.StartsWith("Accept"))
                {
                    headers[header.Key] = header.Value.ToString();
                }
            }

            HttpContent? content = null;
            if (Request.ContentLength.HasValue && Request.ContentLength > 0)
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                content = new StringContent(body, Encoding.UTF8, Request.ContentType ?? "application/json");
            }

            // 쿼리 파라미터 추가
            var queryString = Request.QueryString.ToString();
            var fullEndpoint = endpoint + queryString;

            HttpResponseMessage response = Request.Method.ToUpper() switch
            {
                "GET" => await _httpClientService.GetAsync(serviceUrl, fullEndpoint, headers),
                "POST" => await _httpClientService.PostAsync(serviceUrl, fullEndpoint, content, headers),
                "PUT" => await _httpClientService.PutAsync(serviceUrl, fullEndpoint, content, headers),
                "DELETE" => await _httpClientService.DeleteAsync(serviceUrl, fullEndpoint, headers),
                "PATCH" => await _httpClientService.PatchAsync(serviceUrl, fullEndpoint, content, headers),
                _ => throw new NotSupportedException($"HTTP 메소드 '{Request.Method}'는 지원되지 않습니다.")
            };

            // 응답 헤더 복사
            foreach (var header in response.Headers)
            {
                if (!Response.Headers.ContainsKey(header.Key))
                {
                    Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                }
            }

            // Content 헤더 복사
            foreach (var header in response.Content.Headers)
            {
                if (!Response.Headers.ContainsKey(header.Key))
                {
                    Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                }
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug(
                "프록시 요청 완료: {Method} {Endpoint} -> {StatusCode}",
                Request.Method,
                fullEndpoint,
                response.StatusCode);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "프록시 요청 중 오류 발생: {Method} {Endpoint}", Request.Method, endpoint);
            
            return StatusCode(500, new
            {
                error = "Gateway 오류가 발생했습니다.",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}