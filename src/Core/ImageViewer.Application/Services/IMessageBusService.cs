namespace ImageViewer.Application.Services;

/// <summary>
/// 메시지 버스 서비스 인터페이스
/// RabbitMQ를 통한 이벤트 발행 및 구독을 추상화
/// </summary>
public interface IMessageBusService
{
    /// <summary>
    /// 이벤트를 발행합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="eventData">이벤트 데이터</param>
    /// <param name="routingKey">라우팅 키 (기본값: 이벤트 타입명)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task PublishAsync<T>(T eventData, string? routingKey = null, CancellationToken cancellationToken = default) 
        where T : class;

    /// <summary>
    /// 이벤트를 동기적으로 발행합니다. (주의: 성능에 영향을 줄 수 있음)
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="eventData">이벤트 데이터</param>
    /// <param name="routingKey">라우팅 키 (기본값: 이벤트 타입명)</param>
    void Publish<T>(T eventData, string? routingKey = null) where T : class;

    /// <summary>
    /// 지정된 큐에서 메시지를 구독합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="queueName">큐 이름</param>
    /// <param name="handler">이벤트 핸들러</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// 연결 상태를 확인합니다.
    /// </summary>
    /// <returns>연결 여부</returns>
    bool IsConnected { get; }

    /// <summary>
    /// 연결을 시작합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 연결을 종료합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}