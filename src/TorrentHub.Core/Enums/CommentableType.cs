namespace TorrentHub.Core.Enums;

/// <summary>
/// 评论目标类型枚举
/// </summary>
public enum CommentableType
{
    /// <summary>
    /// 种子评论
    /// </summary>
    Torrent,
    
    /// <summary>
    /// 求种评论
    /// </summary>
    Request,
    
    /// <summary>
    /// 论坛主题评论
    /// </summary>
    ForumTopic
}