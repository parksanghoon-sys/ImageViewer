using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageViewer.ImageService.Services;

/// <summary>
/// 이미지 처리 서비스 구현
/// ImageSharp을 사용하여 이미지 리사이즈, 썸네일 생성, 블러 처리 등을 수행
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly string _imageStoragePath;
    private readonly string[] _supportedFormats = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    /// <summary>
    /// ImageProcessingService 생성자
    /// </summary>
    /// <param name="configuration">설정</param>
    /// <param name="logger">로거</param>
    public ImageProcessingService(IConfiguration configuration, ILogger<ImageProcessingService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 이미지 저장 경로 설정 (기본값: wwwroot/images)
        _imageStoragePath = _configuration["ImageStorage:Path"] ?? Path.Combine("wwwroot", "images");
        
        // 저장 디렉토리가 없으면 생성
        Directory.CreateDirectory(_imageStoragePath);
    }

    /// <summary>
    /// 이미지 파일을 저장하고 썸네일을 생성
    /// </summary>
    public async Task<(string OriginalPath, string ThumbnailPath, long FileSize)> SaveImageAsync(
        Stream imageStream, string fileName, Guid userId)
    {
        try
        {
            if (!IsSupportedImageFormat(fileName))
            {
                throw new ArgumentException($"지원되지 않는 이미지 형식입니다: {fileName}");
            }

            // 사용자별 디렉토리 생성
            var userDirectory = GetUserImageDirectory(userId);
            Directory.CreateDirectory(userDirectory);

            // 고유 파일명 생성
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var originalPath = Path.Combine(userDirectory, uniqueFileName);

            // 원본 이미지 저장
            using (var fileStream = new FileStream(originalPath, FileMode.Create))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            var fileInfo = new FileInfo(originalPath);
            var fileSize = fileInfo.Length;

            // 썸네일 생성
            var thumbnailPath = await CreateThumbnailAsync(originalPath);

            _logger.LogInformation("이미지 저장 완료: {FileName}, 크기: {FileSize} bytes", 
                uniqueFileName, fileSize);

            return (originalPath, thumbnailPath, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 저장 중 오류 발생: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// 썸네일 이미지 생성
    /// </summary>
    public async Task<string> CreateThumbnailAsync(string originalPath, int thumbnailSize = 200)
    {
        try
        {
            var directory = Path.GetDirectoryName(originalPath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var thumbnailPath = Path.Combine(directory!, $"{fileNameWithoutExt}_thumb{extension}");

            using (var image = await Image.LoadAsync(originalPath))
            {
                // 비율을 유지하면서 리사이즈
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(thumbnailSize, thumbnailSize),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(thumbnailPath, new JpegEncoder { Quality = 80 });
            }

            _logger.LogDebug("썸네일 생성 완료: {ThumbnailPath}", thumbnailPath);
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "썸네일 생성 중 오류 발생: {OriginalPath}", originalPath);
            throw;
        }
    }

    /// <summary>
    /// 블러 처리된 미리보기 이미지 생성
    /// </summary>
    public async Task<string> CreateBlurredPreviewAsync(string originalPath, float blurRadius = 10f)
    {
        try
        {
            var directory = Path.GetDirectoryName(originalPath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var blurredPath = Path.Combine(directory!, $"{fileNameWithoutExt}_blur{extension}");

            using (var image = await Image.LoadAsync(originalPath))
            {
                // 작은 크기로 리사이즈한 후 블러 적용
                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(400, 400),
                        Mode = ResizeMode.Max
                    })
                    .GaussianBlur(blurRadius));

                await image.SaveAsync(blurredPath, new JpegEncoder { Quality = 60 });
            }

            _logger.LogDebug("블러 미리보기 생성 완료: {BlurredPath}", blurredPath);
            return blurredPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "블러 미리보기 생성 중 오류 발생: {OriginalPath}", originalPath);
            throw;
        }
    }

    /// <summary>
    /// 이미지 파일 삭제
    /// </summary>
    public async Task DeleteImageAsync(string imagePath)
    {
        try
        {
            if (File.Exists(imagePath))
            {
                await Task.Run(() => File.Delete(imagePath));
                _logger.LogInformation("이미지 파일 삭제 완료: {ImagePath}", imagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 파일 삭제 중 오류 발생: {ImagePath}", imagePath);
            throw;
        }
    }

    /// <summary>
    /// 지원되는 이미지 형식인지 확인
    /// </summary>
    public bool IsSupportedImageFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _supportedFormats.Contains(extension);
    }

    /// <summary>
    /// 사용자별 이미지 저장 디렉토리 경로 생성
    /// </summary>
    public string GetUserImageDirectory(Guid userId)
    {
        return Path.Combine(_imageStoragePath, userId.ToString());
    }
}