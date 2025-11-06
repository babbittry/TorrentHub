using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly ITMDbService _tmdbService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(ITMDbService tmdbService, ILogger<MediaController> logger)
    {
        _tmdbService = tmdbService;
        _logger = logger;
    }

    /// <summary>
    /// 通过豆瓣/IMDb ID或链接获取媒体元数据
    /// </summary>
    /// <param name="input">豆瓣ID、豆瓣URL、IMDb ID或IMDb URL</param>
    /// <param name="language">语言代码 (默认: zh-CN)</param>
    /// <returns>媒体元数据</returns>
    [HttpGet("metadata")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TMDbMovieDto>>> GetMediaMetadata(
        [FromQuery] string input,
        [FromQuery] string language = "zh-CN")
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "输入不能为空"
            });
        }

        _logger.LogInformation("Fetching media metadata for input: {Input}, language: {Language}", input, language);

        var result = await _tmdbService.GetMediaByInputAsync(input, language);

        if (result == null)
        {
            _logger.LogWarning("No media found for input: {Input}", input);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "未找到媒体信息，请检查输入的ID或链接是否正确"
            });
        }

        return Ok(new ApiResponse<TMDbMovieDto>
        {
            Success = true,
            Data = result,
            Message = "Media metadata retrieved successfully."
        });
    }
}