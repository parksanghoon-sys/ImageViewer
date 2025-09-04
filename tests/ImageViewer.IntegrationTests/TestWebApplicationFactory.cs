using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Configuration;
using ImageViewer.Domain.Enums;
using ImageViewer.Infrastructure.MessageBus;

namespace ImageViewer.IntegrationTests;

/// <summary>
/// 통합 테스트를 위한 웹 애플리케이션 팩토리
/// </summary>
public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _serviceName;
    
    public TestWebApplicationFactory(string serviceName)
    {
        _serviceName = serviceName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 기존 데이터베이스 컨텍스트 제거
            RemoveDbContexts(services);

            // InMemory 데이터베이스로 교체
            ConfigureInMemoryDatabases(services);

            // 테스트용 RabbitMQ 서비스 교체
            ConfigureTestRabbitMQ(services);

            // 로깅 설정
            ConfigureTestLogging(services);
        });

        builder.UseEnvironment("Testing");
    }

    private void RemoveDbContexts(IServiceCollection services)
    {
        // ApplicationDbContext 제거
        var applicationDbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        if (applicationDbContextDescriptor != null)
        {
            services.Remove(applicationDbContextDescriptor);
        }

        // AuthContext 제거
        var authContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<AuthContext>));
        if (authContextDescriptor != null)
        {
            services.Remove(authContextDescriptor);
        }
    }

    private void ConfigureInMemoryDatabases(IServiceCollection services)
    {
        // DatabaseOptions 설정
        services.Configure<DatabaseOptions>(options =>
        {
            options.Type = DatabaseType.InMemory;
            options.ConnectionStrings["InMemory"] = $"ImageViewer_Test_{_serviceName}_{Guid.NewGuid()}";
        });

        // ApplicationDbContext InMemory 설정
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_Application_{_serviceName}_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
        });

        // AuthContext InMemory 설정
        services.AddDbContext<AuthContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_Auth_{_serviceName}_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
        });
    }

    private void ConfigureTestRabbitMQ(IServiceCollection services)
    {
        // 실제 RabbitMQ 서비스 제거
        var rabbitMQDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IRabbitMQService));
        if (rabbitMQDescriptor != null)
        {
            services.Remove(rabbitMQDescriptor);
        }

        // 테스트용 Mock RabbitMQ 서비스 추가
        services.AddSingleton<IRabbitMQService, MockRabbitMQService>();
    }

    private void ConfigureTestLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // 데이터베이스 초기화
        using (var scope = host.Services.CreateScope())
        {
            try
            {
                var applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                applicationDbContext?.Database.EnsureCreated();

                var authContext = scope.ServiceProvider.GetService<AuthContext>();
                authContext?.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestWebApplicationFactory<TProgram>>>();
                logger.LogError(ex, "테스트 데이터베이스 초기화 중 오류 발생");
            }
        }

        return host;
    }
}

/// <summary>
/// 테스트용 Mock RabbitMQ 서비스
/// </summary>
public class MockRabbitMQService : IRabbitMQService
{
    private readonly List<(string routingKey, object message)> _publishedMessages = new();
    private readonly Dictionary<string, List<Func<object, Task>>> _subscriptions = new();

    public Task<bool> PublishEventAsync<T>(T eventData, string? routingKey = null) where T : class
    {
        var key = routingKey ?? typeof(T).Name;
        _publishedMessages.Add((key, eventData));
        
        // 구독자가 있으면 즉시 처리
        if (_subscriptions.TryGetValue(key, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                Task.Run(() => subscriber(eventData));
            }
        }
        
        return Task.FromResult(true);
    }

    public void Subscribe<T>(Func<T, Task> handler, string? queueName = null) where T : class
    {
        var key = queueName ?? typeof(T).Name;
        
        if (!_subscriptions.ContainsKey(key))
        {
            _subscriptions[key] = new List<Func<object, Task>>();
        }

        _subscriptions[key].Add(async (message) =>
        {
            if (message is T typedMessage)
            {
                await handler(typedMessage);
            }
        });
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    // 테스트용 헬퍼 메서드들 (기존 테스트와의 호환성을 위해 유지)
    public void Publish<T>(string routingKey, T message) where T : class
    {
        _ = PublishEventAsync(message, routingKey);
    }

    public void Subscribe<T>(string routingKey, Func<T, Task> handler) where T : class
    {
        Subscribe(handler, routingKey);
    }

    public List<(string routingKey, object message)> GetPublishedMessages() => _publishedMessages;

    public void ClearMessages() => _publishedMessages.Clear();

    public void Dispose()
    {
        // Mock 서비스는 특별한 정리 작업 불필요
    }
}