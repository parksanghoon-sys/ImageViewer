using System.ComponentModel.DataAnnotations;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Contracts.Authentication;

/// <summary>
/// 사용자 역할 변경 요청 DTO
/// 관리자가 사용자의 역할을 변경할 때 사용
/// </summary>
public record ChangeRoleRequest
{
    /// <summary>
    /// 새로 설정할 사용자 역할
    /// </summary>
    [Required(ErrorMessage = "새 역할은 필수입니다.")]
    public UserRole NewRole { get; init; }

    /// <summary>
    /// 역할 변경 사유 (선택사항)
    /// </summary>
    [MaxLength(500, ErrorMessage = "변경 사유는 500자를 초과할 수 없습니다.")]
    public string? Reason { get; init; }
}