using System.Text.Json.Serialization;

namespace ImageViewer.Contracts.Events;

/// <summary>
/// 공유 요청 생성 이벤트
/// 대상 사용자에게 알림을 발송하기 위한 이벤트
/// </summary>
public class ShareRequestCreatedEvent
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
    /// 공유 요청자 ID
    /// </summary>
    [JsonPropertyName("requesterId")]
    public required string RequesterId { get; set; }

    /// <summary>
    /// 대상 사용자 ID (알림을 받을 사용자)
    /// </summary>
    [JsonPropertyName("targetUserId")]
    public required string TargetUserId { get; set; }

    /// <summary>
    /// 공유 요청 메시지
    /// </summary>
    [JsonPropertyName("requestMessage")]
    public string? RequestMessage { get; set; }

    /// <summary>
    /// 요청 생성 시간
    /// </summary>
    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}