using ImageViewer.Contracts.Authentication;
using ImageViewer.Contracts.Common;
using ImageViewer.Domain.Identity;
using ImageViewer.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImageViewer.AuthService.Controllers;

/// <summary>
/// 인증 관련 API 컨트롤러
/// Identity 기반 사용자 관리
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService _tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        this._tokenService = _tokenService;
        _logger = logger;
    }

    /// <summary>
    /// 사용자 회원가입
    /// </summary>
    /// <param name="request">회원가입 요청</param>
    /// <returns>회원가입 결과</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("회원가입 시도: {Email}", request.Email);

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(ApiResponse.ErrorResponse("입력 데이터가 유효하지 않습니다.", "INVALID_INPUT"));
            }

            // 이미 존재하는 이메일인지 확인
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse.ErrorResponse("이미 사용 중인 이메일입니다.", "EMAIL_ALREADY_EXISTS"));
            }

            // 새 사용자 생성
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(ApiResponse.ErrorResponse("회원가입에 실패했습니다.", "REGISTRATION_FAILED"));
            }

            // 역할 추가
            var roleResult = await _userManager.AddToRoleAsync(user, request.Role.ToString());
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("역할 할당 실패: {UserId}, {Role}", user.Id, request.Role);
            }

            _logger.LogInformation("회원가입 성공: {UserId}", user.Id);

            // JWT 토큰 생성
            var (accessToken, accessTokenExpires) = _tokenService.GenerateAccessToken(
                Guid.Parse(user.Id), 
                user.Email!, 
                user.UserName!, 
                user.Role);
            var (refreshToken, refreshTokenExpires) = _tokenService.GenerateRefreshToken();

            var response = new AuthenticationResponse
            {
                User = new UserResponse
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email!,
                    Username = user.UserName!,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt.ToString("O")
                },
                UserId = Guid.Parse(user.Id),
                Email = user.Email!,
                Username = user.UserName!,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpires,
                RefreshTokenExpiresAt = refreshTokenExpires,
                TokenType = "Bearer"
            };

            return Ok(ApiResponse<AuthenticationResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "회원가입 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("회원가입 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// 사용자 로그인
    /// </summary>
    /// <param name="request">로그인 요청</param>
    /// <returns>로그인 결과</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("로그인 시도: {Email}", request.Email);

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(ApiResponse.ErrorResponse("입력 데이터가 유효하지 않습니다.", "INVALID_INPUT"));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse.ErrorResponse("이메일 또는 비밀번호가 올바르지 않습니다.", "INVALID_CREDENTIALS"));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse.ErrorResponse("이메일 또는 비밀번호가 올바르지 않습니다.", "INVALID_CREDENTIALS"));
            }

            // 로그인 시간 업데이트
            user.RecordLogin();
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("로그인 성공: {UserId}", user.Id);

            // JWT 토큰 생성
            var (accessToken, accessTokenExpires) = _tokenService.GenerateAccessToken(
                Guid.Parse(user.Id), 
                user.Email!, 
                user.UserName!, 
                user.Role);
            var (refreshToken, refreshTokenExpires) = _tokenService.GenerateRefreshToken();

            var response = new AuthenticationResponse
            {
                User = new UserResponse
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email!,
                    Username = user.UserName!,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt.ToString("O")
                },
                UserId = Guid.Parse(user.Id),
                Email = user.Email!,
                Username = user.UserName!,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpires,
                RefreshTokenExpiresAt = refreshTokenExpires,
                TokenType = "Bearer"
            };

            return Ok(ApiResponse<AuthenticationResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그인 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("로그인 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// 현재 사용자 정보 조회
    /// </summary>
    /// <returns>사용자 정보</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(ApiResponse.ErrorResponse("사용자 정보를 찾을 수 없습니다.", "USER_NOT_FOUND"));
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse.ErrorResponse("사용자를 찾을 수 없습니다.", "USER_NOT_FOUND"));
            }

            var response = new UserResponse
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email!,
                Username = user.UserName!,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.ToString("O")
            };

            return Ok(ApiResponse<UserResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 정보 조회 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("사용자 정보 조회 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// 사용자 로그아웃
    /// </summary>
    /// <returns>로그아웃 응답</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogInformation("사용자 로그아웃: {UserId}", userIdClaim);
                await _signInManager.SignOutAsync();
            }

            return Ok(ApiResponse.SuccessResponse("성공적으로 로그아웃되었습니다."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그아웃 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("로그아웃 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// 모든 사용자 목록 조회 (관리자 전용)
    /// </summary>
    /// <returns>사용자 목록</returns>
    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = _userManager.Users
                .Where(u => u.IsActive)
                .Select(u => new UserResponse
                {
                    Id = Guid.Parse(u.Id),
                    Email = u.Email!,
                    Username = u.UserName!,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt.ToString("O")
                })
                .ToList();

            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 목록 조회 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("사용자 목록 조회 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// 사용자 역할 변경 (관리자 전용)
    /// </summary>
    /// <param name="userId">변경할 사용자 ID</param>
    /// <param name="request">역할 변경 요청</param>
    /// <returns>변경 결과</returns>
    [HttpPut("users/{userId}/role")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(ApiResponse.ErrorResponse("입력 데이터가 유효하지 않습니다.", "INVALID_INPUT"));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResponse("사용자를 찾을 수 없습니다.", "USER_NOT_FOUND"));
            }

            // 기존 역할 제거
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // 새 역할 할당
            user.ChangeRole(request.NewRole);
            await _userManager.UpdateAsync(user);
            await _userManager.AddToRoleAsync(user, request.NewRole.ToString());

            _logger.LogInformation("사용자 역할 변경: {UserId}, {OldRole} -> {NewRole}, 사유: {Reason}", 
                userId, user.Role, request.NewRole, request.Reason);

            return Ok(ApiResponse.SuccessResponse("사용자 역할이 변경되었습니다."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 역할 변경 중 오류 발생");
            return StatusCode(500, ApiResponse.ErrorResponse("사용자 역할 변경 중 서버 오류가 발생했습니다.", "INTERNAL_SERVER_ERROR"));
        }
    }
}