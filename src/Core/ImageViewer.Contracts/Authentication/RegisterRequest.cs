using System.ComponentModel.DataAnnotations;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 회원가입 요청 DTO
/// 클라이언트에서 서버로 회원가입 정보를 전송할 때 사용
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// 사용자 이메일 (로그인 ID)
    /// </summary>
    [Required(ErrorMessage = "이메일은 필수입니다.")]
    [EmailAddress(ErrorMessage = "올바른 이메일 형식이 아닙니다.")]
    [MaxLength(320, ErrorMessage = "이메일은 320자를 초과할 수 없습니다.")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 사용자 이름 (표시명)
    /// </summary>
    [Required(ErrorMessage = "사용자명은 필수입니다.")]
    [MinLength(2, ErrorMessage = "사용자명은 최소 2자 이상이어야 합니다.")]
    [MaxLength(50, ErrorMessage = "사용자명은 50자를 초과할 수 없습니다.")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 사용자 비밀번호
    /// </summary>
    [Required(ErrorMessage = "비밀번호는 필수입니다.")]
    [MinLength(8, ErrorMessage = "비밀번호는 최소 8자 이상이어야 합니다.")]
    [MaxLength(100, ErrorMessage = "비밀번호는 100자를 초과할 수 없습니다.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$", 
        ErrorMessage = "비밀번호는 대문자, 소문자, 숫자, 특수문자를 각각 최소 1개씩 포함해야 합니다.")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// 비밀번호 확인
    /// </summary>
    [Required(ErrorMessage = "비밀번호 확인은 필수입니다.")]
    [Compare(nameof(Password), ErrorMessage = "비밀번호와 비밀번호 확인이 일치하지 않습니다.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>
    /// 사용자 역할 (기본값: User)
    /// Admin 역할은 기존 관리자만 생성 가능
    /// </summary>
    public UserRole Role { get; init; } = UserRole.User;
}