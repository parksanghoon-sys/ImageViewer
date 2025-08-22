using ImageViewer.Domain.Common;
using System;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// 사용자 설정 엔티티
/// 미리보기 크기, 개수 등 사용자별 개인화 설정을 관리
/// </summary>
public class UserSettings : BaseEntity
{
    /// <summary>
    /// 설정을 소유한 사용자 ID (AuthContext의 ApplicationUser.Id 참조)
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// 미리보기 이미지 개수 (페이지당)
    /// </summary>
    public int PreviewCount { get; private set; } = 12;

    /// <summary>
    /// 미리보기 이미지 크기 (픽셀)
    /// </summary>
    public int PreviewSize { get; private set; } = 200;

    /// <summary>
    /// 블러 처리 강도 (0-100)
    /// </summary>
    public int BlurIntensity { get; private set; } = 50;

    /// <summary>
    /// 자동 썸네일 생성 여부
    /// </summary>
    public bool AutoGenerateThumbnails { get; private set; } = true;

    /// <summary>
    /// 공유 요청 알림 수신 여부
    /// </summary>
    public bool ReceiveShareNotifications { get; private set; } = true;

    /// <summary>
    /// 이메일 알림 수신 여부
    /// </summary>
    public bool ReceiveEmailNotifications { get; private set; } = false;

    /// <summary>
    /// 다크 모드 사용 여부
    /// </summary>
    public bool UseDarkMode { get; private set; } = false;


    /// <summary>
    /// 기본 생성자 (EF Core용)
    /// </summary>
    protected UserSettings() { }

    /// <summary>
    /// 새 사용자 설정 생성
    /// </summary>
    /// <param name="userId">사용자 ID (ApplicationUser.Id)</param>
    public UserSettings(string userId)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    /// <summary>
    /// 미리보기 설정 업데이트
    /// </summary>
    /// <param name="count">미리보기 개수</param>
    /// <param name="size">미리보기 크기</param>
    public void UpdatePreviewSettings(int count, int size)
    {
        if (count < 1 || count > 100)
            throw new ArgumentException("미리보기 개수는 1-100 사이여야 합니다.", nameof(count));

        if (size < 50 || size > 500)
            throw new ArgumentException("미리보기 크기는 50-500 픽셀 사이여야 합니다.", nameof(size));

        PreviewCount = count;
        PreviewSize = size;
        MarkAsModified();
    }

    /// <summary>
    /// 블러 강도 설정
    /// </summary>
    /// <param name="intensity">블러 강도 (0-100)</param>
    public void SetBlurIntensity(int intensity)
    {
        if (intensity < 0 || intensity > 100)
            throw new ArgumentException("블러 강도는 0-100 사이여야 합니다.", nameof(intensity));

        BlurIntensity = intensity;
        MarkAsModified();
    }

    /// <summary>
    /// 자동 썸네일 생성 설정
    /// </summary>
    /// <param name="enabled">자동 생성 여부</param>
    public void SetAutoThumbnailGeneration(bool enabled)
    {
        AutoGenerateThumbnails = enabled;
        MarkAsModified();
    }

    /// <summary>
    /// 알림 설정 업데이트
    /// </summary>
    /// <param name="shareNotifications">공유 요청 알림</param>
    /// <param name="emailNotifications">이메일 알림</param>
    public void UpdateNotificationSettings(bool shareNotifications, bool emailNotifications)
    {
        ReceiveShareNotifications = shareNotifications;
        ReceiveEmailNotifications = emailNotifications;
        MarkAsModified();
    }

    /// <summary>
    /// 다크 모드 설정
    /// </summary>
    /// <param name="enabled">다크 모드 사용 여부</param>
    public void SetDarkMode(bool enabled)
    {
        UseDarkMode = enabled;
        MarkAsModified();
    }

    /// <summary>
    /// 미리보기 개수 업데이트
    /// </summary>
    /// <param name="count">미리보기 개수</param>
    public void UpdatePreviewCount(int count)
    {
        if (count < 1 || count > 100)
            throw new ArgumentException("미리보기 개수는 1-100 사이여야 합니다.", nameof(count));

        PreviewCount = count;
        MarkAsModified();
    }

    /// <summary>
    /// 블러 썸네일 설정 업데이트
    /// </summary>
    /// <param name="enabled">블러 처리 여부</param>
    public void UpdateBlurThumbnails(bool enabled)
    {
        // 블러 강도를 0 또는 기본값으로 설정
        BlurIntensity = enabled ? 50 : 0;
        MarkAsModified();
    }

    /// <summary>
    /// 다크 모드 업데이트
    /// </summary>
    /// <param name="enabled">다크 모드 사용 여부</param>
    public void UpdateDarkMode(bool enabled)
    {
        UseDarkMode = enabled;
        MarkAsModified();
    }

    /// <summary>
    /// 블러 썸네일 사용 여부 확인
    /// </summary>
    public bool BlurThumbnails => BlurIntensity > 0;

    /// <summary>
    /// 설정을 기본값으로 초기화
    /// </summary>
    public void ResetToDefaults()
    {
        PreviewCount = 12;
        PreviewSize = 200;
        BlurIntensity = 50;
        AutoGenerateThumbnails = true;
        ReceiveShareNotifications = true;
        ReceiveEmailNotifications = false;
        UseDarkMode = false;
        MarkAsModified();
    }
}