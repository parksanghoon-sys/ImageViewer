using ImageViewer.Domain.Common;
using ImageViewer.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// 사용자 엔티티
/// 회원 가입, 로그인, 인증 관련 정보를 담는 도메인 모델
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// 사용자 이메일 (로그인 ID로 사용)
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// 사용자 이름 (표시명)
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// 비밀번호 해시
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// 비밀번호 솔트
    /// </summary>
    public string PasswordSalt { get; private set; } = string.Empty;

    /// <summary>
    /// 사용자 역할
    /// </summary>
    public UserRole Role { get; private set; } = UserRole.User;

    /// <summary>
    /// 계정 활성화 여부
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// 계정 공개 여부 (true: 누구나 이 사용자의 이미지를 볼 수 있음, false: 비공개)
    /// </summary>
    public bool IsPublic { get; private set; } = false;

    /// <summary>
    /// 마지막 로그인 시간
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// 이 사용자가 업로드한 이미지들
    /// </summary>
    public virtual ICollection<Image> Images { get; private set; } = new List<Image>();

    /// <summary>
    /// 이 사용자가 요청한 공유 요청들
    /// </summary>
    public virtual ICollection<ShareRequest> RequestedShares { get; private set; } = new List<ShareRequest>();

    /// <summary>
    /// 이 사용자에게 온 공유 요청들
    /// </summary>
    public virtual ICollection<ShareRequest> ReceivedShares { get; private set; } = new List<ShareRequest>();

    /// <summary>
    /// 기본 생성자 (EF Core용)
    /// </summary>
    protected User() { }

    /// <summary>
    /// 새 사용자 생성
    /// </summary>
    /// <param name="email">이메일</param>
    /// <param name="username">사용자명</param>
    /// <param name="passwordHash">비밀번호 해시</param>
    /// <param name="passwordSalt">비밀번호 솔트</param>
    /// <param name="role">사용자 역할 (기본값: User)</param>
    public User(string email, string username, string passwordHash, string passwordSalt, UserRole role = UserRole.User)
    {
        Id = Guid.NewGuid(); // 고유 GUID 생성
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        PasswordSalt = passwordSalt ?? throw new ArgumentNullException(nameof(passwordSalt));
        Role = role;
    }

    /// <summary>
    /// 사용자 정보 업데이트
    /// </summary>
    /// <param name="username">새 사용자명</param>
    public void UpdateProfile(string username)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        MarkAsModified();
    }

    /// <summary>
    /// 비밀번호 변경
    /// </summary>
    /// <param name="newPasswordHash">새 비밀번호 해시</param>
    /// <param name="newPasswordSalt">새 비밀번호 솔트</param>
    public void ChangePassword(string newPasswordHash, string newPasswordSalt)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
        PasswordSalt = newPasswordSalt ?? throw new ArgumentNullException(nameof(newPasswordSalt));
        MarkAsModified();
    }

    /// <summary>
    /// 로그인 기록 업데이트
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// 계정 비활성화
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsModified();
    }

    /// <summary>
    /// 계정 활성화
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsModified();
    }

    /// <summary>
    /// 비밀번호 업데이트 (ChangePassword의 별칭)
    /// </summary>
    /// <param name="newPasswordHash">새 비밀번호 해시</param>
    /// <param name="newPasswordSalt">새 비밀번호 솔트</param>
    public void UpdatePassword(string newPasswordHash, string newPasswordSalt)
    {
        ChangePassword(newPasswordHash, newPasswordSalt);
    }

    /// <summary>
    /// 사용자 역할 변경 (관리자만 가능)
    /// </summary>
    /// <param name="newRole">새 역할</param>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        MarkAsModified();
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

    /// <summary>
    /// 마지막 로그인 시간 업데이트 (RecordLogin의 별칭)
    /// </summary>
    public void UpdateLastLogin()
    {
        RecordLogin();
    }

    /// <summary>
    /// 계정을 공개로 설정
    /// </summary>
    public void MakePublic()
    {
        IsPublic = true;
        MarkAsModified();
    }

    /// <summary>
    /// 계정을 비공개로 설정
    /// </summary>
    public void MakePrivate()
    {
        IsPublic = false;
        MarkAsModified();
    }

    /// <summary>
    /// 계정 공개 설정 토글
    /// </summary>
    public void TogglePublic()
    {
        IsPublic = !IsPublic;
        MarkAsModified();
    }

    /// <summary>
    /// 계정 공개 설정 변경
    /// </summary>
    /// <param name="isPublic">공개 여부</param>
    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        MarkAsModified();
    }
}