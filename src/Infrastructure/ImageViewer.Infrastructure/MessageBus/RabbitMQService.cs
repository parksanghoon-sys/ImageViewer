using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ImageViewer.Infrastructure.MessageBus;

/// <summary>
/// RabbitMQ 메시지 버스 서비스 구현
/// 이벤트 기반 아키텍처를 위한 비동기 메시징 처리
/// </summary>
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly string _connectionString;
    private readonly string _exchangeName;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    /// <summary>
    /// RabbitMQ 서비스 생성자
    /// </summary>
    /// <param name="configuration">설정 객체</param>
    /// <param name="logger">로거</param>
    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("RabbitMQ") 
            ?? "amqp://guest:guest@localhost:5672/";
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "imageviewer.exchange";
    }

    /// <summary>
    /// RabbitMQ 연결을 초기화하고 Exchange와 Queue를 설정합니다.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("RabbitMQ 연결을 초기화합니다. ConnectionString: {ConnectionString}", _connectionString);

            var factory = new ConnectionFactory();
            factory.Uri = new Uri(_connectionString);
            factory.DispatchConsumersAsync = true; // 비동기 컨슈머 지원

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Topic Exchange 선언 (라우팅 키 기반 메시지 전달)
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            // 기본 큐들 선언
            DeclareQueue("image.uploaded");
            DeclareQueue("share.request.created");
            DeclareQueue("share.request.approved");
            DeclareQueue("notification.send");

            _logger.LogInformation("RabbitMQ 연결이 성공적으로 초기화되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ 연결 초기화 중 오류가 발생했습니다.");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 큐를 선언하고 Exchange에 바인딩합니다.
    /// </summary>
    /// <param name="routingKey">라우팅 키</param>
    private void DeclareQueue(string routingKey)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ 채널이 초기화되지 않았습니다.");

        var queueName = $"queue.{routingKey}";
        
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: routingKey);

        _logger.LogDebug("큐 '{QueueName}'이 '{RoutingKey}' 라우팅 키로 바인딩되었습니다.", queueName, routingKey);
    }

    /// <summary>
    /// 이벤트를 RabbitMQ 큐에 발행합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="eventData">발행할 이벤트 데이터</param>
    /// <param name="routingKey">라우팅 키</param>
    /// <returns>발행 성공 여부</returns>
    public async Task<bool> PublishEventAsync<T>(T eventData, string? routingKey = null) where T : class
    {
        try
        {
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ 채널이 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
                return false;
            }

            // 라우팅 키 결정 (기본값: 타입명을 kebab-case로 변환)
            routingKey ??= GetRoutingKeyFromType(typeof(T));

            // 이벤트 데이터를 JSON으로 직렬화
            var jsonData = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(jsonData);

            // 메시지 속성 설정
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 메시지 지속성
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.Headers = new Dictionary<string, object>
            {
                ["eventType"] = typeof(T).Name,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["version"] = "1.0"
            };

            // 메시지 발행
            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("이벤트가 성공적으로 발행되었습니다. EventType: {EventType}, RoutingKey: {RoutingKey}", 
                typeof(T).Name, routingKey);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이벤트 발행 중 오류가 발생했습니다. EventType: {EventType}, RoutingKey: {RoutingKey}", 
                typeof(T).Name, routingKey);
            return false;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 특정 타입의 이벤트를 구독하고 핸들러를 등록합니다.
    /// </summary>
    /// <typeparam name="T">구독할 이벤트 타입</typeparam>
    /// <param name="handler">이벤트 처리 핸들러</param>
    /// <param name="queueName">큐 이름</param>
    public void Subscribe<T>(Func<T, Task> handler, string? queueName = null) where T : class
    {
        try
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ 채널이 초기화되지 않았습니다.");

            var routingKey = GetRoutingKeyFromType(typeof(T));
            queueName ??= $"queue.{routingKey}";

            // 큐가 존재하지 않으면 생성
            DeclareQueue(routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogDebug("메시지를 수신했습니다. Queue: {QueueName}, Message: {Message}", queueName, message);

                    // JSON 역직렬화
                    var eventData = JsonSerializer.Deserialize<T>(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (eventData != null)
                    {
                        // 핸들러 실행
                        await handler(eventData);
                        
                        // 메시지 ACK
                        _channel.BasicAck(eventArgs.DeliveryTag, false);
                        
                        _logger.LogInformation("메시지가 성공적으로 처리되었습니다. Queue: {QueueName}, EventType: {EventType}", 
                            queueName, typeof(T).Name);
                    }
                    else
                    {
                        _logger.LogWarning("메시지 역직렬화에 실패했습니다. Queue: {QueueName}, Message: {Message}", 
                            queueName, message);
                        
                        // 메시지 NACK (재처리하지 않음)
                        _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "메시지 처리 중 오류가 발생했습니다. Queue: {QueueName}", queueName);
                    
                    // 메시지 NACK (재처리하지 않음)
                    _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                }
            };

            // 컨슈머 등록
            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            
            _logger.LogInformation("이벤트 구독이 등록되었습니다. EventType: {EventType}, Queue: {QueueName}", 
                typeof(T).Name, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이벤트 구독 등록 중 오류가 발생했습니다. EventType: {EventType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 타입명에서 라우팅 키를 생성합니다.
    /// 예: ImageUploadedEvent -> image.uploaded
    /// </summary>
    /// <param name="type">이벤트 타입</param>
    /// <returns>라우팅 키</returns>
    private string GetRoutingKeyFromType(Type type)
    {
        var typeName = type.Name;
        
        // Event 접미사 제거
        if (typeName.EndsWith("Event"))
            typeName = typeName.Substring(0, typeName.Length - 5);

        // PascalCase를 kebab-case로 변환
        var result = new StringBuilder();
        for (int i = 0; i < typeName.Length; i++)
        {
            if (i > 0 && char.IsUpper(typeName[i]))
                result.Append('.');
            result.Append(char.ToLower(typeName[i]));
        }

        return result.ToString();
    }

    /// <summary>
    /// RabbitMQ 연결을 해제합니다.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                
                _logger.LogInformation("RabbitMQ 연결이 정리되었습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ 연결 정리 중 오류가 발생했습니다.");
            }

            _disposed = true;
        }
    }

    ~RabbitMQService()
    {
        Dispose(false);
    }
}