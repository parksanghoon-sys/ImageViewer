namespace ImageViewer.Domain.Enums;

/// <summary>
/// 공유 요청 상태를 나타내는 열거형
/// </summary>
public enum ShareRequestStatus
{
    /// <summary>
    /// 대기 중 - 아직 승인/거절되지 않은 상태
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 승인됨 - 이미지 소유자가 공유를 승인한 상태
    /// </summary>
    Approved = 1,

    /// <summary>
    /// 거절됨 - 이미지 소유자가 공유를 거절한 상태
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// 취소됨 - 요청자가 요청을 취소한 상태
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// 만료됨 - 요청 기간이 지나 자동으로 만료된 상태
    /// </summary>
    Expired = 4
}