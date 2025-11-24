using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TorrentHub.Core.Services;
using TorrentHub.Services.Configuration;

namespace TorrentHub.Services;

/// <summary>
/// S3 兼容对象存储服务 (支持 Cloudflare R2, MinIO, AWS S3 等)
/// </summary>
public class S3StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(
        IAmazonS3 s3Client,
        IOptions<StorageSettings> settings,
        ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        return await UploadAsync(stream, fileName, contentType, useOriginalName: false);
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, bool useOriginalName)
    {
        try
        {
            // 根据参数决定是否生成唯一文件名
            var finalFileName = useOriginalName
                ? fileName
                : $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = finalFileName,
                InputStream = stream,
                ContentType = contentType,
                // 设置为公开可读
                CannedACL = S3CannedACL.PublicRead
            };

            var response = await _s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("文件上传成功: {FileName} -> {FinalFileName}", fileName, finalFileName);
                return GetPublicUrl(finalFileName);
            }

            _logger.LogError("文件上传失败: {FileName}, StatusCode: {StatusCode}", fileName, response.HttpStatusCode);
            throw new Exception($"文件上传失败: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 上传异常: {Message}", ex.Message);
            throw new Exception($"对象存储上传失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string fileName)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = fileName
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("文件删除成功: {FileName}", fileName);
                return true;
            }

            _logger.LogWarning("文件删除失败: {FileName}, StatusCode: {StatusCode}", fileName, response.HttpStatusCode);
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 删除异常: {FileName}, {Message}", fileName, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetPublicUrl(string fileName)
    {
        // 确保 PublicDomain 不以斜杠结尾
        var domain = _settings.PublicDomain.TrimEnd('/');
        return $"{domain}/{fileName}";
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string fileName)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = fileName
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "检查文件存在性时发生异常: {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(string fileName)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = fileName
            };

            var response = await _s3Client.GetObjectAsync(request);
            
            // 将响应流复制到内存流，以便调用者可以多次读取
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation("文件下载成功: {FileName}, Size: {Size} bytes", fileName, memoryStream.Length);
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("文件不存在: {FileName}", fileName);
            throw new FileNotFoundException($"文件不存在: {fileName}", ex);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 下载异常: {FileName}, {Message}", fileName, ex.Message);
            throw new Exception($"对象存储下载失败: {ex.Message}", ex);
        }
    }
}