using ImageViewer.Contracts.Events;

namespace ImageViewer.Infrastructure.MessageBus;

/// <summary>
/// RabbitMQ 메시지 버스 서비스 인터페이스
/// 이벤트 기반 아키텍처를 위한 비동기 메시징 처리
/// </summary>
public interface IRabbitMQService
{
    /// <summary>
    /// 이벤트를 RabbitMQ 큐에 발행합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="eventData">발행할 이벤트 데이터</param>
    /// <param name="routingKey">라우팅 키 (기본값: 이벤트 타입명)</param>
    /// <returns>발행 성공 여부</returns>
    Task<bool> PublishEventAsync<T>(T eventData, string? routingKey = null) where T : class;

    /// <summary>
    /// 특정 타입의 이벤트를 구독하고 핸들러를 등록합니다.
    /// </summary>
    /// <typeparam name="T">구독할 이벤트 타입</typeparam>
    /// <param name="handler">이벤트 처리 핸들러</param>
    /// <param name="queueName">큐 이름 (기본값: 이벤트 타입명)</param>
    void Subscribe<T>(Func<T, Task> handler, string? queueName = null) where T : class;

    /// <summary>
    /// 연결을 초기화하고 Exchange와 Queue를 설정합니다.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// RabbitMQ 연결을 해제합니다.
    /// </summary>
    void Dispose();
}