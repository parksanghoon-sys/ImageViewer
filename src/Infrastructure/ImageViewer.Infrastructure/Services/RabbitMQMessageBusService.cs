using ImageViewer.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// RabbitMQ 기반 메시지 버스 서비스 구현
/// 이벤트 발행/구독을 통한 마이크로서비스 간 비동기 통신
/// </summary>
public class RabbitMQMessageBusService : IMessageBusService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMQMessageBusService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _exchangeName;
    private readonly Dictionary<string, EventingBasicConsumer> _consumers = new();
    private bool _disposed = false;

    /// <summary>
    /// RabbitMQMessageBusService 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="configuration">설정</param>
    public RabbitMQMessageBusService(
        ILogger<RabbitMQMessageBusService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "imageviewer.events";

        try
        {
            // RabbitMQ 연결 설정
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Exchange 선언 (Topic 타입으로 라우팅 지원)
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation("RabbitMQ 연결 성공: {HostName}:{Port}", factory.HostName, factory.Port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ 연결 실패 - 메시지 버스 기능이 비활성화됩니다");
            _connection = null;
            _channel = null;
        }
    }

    /// <inheritdoc />
    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogInformation("RabbitMQ 메시지 버스 서비스 시작됨");
            await Task.CompletedTask;
        }
        else
        {
            _logger.LogWarning("RabbitMQ 연결이 없어 메시지 버스 서비스를 시작할 수 없습니다");
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogInformation("RabbitMQ 메시지 버스 서비스 중지됨");
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T eventData, string? routingKey = null, CancellationToken cancellationToken = default) 
        where T : class
    {
        if (!IsConnected)
        {
            _logger.LogWarning("RabbitMQ 연결이 없어 이벤트를 발행할 수 없습니다: {EventType}", typeof(T).Name);
            return;
        }

        await Task.Run(() => Publish(eventData, routingKey), cancellationToken);
    }

    /// <inheritdoc />
    public void Publish<T>(T eventData, string? routingKey = null) where T : class
    {
        if (!IsConnected || _channel == null)
        {
            _logger.LogWarning("RabbitMQ 연결이 없어 이벤트를 발행할 수 없습니다: {EventType}", typeof(T).Name);
            return;
        }

        try
        {
            var effectiveRoutingKey = routingKey ?? GetRoutingKey<T>();
            var message = JsonSerializer.Serialize(eventData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 메시지 영속화
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = typeof(T).Name;

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: effectiveRoutingKey,
                basicProperties: properties,
                body: body
            );

            _logger.LogDebug("이벤트 발행 성공: {EventType}, RoutingKey: {RoutingKey}", typeof(T).Name, effectiveRoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이벤트 발행 실패: {EventType}", typeof(T).Name);
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!IsConnected || _channel == null)
        {
            _logger.LogWarning("RabbitMQ 연결이 없어 이벤트를 구독할 수 없습니다: {EventType}", typeof(T).Name);
            return;
        }

        try
        {
            // 큐 선언
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // 큐를 Exchange에 바인딩
            var routingKey = GetRoutingKey<T>();
            _channel.QueueBind(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: routingKey
            );

            // 소비자 설정
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, eventArgs) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                    var eventData = JsonSerializer.Deserialize<T>(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (eventData != null)
                    {
                        await handler(eventData);
                        _channel.BasicAck(eventArgs.DeliveryTag, false);
                        _logger.LogDebug("이벤트 처리 성공: {EventType}, Queue: {QueueName}", typeof(T).Name, queueName);
                    }
                    else
                    {
                        _logger.LogWarning("이벤트 역직렬화 실패: {EventType}, Queue: {QueueName}", typeof(T).Name, queueName);
                        _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "이벤트 처리 중 오류 발생: {EventType}, Queue: {QueueName}", typeof(T).Name, queueName);
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true); // 재시도를 위해 requeue=true
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false, // 수동 ACK로 안정성 확보
                consumer: consumer
            );

            _consumers[queueName] = consumer;
            _logger.LogInformation("이벤트 구독 시작: {EventType}, Queue: {QueueName}, RoutingKey: {RoutingKey}", 
                typeof(T).Name, queueName, routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이벤트 구독 설정 실패: {EventType}, Queue: {QueueName}", typeof(T).Name, queueName);
        }
    }

    /// <summary>
    /// 이벤트 타입에 따른 라우팅 키 생성
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <returns>라우팅 키</returns>
    private static string GetRoutingKey<T>()
    {
        var eventTypeName = typeof(T).Name;
        
        // 네이밍 변환: ImageUploadedEvent -> image.uploaded
        var routingKey = eventTypeName
            .Replace("Event", "")
            .ToLowerInvariant();

        // CamelCase를 dot notation으로 변환
        var result = new StringBuilder();
        for (int i = 0; i < routingKey.Length; i++)
        {
            if (i > 0 && char.IsUpper(routingKey[i]))
            {
                result.Append('.');
            }
            result.Append(char.ToLower(routingKey[i]));
        }

        return result.ToString();
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
                
                _logger.LogInformation("RabbitMQ 연결이 정리되었습니다");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ 연결 정리 중 오류 발생");
            }
            
            _disposed = true;
        }
    }
}