using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using ImageViewer.GatewayService.Configuration;
using ImageViewer.GatewayService.Services;
using ImageViewer.GatewayService.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Serilog 설정
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 설정 바인딩
var gatewaySettings = new GatewaySettings();
builder.Configuration.GetSection(GatewaySettings.SectionName).Bind(gatewaySettings);
builder.Services.AddSingleton(gatewaySettings);

// 서비스 등록
builder.Services.AddControllers();

// HTTP 클라이언트 설정
builder.Services.AddHttpClient<IHttpClientService, HttpClientService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(gatewaySettings.Retry.TimeoutSeconds);
});

// JWT 인증 설정
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(gatewaySettings.Jwt.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = gatewaySettings.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = gatewaySettings.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS 설정
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        var corsSettings = gatewaySettings.Cors;
        
        if (corsSettings.AllowAnyOrigin)
        {
            corsBuilder.AllowAnyOrigin();
        }
        else
        {
            corsBuilder.WithOrigins(corsSettings.AllowedOrigins);
        }

        if (corsSettings.AllowAnyHeader)
        {
            corsBuilder.AllowAnyHeader();
        }

        if (corsSettings.AllowAnyMethod)
        {
            corsBuilder.AllowAnyMethod();
        }

        if (corsSettings.AllowCredentials && !corsSettings.AllowAnyOrigin)
        {
            corsBuilder.AllowCredentials();
        }
    });
});

// Swagger/Scalar 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ImageViewer Gateway API", 
        Version = "v1",
        Description = "ImageViewer 애플리케이션의 API Gateway입니다."
    });
    
    // JWT Bearer 인증 설정
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// 미들웨어 파이프라인 구성
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageViewer Gateway API";
        options.Theme = Scalar.AspNetCore.ScalarTheme.BluePlanet;
        options.ShowSidebar = true;
    });
}

// 요청 로깅 미들웨어 (가장 먼저)
app.UseMiddleware<RequestLoggingMiddleware>();

// CORS
app.UseCors();

// HTTPS 리디렉션
app.UseHttpsRedirection();

// JWT 미들웨어 (인증 전에)
app.UseMiddleware<JwtMiddleware>();

// 인증 및 권한 부여
app.UseAuthentication();
app.UseAuthorization();

// 컨트롤러 매핑
app.MapControllers();

// 헬스체크 엔드포인트
app.MapGet("/health", () => Results.Ok(new
{
    service = "ImageViewer.GatewayService",
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

try
{
    Log.Information("ImageViewer Gateway 서비스를 시작합니다...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway 서비스 시작 중 오류가 발생했습니다");
}
finally
{
    Log.CloseAndFlush();
}

// 테스트를 위해 Program 클래스를 public으로 만듦
public partial class Program { }
