namespace TorrentHub.Core.DTOs;

/// <summary>
/// 认证用户可访问的完整公开配置
/// 包含匿名用户可见字段 + 金币系统相关配置
/// </summary>
public class PublicSiteSettingsDto
{
    // === 匿名用户可访问字段 ===
    
    /// <summary>
    /// 站点名称
    /// </summary>
    public string SiteName { get; set; } = "TorrentHub";
    
    /// <summary>
    /// 站点Logo URL
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }
    
    /// <summary>
    /// 是否开放注册
    /// </summary>
    public bool IsRegistrationOpen { get; set; } = false;
    
    /// <summary>
    /// 是否启用论坛功能
    /// </summary>
    public bool IsForumEnabled { get; set; } = true;
    
    // === 认证用户可访问字段 ===
    
    /// <summary>
    /// 是否启用求种系统
    /// </summary>
    public bool IsRequestSystemEnabled { get; set; } = true;
    
    /// <summary>
    /// 创建求种所需金币
    /// </summary>
    public ulong CreateRequestCost { get; set; } = 1000;
    
    /// <summary>
    /// 填充求种获得的奖励金币
    /// </summary>
    public ulong FillRequestBonus { get; set; } = 500;
    
    /// <summary>
    /// 打赏税率
    /// </summary>
    public double TipTaxRate { get; set; } = 0.10;
    
    /// <summary>
    /// 转账税率
    /// </summary>
    public double TransferTaxRate { get; set; } = 0.05;
    
    /// <summary>
    /// 邀请码价格
    /// </summary>
    public uint InvitePrice { get; set; } = 5000;
    
    /// <summary>
    /// 评论奖励金币数
    /// </summary>
    public ulong CommentBonus { get; set; } = 10;
    
    /// <summary>
    /// 上传种子奖励金币数
    /// </summary>
    public ulong UploadTorrentBonus { get; set; } = 250;
    
    /// <summary>
    /// 每日最多可获得评论奖励的次数
    /// </summary>
    public int MaxDailyCommentBonuses { get; set; } = 10;
}