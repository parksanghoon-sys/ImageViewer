using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ImageViewer.Infrastructure.Extensions;

/// <summary>
/// Swagger/OpenAPI 서비스 확장 메서드
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Swagger와 JWT Bearer 인증을 포함한 API 문서 서비스를 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="title">API 제목</param>
    /// <param name="version">API 버전</param>
    /// <param name="description">API 설명</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddSwaggerWithJwtAuth(
        this IServiceCollection services,
        string title = "API",
        string version = "v1",
        string? description = null)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            // JWT Bearer 인증 설정
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT 인증 헤더. 'Bearer {token}' 형식으로 입력하세요.",
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
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
        });

        return services;
    }
}