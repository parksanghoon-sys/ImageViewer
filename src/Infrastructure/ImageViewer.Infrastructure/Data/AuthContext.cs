using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// 인증/인가 전용 데이터베이스 컨텍스트
/// ASP.NET Core Identity를 사용한 사용자 관리
/// </summary>
public class AuthContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AuthContext(DbContextOptions<AuthContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 테이블 이름 변경
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // ApplicationUser 설정
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<int>()
                .HasDefaultValue(UserRole.User);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.HasIndex(e => e.UserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_UserName");
        });

        // 초기 데이터 시드
        SeedData(modelBuilder);
    }

    /// <summary>
    /// 초기 데이터 시딩
    /// </summary>
    /// <param name="modelBuilder">모델 빌더</param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        var adminRoleId = Guid.NewGuid().ToString();
        var userRoleId = Guid.NewGuid().ToString();
        var guestRoleId = Guid.NewGuid().ToString();

        var adminUserId = Guid.NewGuid().ToString();
        var testUserId = Guid.NewGuid().ToString();
        var guestUserId = Guid.NewGuid().ToString();

        // 역할 시드 데이터
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole
            {
                Id = userRoleId,
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole
            {
                Id = guestRoleId,
                Name = "Guest",
                NormalizedName = "GUEST",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );

        // 비밀번호 해셔
        var passwordHasher = new PasswordHasher<ApplicationUser>();

        // 관리자 계정
        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            UserName = "admin@imageviewer.com",
            NormalizedUserName = "ADMIN@IMAGEVIEWER.COM",
            Email = "admin@imageviewer.com",
            NormalizedEmail = "ADMIN@IMAGEVIEWER.COM",
            EmailConfirmed = true,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");

        // 테스트 사용자 계정
        var testUser = new ApplicationUser
        {
            Id = testUserId,
            UserName = "testuser@imageviewer.com",
            NormalizedUserName = "TESTUSER@IMAGEVIEWER.COM",
            Email = "testuser@imageviewer.com",
            NormalizedEmail = "TESTUSER@IMAGEVIEWER.COM",
            EmailConfirmed = true,
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        testUser.PasswordHash = passwordHasher.HashPassword(testUser, "User123!");

        // 게스트 계정
        var guestUser = new ApplicationUser
        {
            Id = guestUserId,
            UserName = "guest@imageviewer.com",
            NormalizedUserName = "GUEST@IMAGEVIEWER.COM",
            Email = "guest@imageviewer.com",
            NormalizedEmail = "GUEST@IMAGEVIEWER.COM",
            EmailConfirmed = true,
            Role = UserRole.Guest,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        guestUser.PasswordHash = passwordHasher.HashPassword(guestUser, "Guest123!");

        // 사용자 시드 데이터
        modelBuilder.Entity<ApplicationUser>().HasData(adminUser, testUser, guestUser);

        // 사용자-역할 매핑 시드 데이터
        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string>
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            },
            new IdentityUserRole<string>
            {
                UserId = testUserId,
                RoleId = userRoleId
            },
            new IdentityUserRole<string>
            {
                UserId = guestUserId,
                RoleId = guestRoleId
            }
        );
    }
}