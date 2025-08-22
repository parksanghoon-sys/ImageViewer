namespace ImageViewer.Contracts.Images;

/// <summary>
/// 이미지 목록 조회 응답 DTO
/// 페이징된 이미지 목록과 페이징 정보를 포함
/// </summary>
public class ImageListResponse
{
    /// <summary>
    /// 이미지 목록
    /// </summary>
    public List<ImageResponse> Images { get; set; } = new();

    /// <summary>
    /// 페이징 정보
    /// </summary>
    public PaginationInfo Pagination { get; set; } = new();

    /// <summary>
    /// 검색/필터 요약 정보
    /// </summary>
    public SearchSummary SearchSummary { get; set; } = new();
}

/// <summary>
/// 페이징 정보
/// </summary>
public class PaginationInfo
{
    /// <summary>
    /// 현재 페이지 번호
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 페이지당 항목 수
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 총 항목 수
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// 총 페이지 수
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 이전 페이지 존재 여부
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// 다음 페이지 존재 여부
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// 현재 페이지의 첫 번째 항목 인덱스 (1부터 시작)
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// 현재 페이지의 마지막 항목 인덱스
    /// </summary>
    public int EndIndex { get; set; }
}

/// <summary>
/// 검색/필터 요약 정보
/// </summary>
public class SearchSummary
{
    /// <summary>
    /// 적용된 검색 키워드
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 적용된 태그 필터
    /// </summary>
    public string? TagFilter { get; set; }

    /// <summary>
    /// 적용된 정렬 기준
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// 적용된 정렬 순서
    /// </summary>
    public string SortOrder { get; set; } = string.Empty;

    /// <summary>
    /// 총 검색 결과 수 (필터 적용 전)
    /// </summary>
    public int UnfilteredCount { get; set; }

    /// <summary>
    /// 필터 적용 후 결과 수
    /// </summary>
    public int FilteredCount { get; set; }

    /// <summary>
    /// 검색/필터 적용 여부
    /// </summary>
    public bool IsFiltered { get; set; }
}