using ImageViewer.Domain.Enums;

namespace ImageViewer.Infrastructure.Configuration;

/// <summary>
/// 데이터베이스 설정 옵션 클래스
/// appsettings.json의 Database 섹션과 바인딩
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// 설정 섹션 이름
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// 데이터베이스 타입
    /// </summary>
    public DatabaseType Type { get; set; } = DatabaseType.InMemory;

    /// <summary>
    /// 연결 문자열 딕셔너리
    /// </summary>
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();

    /// <summary>
    /// 현재 데이터베이스 타입에 맞는 연결 문자열 반환
    /// </summary>
    /// <returns>연결 문자열</returns>
    public string GetConnectionString()
    {
        return Type switch
        {
            DatabaseType.InMemory => ConnectionStrings.GetValueOrDefault("InMemory", "DefaultInMemoryDb"),
            DatabaseType.PostgreSQL => ConnectionStrings.GetValueOrDefault("PostgreSQL", ""),
            DatabaseType.SqlServer => ConnectionStrings.GetValueOrDefault("SqlServer", ""),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "지원하지 않는 데이터베이스 타입입니다.")
        };
    }

    /// <summary>
    /// 설정 유효성 검증
    /// </summary>
    /// <returns>유효성 검증 결과</returns>
    public bool IsValid()
    {
        if (Type == DatabaseType.PostgreSQL || Type == DatabaseType.SqlServer)
        {
            var connectionString = GetConnectionString();
            return !string.IsNullOrEmpty(connectionString);
        }

        return true; // InMemory는 항상 유효
    }

    /// <summary>
    /// 데이터베이스 타입 문자열 표현
    /// </summary>
    /// <returns>데이터베이스 타입 문자열</returns>
    public string GetDatabaseTypeString()
    {
        return Type switch
        {
            DatabaseType.InMemory => "InMemory",
            DatabaseType.PostgreSQL => "PostgreSQL",
            DatabaseType.SqlServer => "SqlServer",
            _ => "Unknown"
        };
    }
}