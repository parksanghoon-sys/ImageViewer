using ImageViewer.Domain.Common;
using System;
using System.Collections.Generic;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// 이미지 엔티티
/// 업로드된 이미지의 메타데이터와 파일 정보를 관리
/// </summary>
public class Image : BaseEntity
{
    /// <summary>
    /// 이미지를 업로드한 사용자 ID (AuthContext의 ApplicationUser.Id 참조)
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// 원본 파일명
    /// </summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>
    /// 저장된 파일명 (고유한 이름으로 변경됨)
    /// </summary>
    public string StoredFileName { get; private set; } = string.Empty;

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// 썸네일 파일 경로
    /// </summary>
    public string? ThumbnailPath { get; private set; }

    /// <summary>
    /// 파일 크기 (바이트)
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// MIME 타입 (예: image/jpeg, image/png)
    /// </summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>
    /// 이미지 너비
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// 이미지 높이
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// 이미지 제목
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// 이미지 설명 (선택사항)
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 이미지 태그 (쉼표로 구분)
    /// </summary>
    public string? Tags { get; private set; }

    /// <summary>
    /// 공개 여부
    /// </summary>
    public bool IsPublic { get; private set; } = false;

    /// <summary>
    /// 업로드 날짜
    /// </summary>
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 썸네일 생성 완료 여부
    /// </summary>
    public bool ThumbnailReady { get; private set; } = false;

    /// <summary>
    /// 이미지를 업로드한 사용자
    /// </summary>
    public virtual User User { get; private set; } = null!;

    /// <summary>
    /// 이 이미지와 관련된 공유 요청들
    /// </summary>
    public virtual ICollection<ShareRequest> ShareRequests { get; private set; } = new List<ShareRequest>();

    /// <summary>
    /// 기본 생성자 (EF Core용)
    /// </summary>
    protected Image() { }

    /// <summary>
    /// 새 이미지 생성
    /// </summary>
    /// <param name="userId">업로드한 사용자 ID (ApplicationUser.Id)</param>
    /// <param name="originalFileName">원본 파일명</param>
    /// <param name="storedFileName">저장된 파일명</param>
    /// <param name="filePath">파일 경로</param>
    /// <param name="fileSize">파일 크기</param>
    /// <param name="mimeType">MIME 타입</param>
    /// <param name="width">이미지 너비</param>
    /// <param name="height">이미지 높이</param>
    /// <param name="title">이미지 제목</param>
    /// <param name="description">이미지 설명</param>
    /// <param name="tags">이미지 태그</param>
    /// <param name="isPublic">공개 여부</param>
    public Image(
        string userId,
        string originalFileName,
        string storedFileName,
        string filePath,
        long fileSize,
        string mimeType,
        int width,
        int height,
        string title = "",
        string? description = null,
        string? tags = null,
        bool isPublic = false)
    {
        Id = Guid.NewGuid(); // 고유 GUID 생성
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
        StoredFileName = storedFileName ?? throw new ArgumentNullException(nameof(storedFileName));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FileSize = fileSize;
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        Width = width;
        Height = height;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Tags = tags;
        IsPublic = isPublic;
        UploadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 썸네일 경로 설정
    /// </summary>
    /// <param name="thumbnailPath">썸네일 파일 경로</param>
    public void SetThumbnailPath(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath ?? throw new ArgumentNullException(nameof(thumbnailPath));
        ThumbnailReady = true;
        MarkAsModified();
    }

    /// <summary>
    /// 이미지 설명 업데이트
    /// </summary>
    /// <param name="description">새 설명</param>
    public void UpdateDescription(string? description)
    {
        Description = description;
        MarkAsModified();
    }

    /// <summary>
    /// 이미지 크기 정보 업데이트
    /// </summary>
    /// <param name="width">너비</param>
    /// <param name="height">높이</param>
    public void UpdateDimensions(int width, int height)
    {
        Width = width;
        Height = height;
        MarkAsModified();
    }
}