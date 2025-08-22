using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Contracts.Images;

/// <summary>
/// 이미지 목록 조회 요청 DTO
/// 페이징, 정렬, 필터링 기능을 위한 파라미터
/// </summary>
public class GetImagesRequest
{
    /// <summary>
    /// 페이지 번호 (1부터 시작)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "페이지 번호는 1 이상이어야 합니다.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 페이지당 항목 수
    /// 최소 1개, 최대 50개
    /// </summary>
    [Range(1, 50, ErrorMessage = "페이지 크기는 1~50 사이여야 합니다.")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 정렬 기준
    /// UploadDate: 업로드 날짜순
    /// Title: 제목순
    /// FileSize: 파일 크기순
    /// </summary>
    public ImageSortBy SortBy { get; set; } = ImageSortBy.UploadDate;

    /// <summary>
    /// 정렬 순서
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;

    /// <summary>
    /// 검색 키워드 (제목, 설명, 태그에서 검색)
    /// </summary>
    [StringLength(100, ErrorMessage = "검색 키워드는 100자를 초과할 수 없습니다.")]
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 특정 태그로 필터링
    /// </summary>
    [StringLength(50, ErrorMessage = "태그는 50자를 초과할 수 없습니다.")]
    public string? TagFilter { get; set; }

    /// <summary>
    /// 특정 날짜 이후 업로드된 이미지만 조회
    /// </summary>
    public DateTime? UploadedAfter { get; set; }

    /// <summary>
    /// 특정 날짜 이전 업로드된 이미지만 조회
    /// </summary>
    public DateTime? UploadedBefore { get; set; }

    /// <summary>
    /// 공개/비공개 필터
    /// null: 전체, true: 공개만, false: 비공개만
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// 썸네일 생성 완료 여부 필터
    /// null: 전체, true: 썸네일 있는 것만, false: 썸네일 없는 것만
    /// </summary>
    public bool? ThumbnailReady { get; set; }
}

/// <summary>
/// 이미지 정렬 기준 열거형
/// </summary>
public enum ImageSortBy
{
    /// <summary>업로드 날짜순</summary>
    UploadDate,
    /// <summary>제목순</summary>
    Title,
    /// <summary>파일 크기순</summary>
    FileSize,
    /// <summary>이미지 너비순</summary>
    Width,
    /// <summary>이미지 높이순</summary>
    Height
}

/// <summary>
/// 정렬 순서 열거형
/// </summary>
public enum SortOrder
{
    /// <summary>오름차순</summary>
    Ascending,
    /// <summary>내림차순</summary>
    Descending
}