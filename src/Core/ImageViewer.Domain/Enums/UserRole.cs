namespace ImageViewer.Domain.Enums;

/// <summary>
/// 사용자 역할을 정의하는 열거형
/// 시스템 내에서 사용자의 권한 수준을 구분
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 게스트 사용자 - 최소한의 권한 (읽기 전용)
    /// </summary>
    Guest = 0,

    /// <summary>
    /// 일반 사용자 - 기본 기능 사용 가능
    /// </summary>
    User = 1,

    /// <summary>
    /// 관리자 - 모든 기능 및 사용자 관리 권한
    /// </summary>
    Admin = 2
}