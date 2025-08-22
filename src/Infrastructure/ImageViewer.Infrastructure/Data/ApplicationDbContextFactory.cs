using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImageViewer.Infrastructure.Data
{
    /// <summary>
    /// EF Core 디자인타임에서 DbContext 인스턴스를 생성하기 위한 팩토리 클래스
    /// 마이그레이션 생성 및 업데이트 시 사용됨
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <summary>
        /// 디자인타임에서 ApplicationDbContext 인스턴스를 생성합니다.
        /// PostgreSQL 연결 문자열을 사용하여 DbContext를 구성합니다.
        /// </summary>
        /// <param name="args">명령줄 인수 (사용되지 않음)</param>
        /// <returns>구성된 ApplicationDbContext 인스턴스</returns>
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // 개발환경용 PostgreSQL 연결 문자열
            // 실제 운영환경에서는 환경변수나 설정파일에서 가져와야 함
            var connectionString = "Host=localhost;Port=5433;Database=ImageViewerDB;Username=postgres;Password=your_password_here";
            
            optionsBuilder.UseNpgsql(connectionString, options =>
            {
                options.EnableRetryOnFailure(
                    maxRetryCount: 3,           // 최대 재시도 횟수
                    maxRetryDelay: TimeSpan.FromSeconds(5),  // 최대 재시도 지연시간
                    errorCodesToAdd: null       // 추가 재시도 대상 에러코드
                );
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}