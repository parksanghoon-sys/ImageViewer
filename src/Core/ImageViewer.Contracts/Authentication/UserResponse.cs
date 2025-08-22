using ImageViewer.Domain.Enums;

namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 사용자 정보 응답 DTO
/// </summary>
public class UserResponse
{
    /// <summary>
    /// 사용자 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 이메일 주소
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 사용자명
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 사용자 역할
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// 계정 활성화 상태
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 계정 생성일시
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
}