namespace ImageViewer.Domain.Enums;

/// <summary>
/// 데이터베이스 타입을 정의하는 열거형
/// 애플리케이션에서 사용 가능한 데이터베이스 타입들을 나타냄
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// 인메모리 데이터베이스 (테스트 및 개발용)
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// PostgreSQL 데이터베이스
    /// </summary>
    PostgreSQL = 1,

    /// <summary>
    /// SQL Server 데이터베이스 (향후 확장용)
    /// </summary>
    SqlServer = 2
}