using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IPublicSettingsService _publicSettingsService;

    public SettingsController(IPublicSettingsService publicSettingsService)
    {
        _publicSettingsService = publicSettingsService;
    }

    /// <summary>
    /// 获取匿名用户可访问的公开配置
    /// 无需认证，供注册页、登录页、页面布局等使用
    /// </summary>
    [HttpGet("public/anonymous")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AnonymousPublicSettingsDto>>> GetAnonymousPublicSettings()
    {
        var settings = await _publicSettingsService.GetAnonymousPublicSettingsAsync();
        return Ok(ApiResponse<AnonymousPublicSettingsDto>.SuccessResult(settings));
    }

    /// <summary>
    /// 获取认证用户可访问的完整公开配置
    /// 需要认证，包含金币系统等更多配置
    /// </summary>
    [HttpGet("public")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PublicSiteSettingsDto>>> GetPublicSettings()
    {
        var settings = await _publicSettingsService.GetPublicSiteSettingsAsync();
        return Ok(ApiResponse<PublicSiteSettingsDto>.SuccessResult(settings));
    }
}