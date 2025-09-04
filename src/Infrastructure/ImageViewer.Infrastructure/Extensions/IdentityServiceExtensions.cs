using ImageViewer.Domain.Identity;
using ImageViewer.Domain.Enums;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Extensions;

/// <summary>
/// Identity 서비스 확장 메서드
/// </summary>
public static class IdentityServiceExtensions
{
    /// <summary>
    /// Identity 관련 서비스들을 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정</param>
    /// <param name="logger">로거</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILogger? logger = null)
    {
        // Database 설정 바인딩
        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // 설정 유효성 검증
        if (!databaseOptions.IsValid())
        {
            var errorMessage = $"유효하지 않은 데이터베이스 설정: {databaseOptions.Type}";
            logger?.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // AuthContext 데이터베이스 설정
        services.AddDbContext<AuthContext>(options =>
        {
            var connectionString = databaseOptions.GetConnectionString();
            
            switch (databaseOptions.Type)
            {
                case DatabaseType.InMemory:
                    options.UseInMemoryDatabase(connectionString);
                    logger?.LogInformation("InMemory Auth 데이터베이스 설정: {DatabaseName}", connectionString);
                    break;
                    
                case DatabaseType.PostgreSQL:
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(3);
                    });
                    logger?.LogInformation("PostgreSQL Auth 데이터베이스 설정 완료");
                    break;
                    
                case DatabaseType.SqlServer:
                    options.UseSqlServer(connectionString, sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(3);
                    });
                    logger?.LogInformation("SQL Server Auth 데이터베이스 설정 완료");
                    break;
                    
                default:
                    throw new NotSupportedException($"지원하지 않는 데이터베이스 타입: {databaseOptions.Type}");
            }

            // 개발 환경에서는 상세한 로깅 활성화
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Identity 서비스 등록
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // 비밀번호 정책
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            
            // 사용자 정책
            options.User.RequireUniqueEmail = true;
            
            // 로그인 정책
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AuthContext>()
        .AddDefaultTokenProviders();

        logger?.LogInformation("Identity 서비스가 등록되었습니다. 데이터베이스 타입: {DatabaseType}", 
            databaseOptions.GetDatabaseTypeString());

        return services;
    }

    /// <summary>
    /// 데이터베이스를 초기화하고 시드 데이터를 적용합니다
    /// </summary>
    /// <param name="serviceProvider">서비스 프로바이더</param>
    /// <param name="logger">로거</param>
    /// <returns>비동기 작업</returns>
    public static async Task InitializeIdentityDatabaseAsync(
        this IServiceProvider serviceProvider, 
        ILogger? logger = null)
    {
        using var scope = serviceProvider.CreateScope();
        
        try
        {
            var authContext = scope.ServiceProvider.GetRequiredService<AuthContext>();
            var databaseOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            
            // 데이터베이스 타입에 따른 초기화
            if (databaseOptions.Type == DatabaseType.InMemory)
            {
                // InMemory 데이터베이스 초기화
                await authContext.Database.EnsureCreatedAsync();
                logger?.LogInformation("InMemory Auth 데이터베이스가 초기화되었습니다.");
            }
            else
            {
                // 실제 데이터베이스 마이그레이션 적용
                var pendingMigrations = await authContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger?.LogInformation("대기 중인 마이그레이션 적용 중...");
                    await authContext.Database.MigrateAsync();
                    logger?.LogInformation("데이터베이스 마이그레이션이 완료되었습니다.");
                }
                else
                {
                    logger?.LogInformation("적용할 마이그레이션이 없습니다.");
                }
            }
            
            // 시드 데이터 확인
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userCount = authContext.Users.Count();
            logger?.LogInformation("Auth 데이터베이스에 {UserCount}명의 사용자가 등록되어 있습니다. (DB 타입: {DatabaseType})", 
                userCount, databaseOptions.GetDatabaseTypeString());
            
            if (userCount > 0)
            {
                var adminUser = await userManager.FindByEmailAsync("admin@imageviewer.com");
                if (adminUser != null)
                {
                    logger?.LogInformation("기본 관리자 계정이 존재합니다: {Email}", adminUser.Email);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Identity 데이터베이스 초기화 중 오류 발생");
            throw;
        }
    }
}