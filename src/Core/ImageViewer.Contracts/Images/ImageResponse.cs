namespace ImageViewer.Contracts.Images;

/// <summary>
/// 이미지 정보 응답 DTO
/// 클라이언트에게 반환되는 이미지 정보
/// </summary>
public class ImageResponse
{
    /// <summary>
    /// 이미지 고유 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 이미지 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 원본 이미지 파일명
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 파일 크기 (바이트)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME 타입 (image/jpeg, image/png 등)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 너비 (픽셀)
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 이미지 높이 (픽셀)
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 원본 이미지 다운로드 URL
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 썸네일 이미지 URL
    /// 썸네일이 생성되지 않은 경우 null
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 이미지 공개 여부
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 이미지 태그 목록
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 업로드한 사용자 ID (ApplicationUser.Id)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 업로드한 사용자명
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 업로드 일시
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// 썸네일 생성 상태
    /// </summary>
    public bool ThumbnailReady { get; set; }

    /// <summary>
    /// 현재 사용자가 소유한 이미지인지 여부
    /// 권한 체크용
    /// </summary>
    public bool IsOwner { get; set; }
}