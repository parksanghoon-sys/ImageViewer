namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 공개 사용자 목록 응답 DTO
/// </summary>
public class PublicUserListResponse
{
    /// <summary>
    /// 공개 사용자 목록
    /// </summary>
    public List<PublicUserResponse> Users { get; set; } = new();

    /// <summary>
    /// 총 공개 사용자 수
    /// </summary>
    public int TotalCount { get; set; }
}