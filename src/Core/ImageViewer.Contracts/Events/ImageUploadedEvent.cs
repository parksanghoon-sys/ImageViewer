using System.Text.Json.Serialization;

namespace ImageViewer.Contracts.Events;

/// <summary>
/// 이미지 업로드 완료 이벤트
/// 썸네일 생성 및 후처리를 위한 비동기 이벤트
/// </summary>
public class ImageUploadedEvent
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
    /// 업로드된 이미지 ID
    /// </summary>
    [JsonPropertyName("imageId")]
    public required Guid ImageId { get; set; }

    /// <summary>
    /// 업로드한 사용자 ID
    /// </summary>
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }

    /// <summary>
    /// 원본 파일명
    /// </summary>
    [JsonPropertyName("originalFileName")]
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// 저장된 파일 경로
    /// </summary>
    [JsonPropertyName("filePath")]
    public required string FilePath { get; set; }

    /// <summary>
    /// 파일 크기 (바이트)
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME 타입
    /// </summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }

    /// <summary>
    /// 이미지 너비
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// 이미지 높이
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }
}