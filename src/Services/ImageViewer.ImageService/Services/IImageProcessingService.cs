namespace ImageViewer.ImageService.Services;

/// <summary>
/// 이미지 처리 서비스 인터페이스
/// 이미지 업로드, 썸네일 생성, 블러 처리 등의 기능을 제공
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// 이미지 파일을 저장하고 썸네일을 생성
    /// </summary>
    /// <param name="imageStream">이미지 스트림</param>
    /// <param name="fileName">파일명</param>
    /// <param name="userId">사용자 ID</param>
    /// <returns>저장된 이미지 정보</returns>
    Task<(string OriginalPath, string ThumbnailPath, long FileSize)> SaveImageAsync(
        Stream imageStream, string fileName, Guid userId);

    /// <summary>
    /// 썸네일 이미지 생성
    /// </summary>
    /// <param name="originalPath">원본 이미지 경로</param>
    /// <param name="thumbnailSize">썸네일 크기</param>
    /// <returns>썸네일 경로</returns>
    Task<string> CreateThumbnailAsync(string originalPath, int thumbnailSize = 200);

    /// <summary>
    /// 블러 처리된 미리보기 이미지 생성
    /// </summary>
    /// <param name="originalPath">원본 이미지 경로</param>
    /// <param name="blurRadius">블러 반경</param>
    /// <returns>블러 처리된 이미지 경로</returns>
    Task<string> CreateBlurredPreviewAsync(string originalPath, float blurRadius = 10f);

    /// <summary>
    /// 이미지 파일 삭제
    /// </summary>
    /// <param name="imagePath">이미지 경로</param>
    Task DeleteImageAsync(string imagePath);

    /// <summary>
    /// 지원되는 이미지 형식인지 확인
    /// </summary>
    /// <param name="fileName">파일명</param>
    /// <returns>지원 여부</returns>
    bool IsSupportedImageFormat(string fileName);

    /// <summary>
    /// 사용자별 이미지 저장 디렉토리 경로 생성
    /// </summary>
    /// <param name="userId">사용자 ID</param>
    /// <returns>디렉토리 경로</returns>
    string GetUserImageDirectory(Guid userId);
}