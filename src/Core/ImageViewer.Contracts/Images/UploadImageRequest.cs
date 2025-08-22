using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Contracts.Images;

/// <summary>
/// 이미지 업로드 요청 DTO
/// 파일 업로드 시 필요한 정보와 검증 규칙을 정의
/// </summary>
public class UploadImageRequest
{
    /// <summary>
    /// 업로드할 이미지 파일
    /// 최대 10MB, JPG/PNG/GIF 형식만 허용
    /// </summary>
    [Required(ErrorMessage = "이미지 파일은 필수입니다.")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// 이미지 제목 (선택사항)
    /// 제공되지 않으면 파일명을 사용
    /// </summary>
    [StringLength(200, ErrorMessage = "제목은 200자를 초과할 수 없습니다.")]
    public string? Title { get; set; }

    /// <summary>
    /// 이미지 설명 (선택사항)
    /// </summary>
    [StringLength(1000, ErrorMessage = "설명은 1000자를 초과할 수 없습니다.")]
    public string? Description { get; set; }

    /// <summary>
    /// 이미지 공개 여부
    /// true: 공개 (다른 사용자가 볼 수 있음)
    /// false: 비공개 (본인만 볼 수 있음)
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 이미지 태그 (쉼표로 구분)
    /// 예: "풍경,자연,바다"
    /// </summary>
    [StringLength(500, ErrorMessage = "태그는 500자를 초과할 수 없습니다.")]
    public string? Tags { get; set; }

    /// <summary>
    /// 사용자 ID (인증된 사용자에서 자동 설정)
    /// API 컨트롤러에서 JWT 토큰으로부터 추출 (ApplicationUser.Id)
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}