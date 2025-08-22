using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 토큰 갱신 요청 DTO
/// 액세스 토큰이 만료되었을 때 리프레시 토큰으로 새 토큰을 요청
/// </summary>
public record RefreshTokenRequest
{
    /// <summary>
    /// 리프레시 토큰
    /// </summary>
    [Required(ErrorMessage = "리프레시 토큰은 필수입니다.")]
    public string RefreshToken { get; init; } = string.Empty;
}