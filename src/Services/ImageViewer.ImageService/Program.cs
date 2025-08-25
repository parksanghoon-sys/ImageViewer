using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Extensions;
using ImageViewer.Infrastructure.Configuration;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Infrastructure.MessageBus;
using ImageViewer.Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Scalar.AspNetCore;

// Serilog 구성
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/imageservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog를 기본 로거로 설정
builder.Host.UseSerilog();

// 임시 로거 생성 (데이터베이스 설정 로그용)
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var tempLogger = loggerFactory.CreateLogger<Program>();

// 동적 데이터베이스 설정
builder.Services.AddConfigurableDatabase(builder.Configuration, tempLogger);

// JWT 인증 설정
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("JWT SecretKey가 설정되지 않았습니다.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// CORS 설정
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 서비스 등록
builder.Services.AddScoped<IImageService, ImageViewer.Infrastructure.Services.ImageService>();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// 백그라운드 서비스 등록
builder.Services.AddHostedService<ThumbnailGenerationService>();

// 컨트롤러 및 API 탐색기 설정
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// API 문서화 설정 (Swagger + Scalar)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ImageViewer Image Service API", Version = "v1" });
});

var app = builder.Build();

// RabbitMQ 서비스 초기화
using (var scope = app.Services.CreateScope())
{
    var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
    try
    {
        await rabbitMQService.InitializeAsync();
        app.Logger.LogInformation("RabbitMQ 서비스가 성공적으로 초기화되었습니다.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "RabbitMQ 서비스 초기화에 실패했습니다. 서비스는 계속 실행되지만 이벤트 발행이 작동하지 않을 수 있습니다.");
    }
}

// HTTP 요청 파이프라인 구성
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageViewer Image Service API";
        options.Theme = Scalar.AspNetCore.ScalarTheme.BluePlanet;
        options.ShowSidebar = true;
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });
}

app.UseHttpsRedirection();

// Static files 미들웨어 (이미지 파일 서빙을 위해)
app.UseStaticFiles();

// CORS 미들웨어 (인증 전에 위치해야 함)
app.UseCors("AllowReactApp");

// 인증 및 권한 부여
app.UseAuthentication();
app.UseAuthorization();

// 컨트롤러 매핑
app.MapControllers();

// 애플리케이션 시작 로그
app.Logger.LogInformation("ImageViewer Image Service가 시작되었습니다.");

// 헬스 체크 엔드포인트
app.MapGet("/health", () => new { Status = "Healthy", Service = "ImageService", Timestamp = DateTime.UtcNow })
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
