using ImageViewer.Contracts.Common;
using ImageViewer.Contracts.Authentication;
using ImageViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace ImageViewer.Tests.Integration;

/// <summary>
/// AuthService 통합 테스트
/// </summary>
public class AuthServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly PostgreSqlContainer _postgresContainer;
    private HttpClient _client = null!;

    public AuthServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("ImageViewerTestDB")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();

        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 기존 DbContext 제거
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // 테스트용 DbContext 추가
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                });

                // 로깅 제거 (테스트 출력 정리용)
                services.AddLogging(builder => builder.ClearProviders());
            });

            builder.UseEnvironment("Testing");
        }).CreateClient();

        // 데이터베이스 마이그레이션 실행
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_WhenCalledWithValidData_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticationResponse>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.AccessToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
        apiResponse.Data.User.Email.Should().Be(registerRequest.Email);
        apiResponse.Data.User.Username.Should().Be(registerRequest.Username);
    }

    [Fact]
    public async Task Register_WhenCalledWithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Username = "testuser1",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // 첫 번째 사용자 등록
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // 같은 이메일로 두 번째 사용자 등록 시도
        var duplicateRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Username = "testuser2",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ErrorMessage.Should().Contain("이미 사용 중인 이메일");
    }

    [Fact]
    public async Task Login_WhenCalledWithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "login_test@example.com",
            Username = "loginuser",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // 사용자 등록
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login_test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticationResponse>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.AccessToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
        apiResponse.Data.User.Email.Should().Be(loginRequest.Email);
    }

    [Fact]
    public async Task Login_WhenCalledWithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ErrorMessage.Should().Contain("이메일 또는 비밀번호가 올바르지 않습니다");
    }

    [Fact]
    public async Task RefreshToken_WhenCalledWithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "refresh_test@example.com",
            Username = "refreshuser",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // 사용자 등록 및 토큰 획득
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerApiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticationResponse>>(
            registerContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = registerApiResponse!.Data!.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshTokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticationResponse>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.AccessToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeNullOrEmpty();
        
        // 새로운 토큰들이 기존과 다른지 확인
        apiResponse.Data.AccessToken.Should().NotBe(registerApiResponse.Data.AccessToken);
        apiResponse.Data.RefreshToken.Should().NotBe(registerApiResponse.Data.RefreshToken);
    }

    [Fact]
    public async Task GetCurrentUser_WhenCalledWithValidToken_ShouldReturnUserInfo()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "currentuser_test@example.com",
            Username = "currentuser",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // 사용자 등록 및 토큰 획득
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerApiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticationResponse>>(
            registerContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Authorization 헤더 설정
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerApiResponse!.Data!.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserResponse>>(
            content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be(registerRequest.Email);
        apiResponse.Data.Username.Should().Be(registerRequest.Username);
    }

    [Fact]
    public async Task GetCurrentUser_WhenCalledWithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_WhenCalled_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("AuthService");
    }

    [Theory]
    [InlineData("", "testuser", "TestPassword123!", "TestPassword123!")] // 빈 이메일
    [InlineData("invalid-email", "testuser", "TestPassword123!", "TestPassword123!")] // 잘못된 이메일
    [InlineData("test@example.com", "", "TestPassword123!", "TestPassword123!")] // 빈 사용자명
    [InlineData("test@example.com", "testuser", "123", "123")] // 짧은 비밀번호
    [InlineData("test@example.com", "testuser", "TestPassword123!", "DifferentPassword123!")] // 비밀번호 불일치
    public async Task Register_WhenCalledWithInvalidData_ShouldReturnBadRequest(
        string email, string username, string password, string confirmPassword)
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}