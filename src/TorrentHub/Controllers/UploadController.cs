using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Services;
using TorrentHub.Services.Configuration;
using Microsoft.EntityFrameworkCore;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storageService;
    private readonly StorageSettings _storageSettings;
    private readonly ILogger<UploadController> _logger;
    private readonly ApplicationDbContext _context;

    public UploadController(
        IFileStorageService storageService,
        IOptions<StorageSettings> storageSettings,
        ILogger<UploadController> logger,
        ApplicationDbContext context)
    {
        _storageService = storageService;
        _storageSettings = storageSettings.Value;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 上传图片到对象存储
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <returns>上传结果，包含文件 URL</returns>
    [HttpPost("image")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB 限制
    [ProducesResponseType(typeof(ApiResponse<UploadFileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UploadFileResponseDto>>> UploadImage(IFormFile file)
    {
        try
        {
            // 验证文件是否存在
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "未选择文件或文件为空"
                });
            }

            // 验证文件大小
            if (file.Length > _storageSettings.MaxFileSize)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"文件大小超过限制 ({_storageSettings.MaxFileSize / 1024 / 1024}MB)"
                });
            }

            // 验证文件扩展名
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_storageSettings.AllowedImageExtensions.Contains(extension))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"不支持的文件格式。允许的格式: {string.Join(", ", _storageSettings.AllowedImageExtensions)}"
                });
            }

            // 验证 MIME 类型
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "文件必须是图片格式"
                });
            }

            // 上传文件
            await using var stream = file.OpenReadStream();
            var fileUrl = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

            _logger.LogInformation("文件上传成功: {FileName} -> {Url}", file.FileName, fileUrl);

            return Ok(new ApiResponse<UploadFileResponseDto>
            {
                Success = true,
                Data = new UploadFileResponseDto
                {
                    Url = fileUrl,
                    FileName = Path.GetFileName(new Uri(fileUrl).LocalPath),
                    FileSize = file.Length,
                    ContentType = file.ContentType
                },
                Message = "文件上传成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {FileName}", file?.FileName);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"文件上传失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 删除已上传的文件 (仅管理员)
    /// </summary>
    /// <param name="fileName">文件名</param>
    [HttpDelete("{fileName}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFile(string fileName)
    {
        try
        {
            var exists = await _storageService.ExistsAsync(fileName);
            if (!exists)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "文件不存在"
                });
            }

            var success = await _storageService.DeleteAsync(fileName);
            if (success)
            {
                _logger.LogInformation("文件删除成功: {FileName}", fileName);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "文件删除成功"
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "文件删除失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败: {FileName}", fileName);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"文件删除失败: {ex.Message}"
            });
        }
    }

}