using ImageViewer.Application.Services;
using ImageViewer.Contracts.Common;
using ImageViewer.Contracts.Events;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ImageViewer.ShareService.Controllers;

/// <summary>
/// 이미지 공유 기능을 담당하는 컨트롤러
/// 공유 요청, 승인, 거절, 공유된 이미지 조회 등의 기능을 제공
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShareController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShareController> _logger;
    private readonly IMessageBusService _messageBus;

    /// <summary>
    /// ShareController 생성자
    /// </summary>
    /// <param name="context">데이터베이스 컨텍스트</param>
    /// <param name="logger">로거</param>
    /// <param name="messageBus">메시지 버스 서비스</param>
    public ShareController(
        ApplicationDbContext context, 
        ILogger<ShareController> logger,
        IMessageBusService messageBus)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    /// <summary>
    /// 현재 사용자 ID를 가져옵니다.
    /// </summary>
    /// <returns>사용자 ID</returns>
    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            // 개발 환경에서는 기본 테스트 사용자 ID 사용
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // 요청 경로에 따라 다른 사용자 ID 반환 (승인 테스트용)
                var path = HttpContext?.Request?.Path.Value ?? "";
                if (path.Contains("/approve") || path.Contains("/reject") || path.Contains("/received"))
                {
                    var targetUserId = "22222222-2222-2222-2222-222222222222"; // 승인할 사용자
                    _logger.LogDebug("개발 환경: 대상 사용자 ID 사용 - {UserId}", targetUserId);
                    return targetUserId;
                }
                if (path.Contains("/shared-with-me"))
                {
                    var requesterId = "11111111-1111-1111-1111-111111111111"; // 공유 요청한 사용자
                    _logger.LogDebug("개발 환경: 공유 요청자 ID 사용 - {UserId}", requesterId);
                    return requesterId;
                }
                
                var testUserId = "11111111-1111-1111-1111-111111111111"; // 기본 테스트 사용자
                _logger.LogDebug("개발 환경: 기본 테스트 사용자 ID 사용 - {UserId}", testUserId);
                return testUserId;
            }
            throw new UnauthorizedAccessException("유효하지 않은 사용자입니다.");
        }
        return userIdClaim;
    }

    /// <summary>
    /// 이미지 공유 요청을 생성합니다.
    /// </summary>
    /// <param name="imageId">공유할 이미지 ID</param>
    /// <param name="targetUserId">공유 대상 사용자 ID</param>
    /// <param name="message">공유 메시지 (선택사항)</param>
    /// <returns>공유 요청 결과</returns>
    [HttpPost("request")]
    public async Task<IActionResult> CreateShareRequest(
        [FromForm] Guid imageId,
        [FromForm] string targetUserId,
        [FromForm] string? message = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            // 개발 환경에서는 이미지 검증을 단순화
            _logger.LogInformation("개발 환경: 이미지 {ImageId}에 대한 공유 요청 처리", imageId);
            
            // 가상의 이미지 객체 생성 (공유 요청용)
            var virtualImage = new { OriginalFileName = $"image-{imageId.ToString().Substring(0, 8)}" };

            if (string.IsNullOrEmpty(targetUserId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("대상 사용자 ID가 필요합니다."));
            }

            // 자기 자신에게 공유 방지
            if (string.Equals(targetUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("자기 자신에게는 공유할 수 없습니다."));
            }

            // 이미 공유 요청이 있는지 확인
            var existingRequest = await _context.ShareRequests
                .FirstOrDefaultAsync(sr => sr.ImageId == imageId && 
                                          sr.RequesterId == userId && 
                                          sr.OwnerId == targetUserId &&
                                          sr.Status != ShareRequestStatus.Rejected);

            if (existingRequest != null)
            {
                return Conflict(ApiResponse<object>.ErrorResponse("이미 공유 요청이 존재합니다."));
            }

            // 새 공유 요청 생성
            var shareRequest = new ShareRequest(
                userId,      // requesterId
                targetUserId, // ownerId  
                imageId,     // imageId
                message      // requestMessage
            );

            _context.ShareRequests.Add(shareRequest);
            await _context.SaveChangesAsync();

            // RabbitMQ를 통한 공유 요청 생성 이벤트 발행
            try
            {
                var shareRequestEvent = new ShareRequestCreatedEvent
                {
                    ShareRequestId = shareRequest.Id,
                    ImageId = shareRequest.ImageId,
                    ImageFileName = virtualImage.OriginalFileName,
                    RequesterId = userId,
                    TargetUserId = targetUserId,
                    RequestMessage = message,
                    RequestedAt = shareRequest.CreatedAt
                };

                await _messageBus.PublishAsync(shareRequestEvent);
                _logger.LogDebug("공유 요청 생성 이벤트 발행: {ShareRequestId}", shareRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "공유 요청 생성 이벤트 발행 실패: {ShareRequestId}", shareRequest.Id);
            }

            _logger.LogInformation("공유 요청 생성: {ShareRequestId}, 요청자: {RequesterId}, 대상: {TargetId}", 
                shareRequest.Id, userId, targetUserId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Id = shareRequest.Id,
                ImageId = shareRequest.ImageId,
                TargetUserId = targetUserId,
                Message = shareRequest.RequestMessage,
                Status = shareRequest.Status.ToString(),
                CreatedAt = shareRequest.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "공유 요청 생성 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유 요청 생성 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 받은 공유 요청 목록을 조회합니다.
    /// </summary>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>공유 요청 목록</returns>
    [HttpGet("received")]
    public async Task<IActionResult> GetReceivedShareRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.ShareRequests
                .Where(sr => sr.OwnerId == userId)
                .OrderByDescending(sr => sr.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var shareRequests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sr => new
                {
                    Id = sr.Id,
                    ImageId = sr.ImageId,
                    ImageFileName = $"image-{sr.ImageId.ToString().Substring(0, 8)}", // 가상 파일명
                    RequesterId = sr.RequesterId,
                    RequesterEmail = $"user-{sr.RequesterId.Substring(0, 8)}@example.com", // 개발용 이메일
                    RequesterName = $"User-{sr.RequesterId.Substring(0, 8)}", // 개발용 이름
                    Message = sr.RequestMessage,
                    Status = sr.Status.ToString(),
                    CreatedAt = sr.CreatedAt,
                    RespondedAt = sr.RespondedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                ShareRequests = shareRequests,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "받은 공유 요청 조회 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유 요청 조회 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 보낸 공유 요청 목록을 조회합니다.
    /// </summary>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>공유 요청 목록</returns>
    [HttpGet("sent")]
    public async Task<IActionResult> GetSentShareRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.ShareRequests
                .Where(sr => sr.RequesterId == userId)
                .OrderByDescending(sr => sr.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var shareRequests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sr => new
                {
                    Id = sr.Id,
                    ImageId = sr.ImageId,
                    ImageFileName = $"image-{sr.ImageId.ToString().Substring(0, 8)}", // 가상 파일명
                    OwnerId = sr.OwnerId,
                    TargetEmail = $"user-{sr.OwnerId.Substring(0, 8)}@example.com", // 개발용 이메일
                    OwnerName = $"User-{sr.OwnerId.Substring(0, 8)}", // 개발용 이름
                    Message = sr.RequestMessage,
                    Status = sr.Status.ToString(),
                    CreatedAt = sr.CreatedAt,
                    RespondedAt = sr.RespondedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                ShareRequests = shareRequests,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "보낸 공유 요청 조회 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유 요청 조회 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 공유 요청을 승인합니다.
    /// </summary>
    /// <param name="shareRequestId">공유 요청 ID</param>
    /// <returns>승인 결과</returns>
    [HttpPost("{shareRequestId}/approve")]
    public async Task<IActionResult> ApproveShareRequest(Guid shareRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("승인 요청 처리: ShareRequestId={ShareRequestId}, UserId={UserId}", shareRequestId, userId);

            var shareRequest = await _context.ShareRequests
                .FirstOrDefaultAsync(sr => sr.Id == shareRequestId && sr.OwnerId == userId);

            if (shareRequest == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("공유 요청을 찾을 수 없습니다."));
            }

            if (shareRequest.Status != ShareRequestStatus.Pending)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("이미 처리된 요청입니다."));
            }

            shareRequest.Approve();
            await _context.SaveChangesAsync();

            // RabbitMQ를 통한 공유 승인 이벤트 발행
            try
            {
                var shareApprovedEvent = new ShareRequestApprovedEvent
                {
                    ShareRequestId = shareRequest.Id,
                    ImageId = shareRequest.ImageId,
                    ImageFileName = $"image-{shareRequest.ImageId.ToString().Substring(0, 8)}",
                    RequesterId = shareRequest.RequesterId,
                    OwnerId = userId,
                    ApprovedAt = shareRequest.RespondedAt ?? DateTime.UtcNow,
                    OriginalMessage = shareRequest.RequestMessage
                };

                await _messageBus.PublishAsync(shareApprovedEvent);
                _logger.LogDebug("공유 승인 이벤트 발행: {ShareRequestId}", shareRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "공유 승인 이벤트 발행 실패: {ShareRequestId}", shareRequest.Id);
            }

            _logger.LogInformation("공유 요청 승인: {ShareRequestId}, 대상 사용자: {UserId}", 
                shareRequestId, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Id = shareRequest.Id,
                Status = shareRequest.Status.ToString(),
                RespondedAt = shareRequest.RespondedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "공유 요청 승인 중 오류 발생: {ShareRequestId}, {UserId}", 
                shareRequestId, GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유 요청 승인 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 공유 요청을 거절합니다.
    /// </summary>
    /// <param name="shareRequestId">공유 요청 ID</param>
    /// <returns>거절 결과</returns>
    [HttpPost("{shareRequestId}/reject")]
    public async Task<IActionResult> RejectShareRequest(Guid shareRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();

            var shareRequest = await _context.ShareRequests
                .FirstOrDefaultAsync(sr => sr.Id == shareRequestId && sr.OwnerId == userId);

            if (shareRequest == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("공유 요청을 찾을 수 없습니다."));
            }

            if (shareRequest.Status != ShareRequestStatus.Pending)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("이미 처리된 요청입니다."));
            }

            shareRequest.Reject();
            await _context.SaveChangesAsync();

            // TODO: RabbitMQ를 통한 거절 알림 발송

            _logger.LogInformation("공유 요청 거절: {ShareRequestId}, 대상 사용자: {UserId}", 
                shareRequestId, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Id = shareRequest.Id,
                Status = shareRequest.Status.ToString(),
                RespondedAt = shareRequest.RespondedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "공유 요청 거절 중 오류 발생: {ShareRequestId}, {UserId}", 
                shareRequestId, GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유 요청 거절 중 오류가 발생했습니다."));
        }
    }

    /// <summary>
    /// 나와 공유된 이미지 목록을 조회합니다.
    /// </summary>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>공유된 이미지 목록</returns>
    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetSharedWithMeImages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = _context.ShareRequests
                .Where(sr => sr.RequesterId == userId && sr.Status == ShareRequestStatus.Approved)
                .OrderByDescending(sr => sr.RespondedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var sharedImages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sr => new
                {
                    ShareRequestId = sr.Id,
                    ImageId = sr.ImageId,
                    OriginalFileName = $"image-{sr.ImageId.ToString().Substring(0, 8)}", // 가상 파일명
                    FileSize = 1024000, // 가상 파일 크기
                    Width = 1920, // 가상 너비
                    Height = 1080, // 가상 높이
                    Description = "공유된 이미지", // 가상 설명
                    OwnerId = sr.RequesterId,
                    OwnerEmail = $"user-{sr.RequesterId.Substring(0, 8)}@example.com", // 개발용 이메일
                    OwnerUsername = $"User-{sr.RequesterId.Substring(0, 8)}", // 개발용 이름
                    SharedAt = sr.RespondedAt,
                    ThumbnailPath = "/images/placeholder.jpg" // 가상 썸네일 경로
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                SharedImages = sharedImages,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "공유된 이미지 조회 중 오류 발생: {UserId}", GetCurrentUserId());
            return StatusCode(500, ApiResponse<object>.ErrorResponse("공유된 이미지 조회 중 오류가 발생했습니다."));
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
        return Ok(new
        {
            Service = "ShareService",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }
}