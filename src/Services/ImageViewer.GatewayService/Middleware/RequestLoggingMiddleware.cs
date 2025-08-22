using System.Diagnostics;
using System.Text;

namespace ImageViewer.GatewayService.Middleware;

/// <summary>
/// 요청/응답 로깅 미들웨어
/// API Gateway를 통과하는 모든 요청과 응답을 로그로 기록합니다.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 요청 시작 시간 기록
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // 요청 정보 로깅
        await LogRequestAsync(context, requestId);

        // 응답 정보를 캡처하기 위해 응답 스트림 교체
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // 다음 미들웨어 실행
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // 응답 정보 로깅
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

            // 원본 응답 스트림으로 복사
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    /// <summary>
    /// 요청 정보를 로그에 기록합니다.
    /// </summary>
    /// <param name="context">HTTP 컨텍스트</param>
    /// <param name="requestId">요청 ID</param>
    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[{requestId}] 🚀 요청 시작");
        logBuilder.AppendLine($"  메소드: {request.Method}");
        logBuilder.AppendLine($"  URL: {request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
        logBuilder.AppendLine($"  IP: {context.Connection.RemoteIpAddress}");
        logBuilder.AppendLine($"  User-Agent: {request.Headers.UserAgent}");
        
        // Authorization 헤더가 있는지 확인 (토큰 값은 로그하지 않음)
        if (request.Headers.ContainsKey("Authorization"))
        {
            logBuilder.AppendLine($"  인증: Bearer 토큰 포함");
        }

        // Content-Type이 있는 경우
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            logBuilder.AppendLine($"  Content-Type: {request.ContentType}");
            logBuilder.AppendLine($"  Content-Length: {request.ContentLength ?? 0}");
        }

        // 요청 본문 로깅 (JSON인 경우만, 크기 제한)
        if (request.ContentLength.HasValue && request.ContentLength > 0 && 
            request.ContentLength < 10000 && // 10KB 이하만
            request.ContentType?.Contains("application/json") == true)
        {
            request.EnableBuffering();
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;
            
            if (!string.IsNullOrEmpty(body))
            {
                logBuilder.AppendLine($"  본문: {body}");
            }
        }

        _logger.LogInformation(logBuilder.ToString());
    }

    /// <summary>
    /// 응답 정보를 로그에 기록합니다.
    /// </summary>
    /// <param name="context">HTTP 컨텍스트</param>
    /// <param name="requestId">요청 ID</param>
    /// <param name="elapsedMs">소요 시간 (밀리초)</param>
    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
    {
        var response = context.Response;
        
        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[{requestId}] ✅ 응답 완료");
        logBuilder.AppendLine($"  상태코드: {response.StatusCode}");
        logBuilder.AppendLine($"  소요시간: {elapsedMs}ms");
        
        if (!string.IsNullOrEmpty(response.ContentType))
        {
            logBuilder.AppendLine($"  Content-Type: {response.ContentType}");
        }

        // 응답 본문 로깅 (JSON인 경우만, 크기 제한)
        if (response.Body.CanRead && response.Body.Length < 10000 && // 10KB 이하만
            response.ContentType?.Contains("application/json") == true)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            
            if (!string.IsNullOrEmpty(bodyText))
            {
                logBuilder.AppendLine($"  응답: {bodyText}");
            }
        }

        // 로그 레벨 결정 (에러 응답인 경우 Warning, 정상 응답인 경우 Information)
        if (response.StatusCode >= 400)
        {
            _logger.LogWarning(logBuilder.ToString());
        }
        else
        {
            _logger.LogInformation(logBuilder.ToString());
        }
    }
}