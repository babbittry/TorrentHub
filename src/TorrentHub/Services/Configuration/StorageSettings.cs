namespace TorrentHub.Services.Configuration;

/// <summary>
/// S3 兼容对象存储配置 (支持 Cloudflare R2, MinIO, AWS S3 等)
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// 服务 URL (R2: https://[account-id].r2.cloudflarestorage.com, MinIO: http://localhost:9000)
    /// </summary>
    public required string ServiceUrl { get; set; }

    /// <summary>
    /// Access Key ID
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// Secret Access Key
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public required string BucketName { get; set; }

    /// <summary>
    /// 公共访问域名 (用于生成文件 URL)
    /// 例如: https://img.yoursite.com 或 https://pub-xxxxx.r2.dev
    /// </summary>
    public required string PublicDomain { get; set; }

    /// <summary>
    /// 区域 (对于 R2 通常为 auto, MinIO 可以是 us-east-1)
    /// </summary>
    public string Region { get; set; } = "auto";

    /// <summary>
    /// 是否强制使用路径样式 (MinIO 通常需要设为 true)
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// 允许上传的最大文件大小 (字节)
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 允许的图片格式
    /// </summary>
    public string[] AllowedImageExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
}