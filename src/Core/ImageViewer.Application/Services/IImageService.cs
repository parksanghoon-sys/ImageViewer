using ImageViewer.Contracts.Images;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// 이미지 관리 서비스 인터페이스
/// 이미지 업로드, 조회, 썸네일 생성 등의 비즈니스 로직을 정의
/// </summary>
public interface IImageService
{
    /// <summary>
    /// 이미지 업로드 처리
    /// 파일 검증, 저장, 메타데이터 DB 저장, 썸네일 생성 요청 등을 수행
    /// </summary>
    /// <param name="request">업로드 요청 정보 (파일, 사용자 ID 등)</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>업로드된 이미지 정보</returns>
    Task<ImageResponse> UploadImageAsync(UploadImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용자의 이미지 목록 조회
    /// 페이징, 정렬, 필터링 기능 포함
    /// </summary>
    /// <param name="userId">사용자 ID (ApplicationUser.Id)</param>
    /// <param name="request">조회 요청 정보 (페이징, 정렬 등)</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>이미지 목록과 페이징 정보</returns>
    Task<ImageListResponse> GetUserImagesAsync(string userId, GetImagesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 이미지 상세 조회
    /// 접근 권한 검증 포함
    /// </summary>
    /// <param name="imageId">이미지 ID</param>
    /// <param name="userId">요청 사용자 ID (ApplicationUser.Id)</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>이미지 상세 정보</returns>
    Task<ImageResponse?> GetImageByIdAsync(Guid imageId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 이미지 삭제
    /// 파일 시스템과 DB에서 모두 삭제
    /// </summary>
    /// <param name="imageId">삭제할 이미지 ID</param>
    /// <param name="userId">요청 사용자 ID (ApplicationUser.Id)</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>삭제 성공 여부</returns>
    Task<bool> DeleteImageAsync(Guid imageId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 썸네일 생성 상태 업데이트
    /// 비동기 썸네일 생성 완료 시 호출됨
    /// </summary>
    /// <param name="imageId">이미지 ID</param>
    /// <param name="thumbnailPath">생성된 썸네일 경로</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>업데이트 성공 여부</returns>
    Task<bool> UpdateThumbnailAsync(Guid imageId, string thumbnailPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 공유된 이미지 목록 조회
    /// 다른 사용자가 공유한 이미지들을 조회
    /// </summary>
    /// <param name="userId">요청 사용자 ID</param>
    /// <param name="request">조회 요청 정보</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>공유된 이미지 목록</returns>
    Task<ImageListResponse> GetSharedImagesAsync(Guid userId, GetImagesRequest request, CancellationToken cancellationToken = default);
}