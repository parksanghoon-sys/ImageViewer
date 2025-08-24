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
[Authorize]
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
                return "test-user-id";
            }
            throw new UnauthorizedAccessException("유효하지 않은 사용자입니다.");
        }
        return userIdClaim;
    }

    /// <summary>
    /// 이미지 공유 요청을 생성합니다.
    /// </summary>
    /// <param name="imageId">공유할 이미지 ID</param>
    /// <param name="targetUserEmail">공유 대상 사용자 이메일</param>
    /// <param name="message">공유 메시지 (선택사항)</param>
    /// <returns>공유 요청 결과</returns>
    [HttpPost("request")]
    public async Task<IActionResult> CreateShareRequest(
        [FromForm] Guid imageId,
        [FromForm] string targetUserEmail,
        [FromForm] string? message = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            // 이미지 소유권 확인
            var image = await _context.Images
                .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

            if (image == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("이미지를 찾을 수 없거나 공유 권한이 없습니다."));
            }

            // 대상 사용자 ID 검증 (이메일 대신 사용자 ID 직접 사용)
            // AuthContext에서 사용자 검증은 별도로 수행
            var targetUserId = targetUserEmail; // 파라미터명 변경 예정

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
                    ImageFileName = image.OriginalFileName,
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
                OwnerEmail = targetUserEmail,
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
                .Include(sr => sr.Image)
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
                    ImageFileName = sr.Image.OriginalFileName,
                    RequesterId = sr.RequesterId,
                    RequesterEmail = "N/A", // AuthContext에서 조회 필요
                    RequesterName = "N/A", // AuthContext에서 조회 필요
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
                .Include(sr => sr.Image)
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
                    ImageFileName = sr.Image.OriginalFileName,
                    TargetEmail = "N/A", // AuthContext에서 조회 필요
                    Ownername = "N/A", // AuthContext에서 조회 필요
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

            var shareRequest = await _context.ShareRequests
                .Include(sr => sr.Image)
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
                    ImageFileName = shareRequest.Image.OriginalFileName,
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
                .Include(sr => sr.Image)
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
                .Include(sr => sr.Image)
                .Where(sr => sr.OwnerId == userId && sr.Status == ShareRequestStatus.Approved)
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
                    OriginalFileName = sr.Image.OriginalFileName,
                    FileSize = sr.Image.FileSize,
                    Width = sr.Image.Width,
                    Height = sr.Image.Height,
                    Description = sr.Image.Description,
                    OwnerId = sr.RequesterId,
                    OwnerEmail = "N/A", // AuthContext에서 조회 필요
                    OwnerUsername = "N/A", // AuthContext에서 조회 필요
                    SharedAt = sr.RespondedAt,
                    ThumbnailPath = sr.Image.ThumbnailPath
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