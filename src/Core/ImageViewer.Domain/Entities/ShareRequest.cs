using ImageViewer.Domain.Common;
using ImageViewer.Domain.Enums;
using System;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// 이미지 공유 요청 엔티티
/// 사용자 간 이미지 공유 요청과 승인/거절을 관리
/// </summary>
public class ShareRequest : BaseEntity
{
    /// <summary>
    /// 공유를 요청한 사용자 ID (AuthContext의 ApplicationUser.Id 참조)
    /// </summary>
    public string RequesterId { get; private set; } = string.Empty;

    /// <summary>
    /// 이미지 소유자 (공유 승인/거절할 사용자) ID (AuthContext의 ApplicationUser.Id 참조)
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// 공유하려는 이미지 ID
    /// </summary>
    public Guid ImageId { get; private set; }

    /// <summary>
    /// 공유 요청 상태
    /// </summary>
    public ShareRequestStatus Status { get; private set; } = ShareRequestStatus.Pending;

    /// <summary>
    /// 요청 메시지 (선택사항)
    /// </summary>
    public string? RequestMessage { get; private set; }

    /// <summary>
    /// 응답 메시지 (승인/거절 시 메시지)
    /// </summary>
    public string? ResponseMessage { get; private set; }

    /// <summary>
    /// 응답 처리 시간
    /// </summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>
    /// 공유 요청 만료 시간
    /// </summary>
    public DateTime ExpiresAt { get; private set; }


    /// <summary>
    /// 공유하려는 이미지
    /// </summary>
    public virtual Image Image { get; private set; } = null!;

    /// <summary>
    /// 기본 생성자 (EF Core용)
    /// </summary>
    protected ShareRequest() { }

    /// <summary>
    /// 새 공유 요청 생성
    /// </summary>
    /// <param name="requesterId">요청자 ID (ApplicationUser.Id)</param>
    /// <param name="ownerId">소유자 ID (ApplicationUser.Id)</param>
    /// <param name="imageId">이미지 ID</param>
    /// <param name="requestMessage">요청 메시지</param>
    /// <param name="expirationDays">만료일까지의 일수 (기본 7일)</param>
    public ShareRequest(
        string requesterId,
        string ownerId,
        Guid imageId,
        string? requestMessage = null,
        int expirationDays = 7)
    {
        if (string.Equals(requesterId, ownerId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("요청자와 소유자가 같을 수 없습니다.");

        RequesterId = requesterId ?? throw new ArgumentNullException(nameof(requesterId));
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        ImageId = imageId;
        RequestMessage = requestMessage;
        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
    }

    /// <summary>
    /// 공유 요청 승인
    /// </summary>
    /// <param name="responseMessage">응답 메시지</param>
    public void Approve(string? responseMessage = null)
    {
        if (Status != ShareRequestStatus.Pending)
            throw new InvalidOperationException("대기 중인 요청만 승인할 수 있습니다.");

        if (IsExpired())
            throw new InvalidOperationException("만료된 요청은 승인할 수 없습니다.");

        Status = ShareRequestStatus.Approved;
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// 공유 요청 거절
    /// </summary>
    /// <param name="responseMessage">거절 사유</param>
    public void Reject(string? responseMessage = null)
    {
        if (Status != ShareRequestStatus.Pending)
            throw new InvalidOperationException("대기 중인 요청만 거절할 수 있습니다.");

        Status = ShareRequestStatus.Rejected;
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// 공유 요청 취소 (요청자가 취소)
    /// </summary>
    public void Cancel()
    {
        if (Status != ShareRequestStatus.Pending)
            throw new InvalidOperationException("대기 중인 요청만 취소할 수 있습니다.");

        Status = ShareRequestStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// 요청이 만료되었는지 확인
    /// </summary>
    /// <returns>만료 여부</returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// 요청이 처리되었는지 확인
    /// </summary>
    /// <returns>처리 여부</returns>
    public bool IsProcessed()
    {
        return Status != ShareRequestStatus.Pending;
    }

    /// <summary>
    /// 요청을 처리할 수 있는지 확인
    /// </summary>
    /// <returns>처리 가능 여부</returns>
    public bool CanBeProcessed()
    {
        return Status == ShareRequestStatus.Pending && !IsExpired();
    }

    /// <summary>
    /// 응답 처리 시간 (들랙이지)
    /// </summary>
    public DateTime? ResponseAt => RespondedAt;
}