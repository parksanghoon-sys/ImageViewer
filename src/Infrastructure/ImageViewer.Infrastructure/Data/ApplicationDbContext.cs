using ImageViewer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// 비즈니스 로직 전용 데이터베이스 컨텍스트
/// 이미지, 공유 요청, 사용자 설정 등 비즈니스 엔티티를 관리
/// 인증 관련 엔티티는 AuthContext에서 별도 관리
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// 이미지 테이블
    /// </summary>
    public DbSet<Image> Images { get; set; } = null!;

    /// <summary>
    /// 공유 요청 테이블
    /// </summary>
    public DbSet<ShareRequest> ShareRequests { get; set; } = null!;

    /// <summary>
    /// 사용자 설정 테이블
    /// </summary>
    public DbSet<UserSettings> UserSettings { get; set; } = null!;

    /// <summary>
    /// 기본 생성자
    /// </summary>
    /// <param name="options">DbContext 옵션</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 모델 생성 시 엔티티 설정 및 관계 정의
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // In-Memory 데이터베이스에서는 기본 설정만 사용
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // 기본 키 설정만 적용
            ConfigureBasicEntities(modelBuilder);
            return;
        }

        // 현재 어셈블리의 모든 IEntityTypeConfiguration을 자동으로 적용
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // 글로벌 설정
        ConfigureGlobalProperties(modelBuilder);

        // 엔티티별 설정
        ConfigureImage(modelBuilder);
        ConfigureShareRequest(modelBuilder);
        ConfigureUserSettings(modelBuilder);
    }

    /// <summary>
    /// In-Memory 데이터베이스용 기본 엔티티 설정
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private static void ConfigureBasicEntities(ModelBuilder modelBuilder)
    {
        // Image 기본 설정
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.OriginalFileName).IsRequired();
            entity.Property(e => e.StoredFileName).IsRequired();
            entity.Property(e => e.FilePath).IsRequired();
            entity.Property(e => e.MimeType).IsRequired();
            entity.Property(e => e.Title).IsRequired();
            
        });

        // ShareRequest 기본 설정
        modelBuilder.Entity<ShareRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequesterId).IsRequired();
            entity.Property(e => e.OwnerId).IsRequired();
            entity.Property(e => e.ImageId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            
        });

        // UserSettings 기본 설정
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            
        });
    }

    /// <summary>
    /// 모든 엔티티에 공통적으로 적용되는 글로벌 속성 설정
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private void ConfigureGlobalProperties(ModelBuilder modelBuilder)
    {
        // In-Memory 데이터베이스에서는 복잡한 설정을 건너뛰기
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        // 모든 decimal 속성의 기본 정밀도 설정
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // 모든 string 속성의 기본 최대 길이 설정 (명시적으로 설정되지 않은 경우)
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(string)))
        {
            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(500);
            }
        }
    }

    /// <summary>
    /// Image 엔티티 설정
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private static void ConfigureImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>(entity =>
        {
            // 테이블명 설정
            entity.ToTable("Images");

            // 기본키 설정
            entity.HasKey(e => e.Id);

            // UserId는 AuthContext의 ApplicationUser.Id를 참조 (string)
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450) // Identity의 기본 사용자 ID 길이
                .HasColumnType("varchar(450)");

            entity.Property(e => e.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            entity.Property(e => e.StoredFileName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            entity.Property(e => e.ThumbnailPath)
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            entity.Property(e => e.FileSize)
                .IsRequired();

            entity.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            entity.Property(e => e.Width)
                .IsRequired();

            entity.Property(e => e.Height)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            entity.Property(e => e.Tags)
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            entity.Property(e => e.IsPublic)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.UploadedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.ThumbnailReady)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // 인덱스 설정
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Images_UserId");

            entity.HasIndex(e => e.StoredFileName)
                .IsUnique()
                .HasDatabaseName("IX_Images_StoredFileName");

            entity.HasIndex(e => e.MimeType)
                .HasDatabaseName("IX_Images_MimeType");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Images_CreatedAt");

            entity.HasIndex(e => e.ThumbnailReady)
                .HasDatabaseName("IX_Images_IsThumbnailGenerated");

            // User 네비게이션 속성 무시 (AuthContext에서 관리)

            // ShareRequests 관계 설정
            entity.HasMany(e => e.ShareRequests)
                .WithOne(e => e.Image)
                .HasForeignKey(e => e.ImageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// ShareRequest 엔티티 설정
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private static void ConfigureShareRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShareRequest>(entity =>
        {
            // 테이블명 설정
            entity.ToTable("ShareRequests");

            // 기본키 설정
            entity.HasKey(e => e.Id);

            // 속성 설정 - AuthContext의 ApplicationUser.Id를 참조 (string)
            entity.Property(e => e.RequesterId)
                .IsRequired()
                .HasMaxLength(450)
                .HasColumnType("varchar(450)");

            entity.Property(e => e.OwnerId)
                .IsRequired()
                .HasMaxLength(450)
                .HasColumnType("varchar(450)");

            entity.Property(e => e.ImageId)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>(); // Enum을 int로 저장

            entity.Property(e => e.RequestMessage)
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            entity.Property(e => e.ResponseMessage)
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.RespondedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.ExpiresAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            // 인덱스 설정
            entity.HasIndex(e => e.RequesterId)
                .HasDatabaseName("IX_ShareRequests_RequesterId");

            entity.HasIndex(e => e.OwnerId)
                .HasDatabaseName("IX_ShareRequests_OwnerId");

            entity.HasIndex(e => e.ImageId)
                .HasDatabaseName("IX_ShareRequests_ImageId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ShareRequests_Status");

            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_ShareRequests_ExpiresAt");

            // 복합 인덱스: 중복 요청 방지
            entity.HasIndex(e => new { e.RequesterId, e.ImageId, e.Status })
                .HasDatabaseName("IX_ShareRequests_Requester_Image_Status");


            // Image 관계 설정
            entity.HasOne(e => e.Image)
                .WithMany(e => e.ShareRequests)
                .HasForeignKey(e => e.ImageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// UserSettings 엔티티 설정
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private static void ConfigureUserSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSettings>(entity =>
        {
            // 테이블명 설정
            entity.ToTable("UserSettings");

            // 기본키 설정
            entity.HasKey(e => e.Id);

            // UserId는 AuthContext의 ApplicationUser.Id를 참조 (string)
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450)
                .HasColumnType("varchar(450)");

            entity.Property(e => e.PreviewCount)
                .IsRequired()
                .HasDefaultValue(12);

            entity.Property(e => e.PreviewSize)
                .IsRequired()
                .HasDefaultValue(200);

            entity.Property(e => e.BlurIntensity)
                .IsRequired()
                .HasDefaultValue(50);

            entity.Property(e => e.AutoGenerateThumbnails)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.ReceiveShareNotifications)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.ReceiveEmailNotifications)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.UseDarkMode)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // 인덱스 설정
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserSettings_UserId");

            // User 네비게이션 속성 무시 (AuthContext에서 관리)
        });
    }
}