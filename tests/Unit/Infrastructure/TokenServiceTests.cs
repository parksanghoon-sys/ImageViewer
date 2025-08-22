using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ImageViewer.Tests.Unit.Infrastructure;

/// <summary>
/// TokenService에 대한 단위 테스트
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly Fixture _fixture;

    public TokenServiceTests()
    {
        _fixture = new Fixture();
        
        // 테스트용 설정 생성
        var configurationData = new Dictionary<string, string>
        {
            {"Jwt:SecretKey", "test-secret-key-for-unit-testing-minimum-256-bits"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenExpirationMinutes", "15"},
            {"Jwt:RefreshTokenExpirationDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_WhenCalledWithValidUserId_ShouldReturnValidJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";

        // Act
        var (token, _) = _tokenService.GenerateAccessToken(userId, email, username, UserRole.User);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // JWT 토큰 파싱하여 검증
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
        
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        
        // 만료 시간 확인 (15분)
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_WhenCalled_ShouldReturnBase64String()
    {
        // Act
        var (refreshToken, expiresAt) = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(0);
        expiresAt.Should().BeAfter(DateTime.UtcNow);
        
        // Base64 문자열인지 확인
        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetUserIdFromToken_WhenCalledWithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";
        var (token, _) = _tokenService.GenerateAccessToken(expectedUserId, email, username, UserRole.User);

        // Act
        var actualUserId = _tokenService.GetUserIdFromToken(token);

        // Assert
        actualUserId.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetUserIdFromToken_WhenCalledWithInvalidToken_ShouldThrowException()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act & Assert
        var act = () => _tokenService.GetUserIdFromToken(invalidToken);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetUserIdFromToken_WhenCalledWithNullToken_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _tokenService.GetUserIdFromToken(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetUserIdFromToken_WhenCalledWithEmptyToken_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _tokenService.GetUserIdFromToken(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateToken_WhenCalledWithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";
        var (token, _) = _tokenService.GenerateAccessToken(userId, email, username, UserRole.User);

        // Act
        var isValid = _tokenService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WhenCalledWithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _tokenService.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenCalledWithNullToken_ShouldReturnFalse()
    {
        // Act
        var isValid = _tokenService.ValidateToken(null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenCalledWithEmptyToken_ShouldReturnFalse()
    {
        // Act
        var isValid = _tokenService.ValidateToken(string.Empty);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_WhenCalledMultipleTimes_ShouldReturnDifferentTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";

        // Act
        var (token1, _) = _tokenService.GenerateAccessToken(userId, email, username, UserRole.User);
        var (token2, _) = _tokenService.GenerateAccessToken(userId, email, username, UserRole.User);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateRefreshToken_WhenCalledMultipleTimes_ShouldReturnDifferentTokens()
    {
        // Act
        var (token1, _) = _tokenService.GenerateRefreshToken();
        var (token2, _) = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Theory]
    [AutoData]
    public void GenerateAccessToken_WhenCalledWithDifferentUserData_ShouldReturnTokensWithCorrectClaims(
        Guid userId, string email, string username)
    {
        // Act
        var (token, _) = _tokenService.GenerateAccessToken(userId, email, username, UserRole.User);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
    }

    [Fact]
    public void ValidateToken_WhenCalledWithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        // 만료된 토큰을 시뮬레이션하기 위해 음수 만료 시간으로 설정
        var configurationData = new Dictionary<string, string>
        {
            {"Jwt:SecretKey", "test-secret-key-for-unit-testing-minimum-256-bits"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenExpirationMinutes", "-1"}, // 이미 만료된 토큰
            {"Jwt:RefreshTokenExpirationDays", "7"}
        };

        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var expiredTokenService = new TokenService(expiredConfig);
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";
        var (expiredToken, _) = expiredTokenService.GenerateAccessToken(userId, email, username, UserRole.User);

        // Act
        var isValid = _tokenService.ValidateToken(expiredToken);

        // Assert
        isValid.Should().BeFalse();
    }
}