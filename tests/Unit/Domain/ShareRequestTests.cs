using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Tests.Unit.Domain;

/// <summary>
/// ShareRequest 엔티티에 대한 단위 테스트
/// </summary>
public class ShareRequestTests
{
    private readonly Fixture _fixture;

    public ShareRequestTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void ShareRequest_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var requesterId = "requester-user-id";
        var ownerId = "owner-user-id";
        var imageId = Guid.NewGuid();
        var requestMessage = "공유 요청합니다.";

        // Act
        var shareRequest = new ShareRequest(requesterId, ownerId, imageId, requestMessage);

        // Assert
        shareRequest.RequesterId.Should().Be(requesterId);
        shareRequest.OwnerId.Should().Be(ownerId);
        shareRequest.ImageId.Should().Be(imageId);
        shareRequest.RequestMessage.Should().Be(requestMessage);
        shareRequest.Status.Should().Be(ShareRequestStatus.Pending);
        shareRequest.Id.Should().NotBeEmpty();
        shareRequest.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        shareRequest.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ShareRequest_WhenCreatedWithoutMessage_ShouldHaveNullMessage()
    {
        // Arrange
        var requesterId = "requester-user-id";
        var ownerId = "owner-user-id";
        var imageId = Guid.NewGuid();

        // Act
        var shareRequest = new ShareRequest(requesterId, ownerId, imageId);

        // Assert
        shareRequest.RequestMessage.Should().BeNull();
        shareRequest.Status.Should().Be(ShareRequestStatus.Pending);
    }

    [Fact]
    public void ShareRequest_WhenCreatedWithCustomExpirationDays_ShouldSetCorrectExpirationDate()
    {
        // Arrange
        var requesterId = "requester-user-id";
        var ownerId = "owner-user-id";
        var imageId = Guid.NewGuid();
        var expirationDays = 14;

        // Act
        var shareRequest = new ShareRequest(requesterId, ownerId, imageId, null, expirationDays);

        // Assert
        shareRequest.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(expirationDays), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ShareRequest_WhenRequesterAndOwnerAreSame_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "same-user-id";
        var imageId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ShareRequest(userId, userId, imageId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("요청자와 소유자가 같을 수 없습니다.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShareRequest_WhenRequesterIdIsNullOrEmpty_ShouldThrowArgumentNullException(string invalidRequesterId)
    {
        // Arrange
        var ownerId = "owner-user-id";
        var imageId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ShareRequest(invalidRequesterId, ownerId, imageId);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShareRequest_WhenOwnerIdIsNullOrEmpty_ShouldThrowArgumentNullException(string invalidOwnerId)
    {
        // Arrange
        var requesterId = "requester-user-id";
        var imageId = Guid.NewGuid();

        // Act & Assert
        var act = () => new ShareRequest(requesterId, invalidOwnerId, imageId);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Approve_WhenCalled_ShouldChangeStatusToApproved()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();

        // Act
        shareRequest.Approve();

        // Assert
        shareRequest.Status.Should().Be(ShareRequestStatus.Approved);
        shareRequest.ResponseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        shareRequest.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reject_WhenCalledWithReason_ShouldChangeStatusToRejectedWithReason()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();
        var rejectionReason = "개인정보 포함";

        // Act
        shareRequest.Reject(rejectionReason);

        // Assert
        shareRequest.Status.Should().Be(ShareRequestStatus.Rejected);
        shareRequest.ResponseMessage.Should().Be(rejectionReason);
        shareRequest.ResponseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        shareRequest.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reject_WhenCalledWithoutReason_ShouldChangeStatusToRejectedWithoutReason()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();

        // Act
        shareRequest.Reject();

        // Assert
        shareRequest.Status.Should().Be(ShareRequestStatus.Rejected);
        shareRequest.ResponseMessage.Should().BeNull();
        shareRequest.ResponseAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();

        // Act
        var isExpired = shareRequest.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var shareRequest = new ShareRequest(
            "requester-id", 
            "owner-id", 
            Guid.NewGuid(), 
            null, 
            -1); // 만료일을 어제로 설정

        // Act
        var isExpired = shareRequest.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void CanBeProcessed_WhenPendingAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();

        // Act
        var canBeProcessed = shareRequest.CanBeProcessed();

        // Assert
        canBeProcessed.Should().BeTrue();
    }

    [Fact]
    public void CanBeProcessed_WhenApproved_ShouldReturnFalse()
    {
        // Arrange
        var shareRequest = CreateValidShareRequest();
        shareRequest.Approve();

        // Act
        var canBeProcessed = shareRequest.CanBeProcessed();

        // Assert
        canBeProcessed.Should().BeFalse();
    }

    [Fact]
    public void CanBeProcessed_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var shareRequest = new ShareRequest(
            "requester-id", 
            "owner-id", 
            Guid.NewGuid(), 
            null, 
            -1); // 만료일을 어제로 설정

        // Act
        var canBeProcessed = shareRequest.CanBeProcessed();

        // Assert
        canBeProcessed.Should().BeFalse();
    }

    private ShareRequest CreateValidShareRequest()
    {
        return new ShareRequest(
            "requester-user-id",
            "owner-user-id",
            Guid.NewGuid(),
            "공유 요청합니다."
        );
    }
}