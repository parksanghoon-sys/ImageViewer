using ImageViewer.Application.Services;
using ImageViewer.Contracts.Common;
using ImageViewer.Contracts.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImageViewer.ImageService.Controllers;

/// <summary>
/// 이미지 업로드, 조회, 관리를 담당하는 컨트롤러
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImageController> _logger;

    /// <summary>
    /// ImageController 생성자
    /// </summary>
    /// <param name="imageService">이미지 서비스</param>
    /// <param name="logger">로거</param>
    public ImageController(
        IImageService imageService, 
        ILogger<ImageController> logger)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 현재 사용자 ID를 가져옵니다.
    /// </summary>
    /// <returns>사용자 ID (ApplicationUser.Id)</returns>
    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            // 개발 환경에서는 기본 테스트 사용자 ID 사용
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return "test-user-id";
            }
            throw new UnauthorizedAccessException("유효하지 않은 사용자입니다.");
        }
        return userIdClaim;
    }

    /// <summary>
    /// 개발용 이미지 파일 업로드 (인증 없음)
    /// </summary>
    /// <param name="file">업로드할 이미지 파일</param>
    /// <param name="title">이미지 제목 (선택사항)</param>
    /// <param name="description">이미지 설명 (선택사항)</param>
    /// <param name="isPublic">공개 여부</param>
    /// <param name="tags">태그 (쉼표로 구분)</param>
    /// <returns>업로드된 이미지 정보</returns>
    [HttpPost("dev/upload")]
    [AllowAnonymous]
    public async Task<IActionResult> DevUploadImage(
        IFormFile file, 
        [FromForm] string? title = null,
        [FromForm] string? description = null,
        [FromForm] bool isPublic = false,
        [FromForm] string? tags = null)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        {
            return NotFound();
        }

        try
        {
            var userId = "11111111-1111-1111-1111-111111111111";

            var request = new UploadImageRequest
            {
                File = file,
                Title = title,
                Description = description,
                IsPublic = isPublic,
                Tags = tags,
                UserId = userId
            };

            var result = await _imageService.UploadImageAsync(request);

            _logger.LogInformation("개발용 이미지 업로드 완료: {UserId}, {FileName}", userId, file.FileName);

            return Ok(ApiResponse<ImageResponse>.SuccessResponse(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "개발용 이미지 업로드 검증 실패");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "개발용 이미지 업로드 중 오류 발생");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 업로드 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 이미지 파일을 업로드합니다.
    /// </summary>
    /// <param name="file">업로드할 이미지 파일</param>
    /// <param name="title">이미지 제목 (선택사항)</param>
    /// <param name="description">이미지 설명 (선택사항)</param>
    /// <param name="isPublic">공개 여부</param>
    /// <param name="tags">태그 (쉼표로 구분)</param>
    /// <returns>업로드된 이미지 정보</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(
        IFormFile file, 
        [FromForm] string? title = null,
        [FromForm] string? description = null,
        [FromForm] bool isPublic = false,
        [FromForm] string? tags = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var request = new UploadImageRequest
            {
                File = file,
                Title = title,
                Description = description,
                IsPublic = isPublic,
                Tags = tags,
                UserId = userId
            };

            var result = await _imageService.UploadImageAsync(request);

            _logger.LogInformation("이미지 업로드 완료: {UserId}, {FileName}", userId, file.FileName);

            return Ok(ApiResponse<ImageResponse>.SuccessResponse(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "이미지 업로드 검증 실패: {UserId}", GetCurrentUserId());
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 업로드 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 업로드 중 오류가 발생했습니다."));
        }
    }


    /// <summary>
    /// 개발용 이미지 목록 조회 (인증 없음)
    /// </summary>
    /// <param name="request">조회 요청 파라미터</param>
    /// <returns>이미지 목록</returns>
    [HttpGet("dev/my-images")]
    [AllowAnonymous]
    public async Task<IActionResult> DevGetMyImages([FromQuery] GetImagesRequest request)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        {
            return NotFound();
        }

        try
        {
            // 개발용 목 데이터 반환
            var mockResult = new ImageListResponse
            {
                Images = new List<ImageResponse>
                {
                    new ImageResponse
                    {
                        Id = Guid.NewGuid(),
                        Title = "샘플 이미지 1",
                        Description = "개발용 테스트 이미지입니다.",
                        Tags = new List<string> { "테스트", "샘플" },
                        FileName = "sample1.jpg",
                        FileSize = 1024000,
                        ContentType = "image/jpeg",
                        Width = 1920,
                        Height = 1080,
                        IsPublic = false,
                        UploadedAt = DateTime.UtcNow.AddDays(-1),
                        ThumbnailReady = true,
                        ImageUrl = "http://localhost:5215/sample1.jpg",
                        ThumbnailUrl = "http://localhost:5215/thumb_sample1.jpg",
                        UserId = "11111111-1111-1111-1111-111111111111",
                        UserName = "개발자",
                        IsOwner = true
                    },
                    new ImageResponse
                    {
                        Id = Guid.NewGuid(),
                        Title = "샘플 이미지 2",
                        Description = "또 다른 개발용 테스트 이미지입니다.",
                        Tags = new List<string> { "테스트", "개발" },
                        FileName = "sample2.png",
                        FileSize = 512000,
                        ContentType = "image/png",
                        Width = 1280,
                        Height = 720,
                        IsPublic = true,
                        UploadedAt = DateTime.UtcNow.AddHours(-2),
                        ThumbnailReady = true,
                        ImageUrl = "http://localhost:5215/sample2.png",
                        ThumbnailUrl = "http://localhost:5215/thumb_sample2.png",
                        UserId = "11111111-1111-1111-1111-111111111111",
                        UserName = "개발자",
                        IsOwner = true
                    }
                },
                Pagination = new PaginationInfo
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 2,
                    TotalPages = 1,
                    HasPreviousPage = false,
                    HasNextPage = false,
                    StartIndex = 1,
                    EndIndex = 2
                },
                SearchSummary = new SearchSummary
                {
                    SearchKeyword = request.SearchKeyword,
                    TagFilter = request.TagFilter,
                    SortBy = request.SortBy.ToString(),
                    SortOrder = request.SortOrder.ToString(),
                    UnfilteredCount = 2,
                    FilteredCount = 2,
                    IsFiltered = false
                }
            };

            return Ok(ApiResponse<ImageListResponse>.SuccessResponse(mockResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "개발용 이미지 목록 조회 중 오류 발생");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 목록 조회 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 현재 사용자의 이미지 목록을 조회합니다.
    /// </summary>
    /// <param name="request">조회 요청 파라미터</param>
    /// <returns>이미지 목록</returns>
    [HttpGet("my-images")]
    public async Task<IActionResult> GetMyImages([FromQuery] GetImagesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _imageService.GetUserImagesAsync(userId, request);

            return Ok(ApiResponse<ImageListResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 목록 조회 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 목록 조회 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 특정 이미지 상세 조회
    /// </summary>
    /// <param name="imageId">이미지 ID</param>
    /// <returns>이미지 상세 정보</returns>
    [HttpGet("{imageId}")]
    public async Task<IActionResult> GetImageById(Guid imageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _imageService.GetImageByIdAsync(imageId, userId);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("이미지를 찾을 수 없습니다."));
            }

            return Ok(ApiResponse<ImageResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 조회 중 오류 발생: {ImageId}, {UserId}", imageId, GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 조회 중 오류가 발생했습니다."));
        }
    }


    /// <summary>
    /// 이미지를 삭제합니다.
    /// </summary>
    /// <param name="imageId">이미지 ID</param>
    /// <returns>삭제 결과</returns>
    [HttpDelete("{imageId}")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var result = await _imageService.DeleteImageAsync(imageId, userId);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("이미지를 찾을 수 없습니다."));
            }

            _logger.LogInformation("이미지 삭제 완료: {ImageId}, {UserId}", imageId, userId);

            return Ok(ApiResponse<object>.SuccessResponse(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 삭제 중 오류 발생: {ImageId}, {UserId}", imageId, GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("이미지 삭제 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 헬스체크 엔드포인트
    /// </summary>
    /// <returns>서비스 상태</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", service = "ImageService", timestamp = DateTime.UtcNow });
    }
}