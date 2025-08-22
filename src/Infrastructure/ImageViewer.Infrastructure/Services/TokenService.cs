using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// JWT 토큰 생성 및 검증을 담당하는 서비스
/// 액세스 토큰과 리프레시 토큰의 생성, 검증, 갱신 기능을 제공
/// </summary>
public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    /// <summary>
    /// TokenService 생성자
    /// </summary>
    /// <param name="configuration">애플리케이션 설정</param>
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // JWT 설정 값들을 Configuration에서 읽어옴
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey가 설정되지 않았습니다.");
        _issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer가 설정되지 않았습니다.");
        _audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience가 설정되지 않았습니다.");
        
        // 토큰 만료 시간 설정 (기본값 포함)
        _accessTokenExpirationMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var accessMinutes) ? accessMinutes : 15;
        _refreshTokenExpirationDays = int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var refreshDays) ? refreshDays : 7;
    }

    /// <summary>
    /// 사용자에 대한 JWT 액세스 토큰을 생성
    /// </summary>
    /// <param name="userId">사용자 ID</param>
    /// <param name="email">사용자 이메일</param>
    /// <param name="username">사용자명</param>
    /// <param name="role">사용자 역할</param>
    /// <returns>생성된 액세스 토큰과 만료 시간</returns>
    public (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email, string username, UserRole role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

        // JWT 클레임 설정
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 토큰 디스크립터 생성
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // 토큰 생성 및 반환
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiresAt);
    }

    /// <summary>
    /// 리프레시 토큰 생성
    /// </summary>
    /// <returns>생성된 리프레시 토큰과 만료 시간</returns>
    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        // 암호학적으로 안전한 랜덤 바이트 배열 생성
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        var refreshToken = Convert.ToBase64String(randomBytes);
        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

        return (refreshToken, expiresAt);
    }

    /// <summary>
    /// JWT 액세스 토큰을 검증하고 클레임을 추출
    /// </summary>
    /// <param name="token">검증할 토큰</param>
    /// <returns>검증 성공 시 클레임 프린시pal, 실패 시 null</returns>
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            // 토큰 검증 파라미터 설정
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // 토큰 만료 시간에 여유시간을 두지 않음
            };

            // 토큰 검증 실행
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // HMAC SHA256으로 서명된 토큰인지 확인
            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return principal;
            }

            return null;
        }
        catch (Exception)
        {
            // 토큰 검증 실패 시 null 반환
            return null;
        }
    }

    /// <summary>
    /// 토큰에서 사용자 ID를 추출
    /// </summary>
    /// <param name="token">JWT 토큰</param>
    /// <returns>사용자 ID, 추출 실패 시 null</returns>
    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateAccessToken(token);
        var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            return null;

        return userId;
    }

    /// <summary>
    /// 토큰의 만료 시간을 가져옴 (검증 없이)
    /// </summary>
    /// <param name="token">JWT 토큰</param>
    /// <returns>만료 시간, 파싱 실패 시 null</returns>
    public DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 토큰이 곧 만료되는지 확인 (만료 5분 전)
    /// </summary>
    /// <param name="token">JWT 토큰</param>
    /// <returns>곧 만료되면 true, 그렇지 않으면 false</returns>
    public bool IsTokenExpiringSoon(string token)
    {
        var expiration = GetTokenExpiration(token);
        if (expiration == null) return true;

        return expiration.Value <= DateTime.UtcNow.AddMinutes(5);
    }

    /// <summary>
    /// 토큰이 유효한지 검증
    /// </summary>
    /// <param name="token">JWT 토큰</param>
    /// <returns>토큰이 유효하면 true, 그렇지 않으면 false</returns>
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var principal = ValidateAccessToken(token);
        return principal != null;
    }
}