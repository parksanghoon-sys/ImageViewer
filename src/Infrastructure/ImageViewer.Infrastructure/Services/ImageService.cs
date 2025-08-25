using ImageViewer.Application.Services;
using ImageViewer.Contracts.Images;
using ImageViewer.Contracts.Events;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// 이미지 관리 서비스 구현체
/// 이미지 업로드, 조회, 썸네일 생성 등의 비즈니스 로직을 처리
/// </summary>
public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImageService> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly string _uploadPath;
    private readonly string _thumbnailPath;
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedContentTypes = { "image/jpeg", "image/png", "image/gif" };
    private readonly int _thumbnailWidth = 300;
    private readonly int _thumbnailHeight = 300;

    public ImageService(ApplicationDbContext context, ILogger<ImageService> logger, IRabbitMQService rabbitMQService)
    {
        _context = context;
        _logger = logger;
        _rabbitMQService = rabbitMQService;
        
        // 업로드 경로 설정 (실제 환경에서는 appsettings.json에서 가져와야 함)
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
        _thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "thumbnails");
        
        // 디렉토리 생성
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_thumbnailPath);
    }

    /// <summary>
    /// 이미지 업로드 처리
    /// 파일 검증 → 저장 → DB 기록 → 썸네일 생성
    /// </summary>
    public async Task<ImageResponse> UploadImageAsync(UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("사용자 {UserId}의 이미지 업로드 시작: {FileName}", request.UserId, request.File.FileName);

        // 1. 파일 검증
        ValidateFile(request.File);

        // 2. 사용자 ID 유효성 검사
        if (string.IsNullOrEmpty(request.UserId))
        {
            throw new ArgumentNullException(nameof(request.UserId), "사용자 ID가 필요합니다.");
        }

        // 3. 고유 파일명 생성
        var uniqueFileName = GenerateUniqueFileName(request.File.FileName);
        var userUploadPath = Path.Combine(_uploadPath, request.UserId);
        Directory.CreateDirectory(userUploadPath);
        
        var filePath = Path.Combine(userUploadPath, uniqueFileName);

        // 4. 파일 저장
        await SaveFileAsync(request.File, filePath, cancellationToken);

        // 5. 이미지 메타데이터 추출
        var (width, height) = GetImageDimensions(filePath);

        // 6. DB에 이미지 정보 저장
        var title = string.IsNullOrEmpty(request.Title) ? Path.GetFileNameWithoutExtension(request.File.FileName) : request.Title;
        var image = new Domain.Entities.Image(
            request.UserId,
            request.File.FileName,
            uniqueFileName,
            GetRelativePath(filePath),
            request.File.Length,
            request.File.ContentType,
            width,
            height,
            title,
            request.Description,
            request.Tags,
            request.IsPublic
        );

        _context.Images.Add(image);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. 이미지 업로드 이벤트 발행 (RabbitMQ)
        var uploadedEvent = new ImageUploadedEvent
        {
            ImageId = image.Id,
            UserId = request.UserId,
            OriginalFileName = request.File.FileName,
            FilePath = GetRelativePath(filePath),
            FileSize = request.File.Length,
            MimeType = request.File.ContentType,
            Width = width,
            Height = height
        };

        // 비동기로 이벤트 발행 (실패해도 업로드 자체는 성공으로 처리)
        _ = Task.Run(async () =>
        {
            try
            {
                await _rabbitMQService.PublishEventAsync(uploadedEvent, "image.uploaded");
                _logger.LogInformation("이미지 업로드 이벤트 발행 완료: ImageId={ImageId}", image.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이미지 업로드 이벤트 발행 실패: ImageId={ImageId}", image.Id);
            }
        }, cancellationToken);

        _logger.LogInformation("이미지 업로드 완료: ID={ImageId}, UserId={UserId}", image.Id, request.UserId);

        // 8. 응답 DTO 생성
        return MapToImageResponse(image, request.UserId);
    }

    /// <summary>
    /// 사용자의 이미지 목록 조회
    /// </summary>
    public async Task<ImageListResponse> GetUserImagesAsync(string userId, GetImagesRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("사용자 {UserId}의 이미지 목록 조회 시작", userId);

        var query = _context.Images
            .Where(i => i.UserId == userId);

        // 검색 필터 적용
        query = ApplyFilters(query, request);

        // 총 개수 (필터 적용 후)
        var totalCount = await query.CountAsync(cancellationToken);

        // 정렬 적용
        query = ApplySorting(query, request);

        // 페이징 적용
        var images = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 응답 생성
        var imageResponses = images.Select(img => MapToImageResponse(img, userId)).ToList();

        return new ImageListResponse
        {
            Images = imageResponses,
            Pagination = CreatePaginationInfo(request.Page, request.PageSize, totalCount),
            SearchSummary = CreateSearchSummary(request, totalCount, totalCount)
        };
    }

    /// <summary>
    /// 특정 이미지 상세 조회
    /// </summary>
    public async Task<ImageResponse?> GetImageByIdAsync(Guid imageId, string userId, CancellationToken cancellationToken = default)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId, cancellationToken);

        if (image == null)
        {
            _logger.LogWarning("이미지를 찾을 수 없음: ImageId={ImageId}, UserId={UserId}", imageId, userId);
            return null;
        }

        return MapToImageResponse(image, userId);
    }

    /// <summary>
    /// 이미지 삭제
    /// </summary>
    public async Task<bool> DeleteImageAsync(Guid imageId, string userId, CancellationToken cancellationToken = default)
    {
        var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId, cancellationToken);
        
        if (image == null)
        {
            _logger.LogWarning("삭제할 이미지를 찾을 수 없음: ImageId={ImageId}, UserId={UserId}", imageId, userId);
            return false;
        }

        try
        {
            // 파일 삭제
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // 썸네일 삭제
            if (!string.IsNullOrEmpty(image.ThumbnailPath))
            {
                var thumbnailFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ThumbnailPath.TrimStart('/'));
                if (File.Exists(thumbnailFullPath))
                {
                    File.Delete(thumbnailFullPath);
                }
            }

            // DB에서 삭제
            _context.Images.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("이미지 삭제 완료: ImageId={ImageId}, UserId={UserId}", imageId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 삭제 중 오류 발생: ImageId={ImageId}, UserId={UserId}", imageId, userId);
            return false;
        }
    }

    /// <summary>
    /// 썸네일 생성 상태 업데이트
    /// </summary>
    public async Task<bool> UpdateThumbnailAsync(Guid imageId, string thumbnailPath, CancellationToken cancellationToken = default)
    {
        var image = await _context.Images.FindAsync(imageId);
        if (image == null) return false;

        image.SetThumbnailPath(thumbnailPath);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("썸네일 생성 완료: ImageId={ImageId}", imageId);

        return true;
    }

    /// <summary>
    /// 공유된 이미지 목록 조회 (추후 구현)
    /// </summary>
    public async Task<ImageListResponse> GetSharedImagesAsync(Guid userId, GetImagesRequest request, CancellationToken cancellationToken = default)
    {
        // 공유 기능은 3주차에 구현 예정
        // 현재는 빈 결과 반환
        return new ImageListResponse
        {
            Images = new List<ImageResponse>(),
            Pagination = CreatePaginationInfo(1, request.PageSize, 0),
            SearchSummary = CreateSearchSummary(request, 0, 0)
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// 파일 검증 (크기, 타입, 확장자)
    /// </summary>
    private void ValidateFile(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("파일이 선택되지 않았습니다.");

        if (file.Length > _maxFileSize)
            throw new ArgumentException($"파일 크기가 {_maxFileSize / 1024 / 1024}MB를 초과합니다.");

        if (!_allowedContentTypes.Contains(file.ContentType.ToLower()))
            throw new ArgumentException($"지원하지 않는 파일 형식입니다. 허용 형식: {string.Join(", ", _allowedContentTypes)}");

        var extension = Path.GetExtension(file.FileName).ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException($"지원하지 않는 파일 확장자입니다. 허용 확장자: {string.Join(", ", allowedExtensions)}");
    }

    /// <summary>
    /// 고유 파일명 생성
    /// </summary>
    private string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        
        return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// 파일 저장
    /// </summary>
    private async Task SaveFileAsync(Microsoft.AspNetCore.Http.IFormFile file, string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>
    /// 이미지 크기 정보 추출
    /// </summary>
    private (int width, int height) GetImageDimensions(string filePath)
    {
        try
        {
            using var image = System.Drawing.Image.FromFile(filePath);
            return (image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "이미지 크기 정보 추출 실패: {FilePath}", filePath);
            return (0, 0);
        }
    }

    /// <summary>
    /// 상대 경로 생성
    /// </summary>
    private string GetRelativePath(string fullPath)
    {
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relativePath = fullPath.Replace(webRoot, "").Replace("\\", "/");
        // URL은 /으로 시작해야 함
        return relativePath.StartsWith("/") ? relativePath : "/" + relativePath;
    }

    /// <summary>
    /// 썸네일 생성 (비동기)
    /// </summary>
    private async Task GenerateThumbnailAsync(Guid imageId, string originalPath)
    {
        try
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null) return;

            var thumbnailFileName = $"thumb_{image.StoredFileName}";
            var userThumbnailPath = Path.Combine(_thumbnailPath, image.UserId.ToString());
            Directory.CreateDirectory(userThumbnailPath);

            var thumbnailFullPath = Path.Combine(userThumbnailPath, thumbnailFileName);

            // ImageSharp은 제네릭 픽셀 타입 필요 (Rgba32가 가장 흔히 씀)
            using (var originalImage = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(originalPath))
            {
                var thumbnailSize = CalculateThumbnailSize(originalImage.Width, originalImage.Height);

                // Mutate는 SixLabors.ImageSharp.Processing 네임스페이스에 있음
                originalImage.Mutate(x => x.Resize(thumbnailSize.width, thumbnailSize.height));

                var encoder = new JpegEncoder { Quality = 85 };
                await originalImage.SaveAsync(thumbnailFullPath, encoder);
            }

            await UpdateThumbnailAsync(imageId, GetRelativePath(thumbnailFullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "썸네일 생성 실패: ImageId={ImageId}", imageId);
        }
    }

    /// <summary>
    /// 썸네일 크기 계산 (비율 유지)
    /// </summary>
    private (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight)
    {
        var ratio = Math.Min((double)_thumbnailWidth / originalWidth, (double)_thumbnailHeight / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    /// <summary>
    /// 필터 적용
    /// </summary>
    private IQueryable<Domain.Entities.Image> ApplyFilters(IQueryable<Domain.Entities.Image> query, GetImagesRequest request)
    {
        if (!string.IsNullOrEmpty(request.SearchKeyword))
        {
            var keyword = request.SearchKeyword.ToLower();
            query = query.Where(i => 
                i.Title.ToLower().Contains(keyword) ||
                (i.Description != null && i.Description.ToLower().Contains(keyword)) ||
                (i.Tags != null && i.Tags.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrEmpty(request.TagFilter))
        {
            query = query.Where(i => i.Tags != null && i.Tags.ToLower().Contains(request.TagFilter.ToLower()));
        }

        if (request.UploadedAfter.HasValue)
        {
            query = query.Where(i => i.UploadedAt >= request.UploadedAfter.Value);
        }

        if (request.UploadedBefore.HasValue)
        {
            query = query.Where(i => i.UploadedAt <= request.UploadedBefore.Value);
        }

        if (request.IsPublic.HasValue)
        {
            query = query.Where(i => i.IsPublic == request.IsPublic.Value);
        }

        if (request.ThumbnailReady.HasValue)
        {
            query = query.Where(i => i.ThumbnailReady == request.ThumbnailReady.Value);
        }

        return query;
    }

    /// <summary>
    /// 정렬 적용
    /// </summary>
    private IQueryable<Domain.Entities.Image> ApplySorting(IQueryable<Domain.Entities.Image> query, GetImagesRequest request)
    {
        return request.SortBy switch
        {
            ImageSortBy.Title => request.SortOrder == SortOrder.Ascending 
                ? query.OrderBy(i => i.Title) 
                : query.OrderByDescending(i => i.Title),
            ImageSortBy.FileSize => request.SortOrder == SortOrder.Ascending 
                ? query.OrderBy(i => i.FileSize) 
                : query.OrderByDescending(i => i.FileSize),
            ImageSortBy.Width => request.SortOrder == SortOrder.Ascending 
                ? query.OrderBy(i => i.Width) 
                : query.OrderByDescending(i => i.Width),
            ImageSortBy.Height => request.SortOrder == SortOrder.Ascending 
                ? query.OrderBy(i => i.Height) 
                : query.OrderByDescending(i => i.Height),
            _ => request.SortOrder == SortOrder.Ascending 
                ? query.OrderBy(i => i.UploadedAt) 
                : query.OrderByDescending(i => i.UploadedAt)
        };
    }

    /// <summary>
    /// Entity를 Response DTO로 매핑
    /// </summary>
    private ImageResponse MapToImageResponse(Domain.Entities.Image image, string currentUserId)
    {
        return new ImageResponse
        {
            Id = image.Id,
            Title = image.Title,
            Description = image.Description,
            FileName = image.OriginalFileName,
            FileSize = image.FileSize,
            ContentType = image.MimeType,
            Width = image.Width,
            Height = image.Height,
            ImageUrl = image.FilePath,
            ThumbnailUrl = string.IsNullOrEmpty(image.ThumbnailPath) ? null : image.ThumbnailPath,
            IsPublic = image.IsPublic,
            Tags = string.IsNullOrEmpty(image.Tags) ? new List<string>() : image.Tags.Split(',').Select(t => t.Trim()).ToList(),
            UserId = image.UserId,
            UserName = "Unknown User",
            UploadedAt = image.UploadedAt,
            ThumbnailReady = image.ThumbnailReady,
            IsOwner = string.Equals(currentUserId, image.UserId, StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// 페이징 정보 생성
    /// </summary>
    private PaginationInfo CreatePaginationInfo(int page, int pageSize, int totalItems)
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var startIndex = (page - 1) * pageSize + 1;
        var endIndex = Math.Min(startIndex + pageSize - 1, totalItems);

        return new PaginationInfo
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            StartIndex = totalItems > 0 ? startIndex : 0,
            EndIndex = totalItems > 0 ? endIndex : 0
        };
    }

    /// <summary>
    /// 검색 요약 정보 생성
    /// </summary>
    private SearchSummary CreateSearchSummary(GetImagesRequest request, int unfilteredCount, int filteredCount)
    {
        return new SearchSummary
        {
            SearchKeyword = request.SearchKeyword,
            TagFilter = request.TagFilter,
            SortBy = request.SortBy.ToString(),
            SortOrder = request.SortOrder.ToString(),
            UnfilteredCount = unfilteredCount,
            FilteredCount = filteredCount,
            IsFiltered = !string.IsNullOrEmpty(request.SearchKeyword) || 
                        !string.IsNullOrEmpty(request.TagFilter) ||
                        request.UploadedAfter.HasValue ||
                        request.UploadedBefore.HasValue ||
                        request.IsPublic.HasValue ||
                        request.ThumbnailReady.HasValue
        };
    }

    /// <inheritdoc />
    public async Task<object> GetPublicUsersAsync(CancellationToken cancellationToken = default)
    {
        // TODO: 실제 구현 - 공개 사용자 목록 조회 (AuthService와 연동 필요)
        _logger.LogInformation("공개 사용자 목록 조회 - 임시 구현");
        return new List<object>();
    }

    /// <inheritdoc />
    public async Task<ImageListResponse> GetPublicUserImagesAsync(string userId, GetImagesRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: 실제 구현 - 공개 사용자의 이미지 조회 (AuthService와 연동 필요)
        _logger.LogInformation("공개 사용자 이미지 조회 - 임시 구현");
        return new ImageListResponse();
    }

    /// <inheritdoc />
    public async Task<bool> SetUserPublicAsync(string userId, bool isPublic, CancellationToken cancellationToken = default)
    {
        // TODO: 실제 구현 - AuthService와 연동하여 사용자 공개 설정 변경
        _logger.LogInformation("사용자 공개 설정 변경 - 임시 구현");
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> GetUserPublicStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: 실제 구현 - AuthService와 연동하여 사용자 공개 상태 조회
        _logger.LogDebug("사용자 공개 상태 조회 - 임시 구현");
        return false;
    }

    #endregion
}