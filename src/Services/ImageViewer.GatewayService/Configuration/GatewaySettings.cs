namespace ImageViewer.GatewayService.Configuration;

/// <summary>
/// Gateway 설정 클래스
/// </summary>
public class GatewaySettings
{
    public const string SectionName = "Gateway";

    /// <summary>
    /// 백엔드 서비스 설정
    /// </summary>
    public ServiceEndpoints Services { get; set; } = new();

    /// <summary>
    /// JWT 설정
    /// </summary>
    public JwtSettings Jwt { get; set; } = new();

    /// <summary>
    /// CORS 설정
    /// </summary>
    public CorsSettings Cors { get; set; } = new();

    /// <summary>
    /// 재시도 정책 설정
    /// </summary>
    public RetrySettings Retry { get; set; } = new();
}

/// <summary>
/// 백엔드 서비스 엔드포인트 설정
/// </summary>
public class ServiceEndpoints
{
    /// <summary>
    /// 인증 서비스 URL
    /// </summary>
    public string AuthService { get; set; } = "http://localhost:5001";

    /// <summary>
    /// 이미지 서비스 URL
    /// </summary>
    public string ImageService { get; set; } = "http://localhost:5002";

    /// <summary>
    /// 공유 서비스 URL
    /// </summary>
    public string ShareService { get; set; } = "http://localhost:5003";

    /// <summary>
    /// 알림 서비스 URL
    /// </summary>
    public string NotificationService { get; set; } = "http://localhost:5004";
}

/// <summary>
/// JWT 설정
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT 비밀키
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 발급자
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 대상
    /// </summary>
    public string Audience { get; set; } = string.Empty;
}

/// <summary>
/// CORS 설정
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// 허용된 출처
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "http://localhost:3000" };

    /// <summary>
    /// 모든 출처 허용 여부
    /// </summary>
    public bool AllowAnyOrigin { get; set; } = false;

    /// <summary>
    /// 모든 헤더 허용 여부
    /// </summary>
    public bool AllowAnyHeader { get; set; } = true;

    /// <summary>
    /// 모든 메소드 허용 여부
    /// </summary>
    public bool AllowAnyMethod { get; set; } = true;

    /// <summary>
    /// 자격 증명 허용 여부
    /// </summary>
    public bool AllowCredentials { get; set; } = true;
}

/// <summary>
/// 재시도 정책 설정
/// </summary>
public class RetrySettings
{
    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 기본 지연 시간 (초)
    /// </summary>
    public double BaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 서킷 브레이커 실패 임계값
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// 서킷 브레이커 복구 시간 (초)
    /// </summary>
    public int CircuitBreakerRecoveryTimeSeconds { get; set; } = 60;
}