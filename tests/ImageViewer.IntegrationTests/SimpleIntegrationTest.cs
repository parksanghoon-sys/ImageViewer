using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests;

/// <summary>
/// 간단한 통합 테스트 - Mock RabbitMQ만 테스트
/// </summary>
public class SimpleIntegrationTest
{
    [Fact]
    public void MockRabbitMQService_ShouldWork_WithNewInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<SimpleIntegrationTest>>();

        logger.LogInformation("=== Mock RabbitMQ 기본 테스트 시작 ===");

        var mockRabbitMQ = new MockRabbitMQService();

        // Act & Assert - InitializeAsync 메서드 테스트
        var initTask = mockRabbitMQ.InitializeAsync();
        initTask.Should().NotBeNull();
        initTask.IsCompleted.Should().BeTrue();

        logger.LogInformation("✅ InitializeAsync 메서드 정상 동작");

        // Act & Assert - PublishEventAsync 메서드 테스트
        var testMessage = new { Id = Guid.NewGuid(), Message = "Test Event" };
        var publishTask = mockRabbitMQ.PublishEventAsync(testMessage, "test.event");
        
        publishTask.Should().NotBeNull();
        publishTask.IsCompleted.Should().BeTrue();
        publishTask.Result.Should().BeTrue();

        // 발행된 메시지 확인
        var messages = mockRabbitMQ.GetPublishedMessages();
        messages.Should().HaveCount(1);
        messages[0].routingKey.Should().Be("test.event");

        logger.LogInformation("✅ PublishEventAsync 메서드 정상 동작");

        // Act & Assert - Subscribe 메서드 테스트
        var receivedMessages = new List<object>();
        
        mockRabbitMQ.Subscribe<object>(async (message) =>
        {
            receivedMessages.Add(message);
            await Task.CompletedTask;
        }, "test.queue");

        logger.LogInformation("✅ Subscribe 메서드 정상 동작");

        // 정리
        mockRabbitMQ.Dispose();

        logger.LogInformation("=== Mock RabbitMQ 기본 테스트 완료 ===");
    }

    [Fact]
    public async Task MockRabbitMQService_ShouldHandle_PublishAndSubscribe()
    {
        // Arrange
        var mockRabbitMQ = new MockRabbitMQService();
        await mockRabbitMQ.InitializeAsync();

        var receivedMessages = new List<string>();
        var testMessage = "Hello Integration Test!";

        // Subscribe 설정
        mockRabbitMQ.Subscribe<string>(async (message) =>
        {
            receivedMessages.Add(message);
            await Task.CompletedTask;
        }, "test.messages");

        // Act
        await mockRabbitMQ.PublishEventAsync(testMessage, "test.messages");
        
        // 비동기 처리 대기
        await Task.Delay(100);

        // Assert
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Should().Be(testMessage);

        // 정리
        mockRabbitMQ.Dispose();
    }

    [Fact]
    public void MockRabbitMQService_ShouldClear_TrackedMessages()
    {
        // Arrange
        var mockRabbitMQ = new MockRabbitMQService();
        var testMessage = new { Data = "Clear Test" };

        // Act
        _ = mockRabbitMQ.PublishEventAsync(testMessage, "clear.test");
        mockRabbitMQ.GetPublishedMessages().Should().HaveCount(1);

        mockRabbitMQ.ClearMessages();

        // Assert
        mockRabbitMQ.GetPublishedMessages().Should().BeEmpty();

        // 정리
        mockRabbitMQ.Dispose();
    }
}