using ImageViewer.Domain.Entities;

namespace ImageViewer.Tests.Unit.Domain;

/// <summary>
/// UserSettings 엔티티에 대한 단위 테스트
/// </summary>
public class UserSettingsTests
{
    private readonly Fixture _fixture;

    public UserSettingsTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void UserSettings_WhenCreatedWithValidUserId_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = "test-user-id-123";

        // Act
        var userSettings = new UserSettings(userId);

        // Assert
        userSettings.UserId.Should().Be(userId);
        userSettings.PreviewCount.Should().Be(12); // 기본값
        userSettings.BlurThumbnails.Should().BeTrue(); // 기본값
        userSettings.UseDarkMode.Should().BeFalse(); // 기본값
        userSettings.Id.Should().NotBeEmpty();
        userSettings.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UserSettings_WhenCreatedWithInvalidUserId_ShouldThrowArgumentNullException(string invalidUserId)
    {
        // Act & Assert
        var act = () => new UserSettings(invalidUserId);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdatePreviewCount_WhenCalledWithValidCount_ShouldUpdatePreviewCount()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        var newPreviewCount = 20;

        // Act
        userSettings.UpdatePreviewCount(newPreviewCount);

        // Assert
        userSettings.PreviewCount.Should().Be(newPreviewCount);
        userSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] // 최대값 초과
    public void UpdatePreviewCount_WhenCalledWithInvalidCount_ShouldThrowArgumentException(int invalidCount)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act & Assert
        var act = () => userSettings.UpdatePreviewCount(invalidCount);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateBlurThumbnails_WhenCalled_ShouldUpdateBlurThumbnails(bool blurEnabled)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateBlurThumbnails(blurEnabled);

        // Assert
        userSettings.BlurThumbnails.Should().Be(blurEnabled);
        userSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateDarkMode_WhenCalled_ShouldUpdateDarkMode(bool darkModeEnabled)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdateDarkMode(darkModeEnabled);

        // Assert
        userSettings.UseDarkMode.Should().Be(darkModeEnabled);
        userSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ResetToDefaults_WhenCalled_ShouldResetAllSettingsToDefaults()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();
        userSettings.UpdatePreviewCount(24);
        userSettings.UpdateBlurThumbnails(false);
        userSettings.UpdateDarkMode(true);

        // Act
        userSettings.ResetToDefaults();

        // Assert
        userSettings.PreviewCount.Should().Be(12);
        userSettings.BlurThumbnails.Should().BeTrue();
        userSettings.UseDarkMode.Should().BeFalse();
        userSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(48)]
    [InlineData(100)]
    public void UpdatePreviewCount_WhenCalledWithValidCounts_ShouldAcceptValues(int validCount)
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdatePreviewCount(validCount);

        // Assert
        userSettings.PreviewCount.Should().Be(validCount);
    }

    [Fact]
    public void GetDefaultSettings_WhenCalledWithUserId_ShouldReturnDefaultSettings()
    {
        // Arrange
        var userId = "test-user-default";

        // Act
        var userSettings = new UserSettings(userId);

        // Assert
        userSettings.UserId.Should().Be(userId);
        userSettings.PreviewCount.Should().Be(12);
        userSettings.BlurThumbnails.Should().BeTrue();
        userSettings.UseDarkMode.Should().BeFalse();
    }

    [Fact]
    public void UserSettings_WhenMultipleUpdatesPerformed_ShouldMaintainStateConsistency()
    {
        // Arrange
        var userSettings = CreateValidUserSettings();

        // Act
        userSettings.UpdatePreviewCount(24);
        userSettings.UpdateBlurThumbnails(false);
        userSettings.UpdateDarkMode(true);

        // Assert
        userSettings.PreviewCount.Should().Be(24);
        userSettings.BlurThumbnails.Should().BeFalse();
        userSettings.UseDarkMode.Should().BeTrue();
        userSettings.UserId.Should().Be("test-user-id-123");
    }

    private UserSettings CreateValidUserSettings()
    {
        return new UserSettings("test-user-id-123");
    }
}