using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ImageViewer.GatewayService.Configuration;

namespace ImageViewer.GatewayService.Middleware;

/// <summary>
/// JWT 토큰 검증 미들웨어
/// Authorization 헤더의 JWT 토큰을 검증하고 사용자 정보를 추출합니다.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewaySettings _gatewaySettings;
    private readonly ILogger<JwtMiddleware> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtMiddleware(
        RequestDelegate next,
        GatewaySettings gatewaySettings,
        ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _gatewaySettings = gatewaySettings;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();

        // 토큰 검증 파라미터 설정
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_gatewaySettings.Jwt.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _gatewaySettings.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = _gatewaySettings.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // 시간 편차 허용 안함
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromHeader(context);
        
        if (!string.IsNullOrEmpty(token))
        {
            ValidateTokenAsync(context, token);
        }

        await _next(context);
    }

    /// <summary>
    /// Authorization 헤더에서 JWT 토큰을 추출합니다.
    /// </summary>
    /// <param name="context">HTTP 컨텍스트</param>
    /// <returns>추출된 토큰 또는 null</returns>
    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
            return null;

        // "Bearer " 접두사 제거
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }

    /// <summary>
    /// JWT 토큰을 검증하고 사용자 정보를 컨텍스트에 추가합니다.
    /// </summary>
    /// <param name="context">HTTP 컨텍스트</param>
    /// <param name="token">JWT 토큰</param>
    private void ValidateTokenAsync(HttpContext context, string token)
    {
        try
        {
            // 토큰 검증
            var principal = _tokenHandler.ValidateToken(
                token, 
                _tokenValidationParameters, 
                out SecurityToken validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                // 토큰이 올바른 알고리즘으로 생성되었는지 확인
                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("유효하지 않은 JWT 알고리즘: {Algorithm}", jwtToken.Header.Alg);
                    return;
                }

                // 사용자 정보를 컨텍스트에 추가
                context.User = principal;

                // 사용자 ID를 헤더에 추가 (백엔드 서비스에서 사용)
                var userId = principal.FindFirst("sub")?.Value ?? 
                           principal.FindFirst("nameid")?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Request.Headers["X-User-Id"] = userId;
                    _logger.LogDebug("사용자 ID가 헤더에 추가됨: {UserId}", userId);
                }

                // 사용자 이메일을 헤더에 추가
                var userEmail = principal.FindFirst("email")?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    context.Request.Headers["X-User-Email"] = userEmail;
                }

                // 사용자명을 헤더에 추가
                var username = principal.FindFirst("name")?.Value;
                if (!string.IsNullOrEmpty(username))
                {
                    context.Request.Headers["X-Username"] = username;
                }

                _logger.LogDebug("JWT 토큰 검증 성공: {Email}", userEmail);
            }
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("만료된 JWT 토큰: {Token}", token[..10] + "...");
            
            // 만료된 토큰에 대한 특별한 응답 헤더 추가
            context.Response.Headers["X-Token-Expired"] = "true";
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("유효하지 않은 JWT 서명: {Error}", ex.Message);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("JWT 토큰 검증 실패: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT 토큰 처리 중 예상치 못한 오류");
        }
    }
}