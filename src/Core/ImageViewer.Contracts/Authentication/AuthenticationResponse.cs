namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 인증 성공 응답 DTO
/// 로그인/토큰 갱신 성공 시 클라이언트에게 반환되는 정보
/// </summary>
public record AuthenticationResponse
{
    /// <summary>
    /// 사용자 정보
    /// </summary>
    public UserResponse User { get; init; } = new();

    /// <summary>
    /// 사용자 고유 ID (User.Id와 동일, 호환성을 위해 유지)
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 사용자 이메일 (User.Email과 동일, 호환성을 위해 유지)
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 사용자 이름 (User.Username과 동일, 호환성을 위해 유지)
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// JWT 액세스 토큰
    /// API 요청 시 Authorization 헤더에 포함
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// JWT 리프레시 토큰
    /// 액세스 토큰 갱신 시 사용
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// 액세스 토큰 만료 시간 (UTC)
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// 리프레시 토큰 만료 시간 (UTC)
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// 토큰 타입 (일반적으로 "Bearer")
    /// </summary>
    public string TokenType { get; init; } = "Bearer";
}