namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 공개 사용자 정보 응답 DTO
/// 다른 사용자에게 표시되는 제한된 사용자 정보
/// </summary>
public class PublicUserResponse
{
    /// <summary>
    /// 사용자 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 사용자명
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 계정 공개 상태
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 총 이미지 개수
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// 계정 생성일시
    /// </summary>
    public DateTime CreatedAt { get; set; }
}