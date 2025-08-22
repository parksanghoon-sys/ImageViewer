using ImageViewer.Infrastructure.Extensions;
using ImageViewer.Infrastructure.Services;
using Serilog;
using Scalar.AspNetCore;

// Serilog 설정
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .WriteTo.Console()
    .WriteTo.File("logs/auth-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog를 기본 로거로 사용
builder.Host.UseSerilog();

// 임시 로거 생성
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var tempLogger = loggerFactory.CreateLogger<Program>();

// === 서비스 등록 ===
// Identity 서비스 등록
builder.Services.AddIdentityServices(builder.Configuration, tempLogger);

// JWT 인증 및 권한 부여 서비스 등록
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleBasedAuthorization();

// 비즈니스 서비스 등록
builder.Services.AddScoped<TokenService>();

// 컨트롤러 및 API 서비스
builder.Services.AddControllers();

// CORS 설정 (개발 환경용)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// API 문서화 설정
builder.Services.AddSwaggerWithJwtAuth(
    title: "ImageViewer Auth Service API",
    version: "v1",
    description: "이미지 뷰어 인증 서비스 API"
);

var app = builder.Build();

// === 데이터베이스 초기화 ===
await app.Services.InitializeIdentityDatabaseAsync(app.Logger);

// === HTTP 요청 파이프라인 구성 ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageViewer Auth Service API";
        options.Theme = ScalarTheme.BluePlanet;
        options.ShowSidebar = true;
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });
}

app.UseHttpsRedirection();

// CORS 미들웨어 (개발 환경용)
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCorsPolicy");
}

// 인증 및 권한 부여 미들웨어
app.UseAuthentication();
app.UseAuthorization();

// 컨트롤러 라우팅
app.MapControllers();

// 헬스 체크 엔드포인트
app.MapGet("/health", () => new 
{ 
    Status = "Healthy", 
    Service = "AuthService", 
    Timestamp = DateTime.UtcNow,
    Database = "InMemory"
})
.WithName("HealthCheck")
.WithOpenApi();

// 애플리케이션 시작 로그
app.Logger.LogInformation("ImageViewer Auth Service가 시작되었습니다.");

app.Run();

// 테스트를 위해 Program 클래스를 public으로 만듦
public partial class Program { }