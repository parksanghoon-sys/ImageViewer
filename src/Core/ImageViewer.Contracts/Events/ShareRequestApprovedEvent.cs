using System.Text.Json.Serialization;

namespace ImageViewer.Contracts.Events;

/// <summary>
/// 공유 요청 승인 이벤트
/// 알림 발송을 위한 이벤트
/// </summary>
public class ShareRequestApprovedEvent
{
    /// <summary>
    /// 이벤트 ID
    /// </summary>
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 이벤트 발생 시간
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 공유 요청 ID
    /// </summary>
    [JsonPropertyName("shareRequestId")]
    public required Guid ShareRequestId { get; set; }

    /// <summary>
    /// 이미지 ID
    /// </summary>
    [JsonPropertyName("imageId")]
    public required Guid ImageId { get; set; }

    /// <summary>
    /// 이미지 파일명
    /// </summary>
    [JsonPropertyName("imageFileName")]
    public required string ImageFileName { get; set; }

    /// <summary>
    /// 공유 요청자 ID (알림을 받을 사용자)
    /// </summary>
    [JsonPropertyName("requesterId")]
    public required string RequesterId { get; set; }

    /// <summary>
    /// 승인자 ID (이미지 소유자)
    /// </summary>
    [JsonPropertyName("ownerId")]
    public required string OwnerId { get; set; }

    /// <summary>
    /// 승인 시간
    /// </summary>
    [JsonPropertyName("approvedAt")]
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 원래 공유 요청 메시지
    /// </summary>
    [JsonPropertyName("originalMessage")]
    public string? OriginalMessage { get; set; }
}