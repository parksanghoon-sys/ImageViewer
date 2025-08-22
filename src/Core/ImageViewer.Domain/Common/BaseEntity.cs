using System;

namespace ImageViewer.Domain.Common;

/// <summary>
/// 모든 엔티티의 기본 클래스
/// 공통적으로 사용되는 ID, 생성일, 수정일 등의 속성을 포함
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 엔티티의 고유 식별자
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// 엔티티가 생성된 날짜 및 시간
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// 엔티티가 마지막으로 수정된 날짜 및 시간
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// 엔티티 수정 시 호출되는 메서드
    /// UpdatedAt 속성을 현재 시간으로 설정
    /// </summary>
    protected void MarkAsModified()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}