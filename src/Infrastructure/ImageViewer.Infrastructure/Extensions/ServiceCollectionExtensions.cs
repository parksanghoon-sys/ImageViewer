using ImageViewer.Domain.Enums;
using ImageViewer.Infrastructure.Configuration;
using ImageViewer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Extensions;

/// <summary>
/// 서비스 컬렉션 확장 메서드
/// 데이터베이스 설정을 위한 확장 메서드들을 제공
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 데이터베이스 컨텍스트를 구성에 따라 동적으로 설정
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">구성 정보</param>
    /// <param name="logger">로거 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddConfigurableDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger? logger = null)
    {
        // 데이터베이스 옵션 바인딩
        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);

        // 옵션 등록
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // 설정 유효성 검증
        if (!databaseOptions.IsValid())
        {
            throw new InvalidOperationException($"데이터베이스 설정이 유효하지 않습니다. Type: {databaseOptions.Type}");
        }

        logger?.LogInformation("데이터베이스 타입: {DatabaseType}", databaseOptions.GetDatabaseTypeString());

        // 데이터베이스 타입에 따른 DbContext 설정
        return databaseOptions.Type switch
        {
            DatabaseType.InMemory => services.AddInMemoryDatabase(databaseOptions, logger),
            DatabaseType.PostgreSQL => services.AddPostgreSQLDatabase(databaseOptions, logger),
            DatabaseType.SqlServer => throw new NotImplementedException("SQL Server는 현재 지원하지 않습니다. 필요한 경우 Microsoft.EntityFrameworkCore.SqlServer 패키지를 추가하세요."),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseOptions.Type), databaseOptions.Type, "지원하지 않는 데이터베이스 타입입니다.")
        };
    }

    /// <summary>
    /// InMemory 데이터베이스 설정
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="options">데이터베이스 옵션</param>
    /// <param name="logger">로거</param>
    /// <returns>서비스 컬렉션</returns>
    private static IServiceCollection AddInMemoryDatabase(
        this IServiceCollection services,
        DatabaseOptions options,
        ILogger? logger)
    {
        var connectionString = options.GetConnectionString();
        logger?.LogInformation("InMemory 데이터베이스 설정: {DatabaseName}", connectionString);

        services.AddDbContext<ApplicationDbContext>(dbOptions =>
        {
            dbOptions.UseInMemoryDatabase(connectionString);
            dbOptions.EnableSensitiveDataLogging(true);
            dbOptions.EnableDetailedErrors(true);
        });

        return services;
    }

    /// <summary>
    /// PostgreSQL 데이터베이스 설정
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="options">데이터베이스 옵션</param>
    /// <param name="logger">로거</param>
    /// <returns>서비스 컬렉션</returns>
    private static IServiceCollection AddPostgreSQLDatabase(
        this IServiceCollection services,
        DatabaseOptions options,
        ILogger? logger)
    {
        var connectionString = options.GetConnectionString();
        logger?.LogInformation("PostgreSQL 데이터베이스 설정: {ConnectionString}", 
            connectionString.Replace("Password=", "Password=***"));

        services.AddDbContext<ApplicationDbContext>(dbOptions =>
        {
            dbOptions.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("ImageViewer.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // 개발 환경에서만 민감한 데이터 로깅 활성화
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                dbOptions.EnableSensitiveDataLogging(true);
                dbOptions.EnableDetailedErrors(true);
            }
        });

        return services;
    }

    /// <summary>
    /// SQL Server 데이터베이스 설정 (향후 확장용)
    /// 현재는 구현되지 않음 - Microsoft.EntityFrameworkCore.SqlServer 패키지가 필요함
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="options">데이터베이스 옵션</param>
    /// <param name="logger">로거</param>
    /// <returns>서비스 컬렉션</returns>
    private static IServiceCollection AddSqlServerDatabase(
        this IServiceCollection services,
        DatabaseOptions options,
        ILogger? logger)
    {
        // TODO: SQL Server 지원을 위해서는 다음 패키지를 추가해야 함:
        // dotnet add package Microsoft.EntityFrameworkCore.SqlServer
        
        throw new NotImplementedException("SQL Server 지원은 현재 구현되지 않았습니다.");
    }
}