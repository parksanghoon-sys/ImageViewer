using ImageViewer.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ImageViewer.Domain.Identity;

/// <summary>
/// Identity 기반 애플리케이션 사용자 엔티티
/// ASP.NET Core Identity를 사용한 인증/인가 관리
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// 사용자 역할
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// 계정 활성화 여부
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 마지막 로그인 시간
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 계정 생성일시
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 계정 수정일시
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// 마지막 로그인 기록 업데이트
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 계정 비활성화
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 계정 활성화
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 사용자 역할 변경
    /// </summary>
    /// <param name="newRole">새 역할</param>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 관리자인지 확인
    /// </summary>
    /// <returns>관리자 여부</returns>
    public bool IsAdmin()
    {
        return Role == UserRole.Admin;
    }

    /// <summary>
    /// 일반 사용자인지 확인
    /// </summary>
    /// <returns>일반 사용자 여부</returns>
    public bool IsUser()
    {
        return Role == UserRole.User;
    }

    /// <summary>
    /// 게스트인지 확인
    /// </summary>
    /// <returns>게스트 여부</returns>
    public bool IsGuest()
    {
        return Role == UserRole.Guest;
    }
}