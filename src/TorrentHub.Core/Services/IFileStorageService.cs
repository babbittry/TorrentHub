namespace TorrentHub.Core.Services;

/// <summary>
/// 文件存储服务接口 (支持 S3 兼容存储: Cloudflare R2, MinIO, AWS S3 等)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件到对象存储
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名 (包含扩展名)</param>
    /// <param name="contentType">MIME 类型 (如: image/jpeg)</param>
    /// <returns>文件的公共访问 URL</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    Task<bool> DeleteAsync(string fileName);

    /// <summary>
    /// 获取文件的公共访问 URL
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件 URL</returns>
    string GetPublicUrl(string fileName);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="fileName">文件名</param>
    Task<bool> ExistsAsync(string fileName);

    /// <summary>
    /// 从对象存储下载文件到内存流
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件内容的内存流</returns>
    Task<Stream> DownloadAsync(string fileName);

    /// <summary>
    /// 上传文件到对象存储 (指定文件名，不生成随机名称)
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">指定的文件名 (如: infohash.torrent)</param>
    /// <param name="contentType">MIME 类型</param>
    /// <param name="useOriginalName">是否使用原始文件名 (true) 或生成 UUID (false)</param>
    /// <returns>文件的公共访问 URL</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, bool useOriginalName);
}