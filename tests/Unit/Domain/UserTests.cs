using ImageViewer.Domain.Entities;

namespace ImageViewer.Tests.Unit.Domain;

/// <summary>
/// User 엔티티에 대한 단위 테스트
/// </summary>
public class UserTests
{
    private readonly Fixture _fixture;

    public UserTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void User_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act
        var user = new User(email, username, passwordHash, passwordSalt);

        // Assert
        user.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.PasswordHash.Should().Be(passwordHash);
        user.PasswordSalt.Should().Be(passwordSalt);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_WhenCreatedWithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
        var username = "testuser";
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act & Assert
        var act = () => new User(invalidEmail, username, passwordHash, passwordSalt);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_WhenCreatedWithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act & Assert
        var act = () => new User(email, invalidUsername, passwordHash, passwordSalt);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateLastLogin_WhenCalled_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = CreateValidUser();
        var loginTime = DateTime.UtcNow;

        // Act
        user.UpdateLastLogin();

        // Assert
        user.LastLoginAt.Should().BeCloseTo(loginTime, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(loginTime, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_WhenCalled_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = CreateValidUser();
        user.IsActive.Should().BeTrue();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_WhenCalled_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = CreateValidUser();
        user.Deactivate(); // 먼저 비활성화

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdatePassword_WhenCalledWithValidData_ShouldUpdatePasswordHashAndSalt()
    {
        // Arrange
        var user = CreateValidUser();
        var newPasswordHash = "newhashedpassword";
        var newPasswordSalt = "newsalt";

        // Act
        user.UpdatePassword(newPasswordHash, newPasswordSalt);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
        user.PasswordSalt.Should().Be(newPasswordSalt);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null, "salt")]
    [InlineData("", "salt")]
    [InlineData("hash", null)]
    [InlineData("hash", "")]
    public void UpdatePassword_WhenCalledWithInvalidData_ShouldThrowArgumentException(
        string passwordHash, string passwordSalt)
    {
        // Arrange
        var user = CreateValidUser();

        // Act & Assert
        var act = () => user.UpdatePassword(passwordHash, passwordSalt);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_WhenCreated_ShouldHaveEmptyCollections()
    {
        // Arrange & Act
        var user = CreateValidUser();

        // Assert
        user.Images.Should().NotBeNull().And.BeEmpty();
        user.RequestedShares.Should().NotBeNull().And.BeEmpty();
        user.ReceivedShares.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_WhenEmailTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longEmail = new string('a', 310) + "@example.com"; // 총 322자
        var username = "testuser";
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act & Assert
        var act = () => new User(longEmail, username, passwordHash, passwordSalt);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*320*"); // 최대 길이 메시지 포함
    }

    [Fact]
    public void User_WhenUsernameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var email = "test@example.com";
        var longUsername = new string('a', 51); // 51자
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act & Assert
        var act = () => new User(email, longUsername, passwordHash, passwordSalt);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*50*"); // 최대 길이 메시지 포함
    }

    private User CreateValidUser()
    {
        return new User(
            "test@example.com",
            "testuser",
            "hashedpassword",
            "salt"
        );
    }
}