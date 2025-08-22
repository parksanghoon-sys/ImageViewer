using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 로그인 요청 DTO
/// 클라이언트에서 서버로 로그인 정보를 전송할 때 사용
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// 사용자 이메일 (로그인 ID)
    /// </summary>
    [Required(ErrorMessage = "이메일은 필수입니다.")]
    [EmailAddress(ErrorMessage = "올바른 이메일 형식이 아닙니다.")]
    [MaxLength(320, ErrorMessage = "이메일은 320자를 초과할 수 없습니다.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 사용자 비밀번호
    /// </summary>
    [Required(ErrorMessage = "비밀번호는 필수입니다.")]
    [MinLength(8, ErrorMessage = "비밀번호는 최소 8자 이상이어야 합니다.")]
    [MaxLength(100, ErrorMessage = "비밀번호는 100자를 초과할 수 없습니다.")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// 로그인 상태 유지 여부
    /// </summary>
    public bool RememberMe { get; init; } = false;
}