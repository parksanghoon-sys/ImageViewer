using ImageViewer.Infrastructure.MessageBus;
using ImageViewer.Contracts.Events;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddCors();

// Add DbContext for notifications
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Add services
builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();

// Configure RabbitMQ
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Add RabbitMQ service
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Add hosted service for consuming messages
builder.Services.AddHostedService<NotificationConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageViewer Notification Service API";
        options.Theme = Scalar.AspNetCore.ScalarTheme.BluePlanet;
        options.ShowSidebar = true;
        options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
    });
}

// Configure CORS for SignalR
app.UseCors(policy =>
{
    policy.WithOrigins("http://localhost:3000", "http://frontend:3000")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

// Map notification APIs
app.MapGet("/api/notifications/{userId:guid}", async (Guid userId, INotificationService notificationService) =>
{
    var notifications = await notificationService.GetUserNotificationsAsync(userId);
    return Results.Ok(notifications);
});

app.MapPost("/api/notifications/{notificationId:guid}/mark-read", async (Guid notificationId, INotificationService notificationService) =>
{
    await notificationService.MarkAsReadAsync(notificationId);
    return Results.Ok();
});

app.MapPost("/api/notifications/user/{userId:guid}/mark-all-read", async (Guid userId, INotificationService notificationService) =>
{
    await notificationService.MarkAllAsReadAsync(userId);
    return Results.Ok();
});

app.MapGet("/api/notifications/{userId:guid}/unread-count", async (Guid userId, INotificationService notificationService) =>
{
    var count = await notificationService.GetUnreadCountAsync(userId);
    return Results.Ok(new { count });
});

app.Run();

// SignalR Hub for real-time notifications
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }
}

// Background service to consume RabbitMQ messages
public class NotificationConsumerService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NotificationConsumerService(
        IRabbitMQService rabbitMQService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _rabbitMQService = rabbitMQService;
        _hubContext = hubContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // Wait for RabbitMQ to be ready

        // Subscribe to image upload events
        _rabbitMQService.Subscribe<ImageUploadedEvent>(async (message) =>
        {
            _logger.LogInformation($"Processing image upload notification for user {message.UserId}");
            
            // Save notification to database
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            var notification = await notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = Guid.Parse(message.UserId),
                Title = "이미지 업로드 완료",
                Message = $"'{message.OriginalFileName}' 이미지가 성공적으로 업로드되었습니다.",
                Type = "ImageUploaded",
                Data = new { ImageId = message.ImageId, FileName = message.OriginalFileName }
            });
            
            // Send real-time notification
            await _hubContext.Clients.Group($"user_{message.UserId}")
                .SendAsync("ReceiveNotification", notification, stoppingToken);
        }, "image.uploaded");

        // Subscribe to share request events
        _rabbitMQService.Subscribe<ShareRequestCreatedEvent>(async (message) =>
        {
            _logger.LogInformation($"Processing share request notification for user {message.TargetUserId}");
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            var notification = await notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = Guid.Parse(message.TargetUserId),
                Title = "공유 요청",
                Message = $"이미지 '{message.ImageFileName}' 공유 요청이 있습니다.",
                Type = "ShareRequest",
                Data = new { ShareRequestId = message.ShareRequestId, ImageId = message.ImageId, RequesterId = message.RequesterId }
            });
            
            await _hubContext.Clients.Group($"user_{message.TargetUserId}")
                .SendAsync("ReceiveNotification", notification, stoppingToken);
        }, "share.request.created");

        // Subscribe to share request approval events
        _rabbitMQService.Subscribe<ShareRequestApprovedEvent>(async (message) =>
        {
            _logger.LogInformation($"Processing share approval notification for user {message.RequesterId}");
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            var notification = await notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = Guid.Parse(message.RequesterId),
                Title = "공유 승인",
                Message = $"이미지 '{message.ImageFileName}' 공유 요청이 승인되었습니다.",
                Type = "ShareApproved",
                Data = new { ShareRequestId = message.ShareRequestId, ImageId = message.ImageId, OwnerId = message.OwnerId }
            });
            
            await _hubContext.Clients.Group($"user_{message.RequesterId}")
                .SendAsync("ReceiveNotification", notification, stoppingToken);
        }, "share.request.approved");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public class RabbitMQSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "rabbitmq";
    public string Password { get; set; } = "rabbitmq123";
}

// Notification Entity
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

// DbContext
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }
    
    public DbSet<Notification> Notifications { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });
    }
}

// DTOs
public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

// Service Interface
public interface INotificationService
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
}

// Service Implementation
public class NotificationServiceImpl : INotificationService
{
    private readonly NotificationDbContext _context;
    
    public NotificationServiceImpl(NotificationDbContext context)
    {
        _context = context;
    }
    
    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        return MapToDto(notification);
    }
    
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
            
        return notifications.Select(MapToDto).ToList();
    }
    
    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
            
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }
    
    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Data = notification.Data,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}
