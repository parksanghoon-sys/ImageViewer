using ImageViewer.Domain.Entities;

namespace ImageViewer.Tests.Unit.Domain;

/// <summary>
/// Image 엔티티에 대한 단위 테스트
/// </summary>
public class ImageTests
{
    private readonly Fixture _fixture;

    public ImageTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Image_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;
        var description = "Test image";

        // Act
        var image = new Image(userId, originalFileName, storedFileName, filePath, 
                             fileSize, mimeType, width, height, description);

        // Assert
        image.UserId.Should().Be(userId);
        image.OriginalFileName.Should().Be(originalFileName);
        image.StoredFileName.Should().Be(storedFileName);
        image.FilePath.Should().Be(filePath);
        image.FileSize.Should().Be(fileSize);
        image.MimeType.Should().Be(mimeType);
        image.Width.Should().Be(width);
        image.Height.Should().Be(height);
        image.Description.Should().Be(description);        
        image.ThumbnailPath.Should().BeNull();
        image.Id.Should().NotBeEmpty();
        image.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Image_WhenCreatedWithoutDescription_ShouldHaveNullDescription()
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;

        // Act
        var image = new Image(userId, originalFileName, storedFileName, filePath, 
                             fileSize, mimeType, width, height);

        // Assert
        image.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Image_WhenCreatedWithInvalidOriginalFileName_ShouldThrowArgumentException(string invalidFileName)
    {
        // Arrange
        var userId = "test-user-id-123";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, invalidFileName, storedFileName, filePath, 
                                 fileSize, mimeType, width, height);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Image_WhenCreatedWithInvalidStoredFileName_ShouldThrowArgumentException(string invalidStoredFileName)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, invalidStoredFileName, filePath, 
                                 fileSize, mimeType, width, height);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Image_WhenCreatedWithInvalidFilePath_ShouldThrowArgumentException(string invalidFilePath)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, storedFileName, invalidFilePath, 
                                 fileSize, mimeType, width, height);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Image_WhenCreatedWithInvalidMimeType_ShouldThrowArgumentException(string invalidMimeType)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var width = 1920;
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, storedFileName, filePath, 
                                 fileSize, invalidMimeType, width, height);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Image_WhenCreatedWithInvalidFileSize_ShouldThrowArgumentException(long invalidFileSize)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var mimeType = "image/jpeg";
        var width = 1920;
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, storedFileName, filePath, 
                                 invalidFileSize, mimeType, width, height);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Image_WhenCreatedWithInvalidWidth_ShouldThrowArgumentException(int invalidWidth)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var height = 1080;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, storedFileName, filePath, 
                                 fileSize, mimeType, invalidWidth, height);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Image_WhenCreatedWithInvalidHeight_ShouldThrowArgumentException(int invalidHeight)
    {
        // Arrange
        var userId = "test-user-id-123";
        var originalFileName = "test.jpg";
        var storedFileName = "stored_test.jpg";
        var filePath = "/uploads/test.jpg";
        var fileSize = 1024000L;
        var mimeType = "image/jpeg";
        var width = 1920;

        // Act & Assert
        var act = () => new Image(userId, originalFileName, storedFileName, filePath, 
                                 fileSize, mimeType, width, invalidHeight);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetThumbnailPath_WhenCalledWithValidPath_ShouldSetThumbnailPathAndMarkAsGenerated()
    {
        // Arrange
        var image = CreateValidImage();
        var thumbnailPath = "/thumbnails/thumb_test.jpg";

        // Act
        image.SetThumbnailPath(thumbnailPath);

        // Assert
        image.ThumbnailPath.Should().Be(thumbnailPath);
        
        image.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetThumbnailPath_WhenCalledWithInvalidPath_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var image = CreateValidImage();

        // Act & Assert
        var act = () => image.SetThumbnailPath(invalidPath);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateDescription_WhenCalledWithValidDescription_ShouldUpdateDescription()
    {
        // Arrange
        var image = CreateValidImage();
        var newDescription = "Updated description";

        // Act
        image.UpdateDescription(newDescription);

        // Assert
        image.Description.Should().Be(newDescription);
        image.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateDescription_WhenCalledWithNull_ShouldSetDescriptionToNull()
    {
        // Arrange
        var image = CreateValidImage();

        // Act
        image.UpdateDescription(null);

        // Assert
        image.Description.Should().BeNull();
        image.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateDimensions_WhenCalledWithValidDimensions_ShouldUpdateWidthAndHeight()
    {
        // Arrange
        var image = CreateValidImage();
        var newWidth = 2560;
        var newHeight = 1440;

        // Act
        image.UpdateDimensions(newWidth, newHeight);

        // Assert
        image.Width.Should().Be(newWidth);
        image.Height.Should().Be(newHeight);
        image.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(-1, 1080)]
    [InlineData(1920, 0)]
    [InlineData(1920, -1)]
    public void UpdateDimensions_WhenCalledWithInvalidDimensions_ShouldThrowArgumentException(int width, int height)
    {
        // Arrange
        var image = CreateValidImage();

        // Act & Assert
        var act = () => image.UpdateDimensions(width, height);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Image_WhenCreated_ShouldHaveEmptyShareRequestsCollection()
    {
        // Arrange & Act
        var image = CreateValidImage();

        // Assert
        image.ShareRequests.Should().NotBeNull().And.BeEmpty();
    }

    private Image CreateValidImage()
    {
        return new Image(
            "test-user-id-123",
            "test.jpg",
            "stored_test.jpg",
            "/uploads/test.jpg",
            1024000L,
            "image/jpeg",
            1920,
            1080,
            "Test image"
        );
    }
}