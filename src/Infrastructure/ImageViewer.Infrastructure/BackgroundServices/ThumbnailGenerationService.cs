using ImageViewer.Contracts.Events;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageViewer.Infrastructure.BackgroundServices;

/// <summary>
/// 썸네일 생성 백그라운드 서비스
/// RabbitMQ에서 ImageUploadedEvent를 구독하여 비동기로 썸네일을 생성합니다.
/// </summary>
public class ThumbnailGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThumbnailGenerationService> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly int _thumbnailWidth = 300;
    private readonly int _thumbnailHeight = 300;

    public ThumbnailGenerationService(
        IServiceProvider serviceProvider, 
        ILogger<ThumbnailGenerationService> logger,
        IRabbitMQService rabbitMQService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMQService = rabbitMQService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("썸네일 생성 백그라운드 서비스가 시작되었습니다.");

        try
        {
            // RabbitMQ 초기화
            await _rabbitMQService.InitializeAsync();

            // ImageUploadedEvent 구독
            _rabbitMQService.Subscribe<ImageUploadedEvent>(HandleImageUploadedEvent);

            _logger.LogInformation("ImageUploadedEvent 구독이 등록되었습니다.");

            // 서비스가 중지될 때까지 대기
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "썸네일 생성 서비스 실행 중 오류가 발생했습니다.");
        }

        _logger.LogInformation("썸네일 생성 백그라운드 서비스가 종료되었습니다.");
    }

    /// <summary>
    /// 이미지 업로드 이벤트 처리 핸들러
    /// </summary>
    /// <param name="uploadedEvent">이미지 업로드 이벤트</param>
    private async Task HandleImageUploadedEvent(ImageUploadedEvent uploadedEvent)
    {
        try
        {
            _logger.LogInformation("썸네일 생성 시작: ImageId={ImageId}, UserId={UserId}", 
                uploadedEvent.ImageId, uploadedEvent.UserId);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 이미지 정보 조회
            var image = await context.Images
                .FirstOrDefaultAsync(i => i.Id == uploadedEvent.ImageId);

            if (image == null)
            {
                _logger.LogWarning("썸네일 생성 실패: 이미지를 찾을 수 없음. ImageId={ImageId}", uploadedEvent.ImageId);
                return;
            }

            // 이미 썸네일이 생성된 경우 스킵
            if (image.ThumbnailReady)
            {
                _logger.LogInformation("썸네일이 이미 생성되어 있습니다. ImageId={ImageId}", uploadedEvent.ImageId);
                return;
            }

            // 원본 이미지 파일 경로 구성
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var originalPath = Path.Combine(webRoot, uploadedEvent.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(originalPath))
            {
                _logger.LogWarning("썸네일 생성 실패: 원본 파일을 찾을 수 없음. FilePath={FilePath}", originalPath);
                return;
            }

            // 썸네일 저장 경로 구성
            var thumbnailDir = Path.Combine(webRoot, "uploads", "thumbnails", uploadedEvent.UserId);
            Directory.CreateDirectory(thumbnailDir);

            var thumbnailFileName = $"thumb_{Path.GetFileName(uploadedEvent.FilePath)}";
            var thumbnailPath = Path.Combine(thumbnailDir, thumbnailFileName);

            // 썸네일 생성
            await GenerateThumbnailAsync(originalPath, thumbnailPath);

            // DB 업데이트 (상대 경로로 저장)
            var relativeThumbnailPath = GetRelativePath(thumbnailPath, webRoot);
            image.SetThumbnailPath(relativeThumbnailPath);
            
            await context.SaveChangesAsync();

            _logger.LogInformation("썸네일 생성 완료: ImageId={ImageId}, ThumbnailPath={ThumbnailPath}", 
                uploadedEvent.ImageId, relativeThumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "썸네일 생성 처리 중 오류 발생: ImageId={ImageId}", uploadedEvent.ImageId);
        }
    }

    /// <summary>
    /// 썸네일 이미지를 생성합니다.
    /// </summary>
    /// <param name="originalPath">원본 이미지 경로</param>
    /// <param name="thumbnailPath">썸네일 저장 경로</param>
    private async Task GenerateThumbnailAsync(string originalPath, string thumbnailPath)
    {
        using var originalImage = await Image.LoadAsync<Rgba32>(originalPath);
        
        // 썸네일 크기 계산 (비율 유지)
        var (width, height) = CalculateThumbnailSize(originalImage.Width, originalImage.Height);

        // 이미지 리사이즈
        originalImage.Mutate(x => x.Resize(width, height));

        // JPEG 형식으로 저장 (품질 85)
        var encoder = new JpegEncoder { Quality = 85 };
        await originalImage.SaveAsync(thumbnailPath, encoder);
        
        _logger.LogDebug("썸네일 파일 생성됨: {ThumbnailPath}, 크기: {Width}x{Height}", 
            thumbnailPath, width, height);
    }

    /// <summary>
    /// 썸네일 크기를 계산합니다 (비율 유지).
    /// </summary>
    /// <param name="originalWidth">원본 이미지 너비</param>
    /// <param name="originalHeight">원본 이미지 높이</param>
    /// <returns>썸네일 크기 (너비, 높이)</returns>
    private (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight)
    {
        var widthRatio = (double)_thumbnailWidth / originalWidth;
        var heightRatio = (double)_thumbnailHeight / originalHeight;
        var ratio = Math.Min(widthRatio, heightRatio);

        var newWidth = (int)(originalWidth * ratio);
        var newHeight = (int)(originalHeight * ratio);

        return (newWidth, newHeight);
    }

    /// <summary>
    /// 절대 경로를 웹 상대 경로로 변환합니다.
    /// </summary>
    /// <param name="fullPath">절대 경로</param>
    /// <param name="webRoot">웹 루트 경로</param>
    /// <returns>상대 경로</returns>
    private string GetRelativePath(string fullPath, string webRoot)
    {
        var relativePath = fullPath.Replace(webRoot, "").Replace("\\", "/");
        return relativePath.StartsWith("/") ? relativePath : "/" + relativePath;
    }

    public override void Dispose()
    {
        _rabbitMQService?.Dispose();
        base.Dispose();
    }
}