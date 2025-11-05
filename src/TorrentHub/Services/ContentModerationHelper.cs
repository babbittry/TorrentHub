using TorrentHub.Core.Enums;

namespace TorrentHub.Services;

/// <summary>
/// 内容审核帮助类，提供删除和编辑权限验证
/// </summary>
public static class ContentModerationHelper
{
    /// <summary>
    /// 检查用户是否可以删除评论/帖子
    /// </summary>
    /// <param name="authorId">内容作者ID</param>
    /// <param name="currentUserId">当前用户ID</param>
    /// <param name="userRole">当前用户角色</param>
    /// <param name="replyCount">回复数量</param>
    /// <param name="createdAt">创建时间</param>
    /// <param name="editWindowMinutes">编辑窗口（分钟）</param>
    /// <param name="isFirstPost">是否为楼主帖（仅论坛）</param>
    /// <returns>是否可以删除及错误消息</returns>
    public static (bool CanDelete, string ErrorMessage) CanDeleteContent(
        int authorId,
        int currentUserId,
        UserRole userRole,
        int replyCount,
        DateTimeOffset createdAt,
        int editWindowMinutes,
        bool isFirstPost = false)
    {
        // 规则 1: 管理员可以删除任何内容
        if (userRole == UserRole.Administrator)
            return (true, string.Empty);

        // 规则 2: 楼主帖永远不能删除
        if (isFirstPost)
            return (false, "Cannot delete the first post of a topic");

        // 规则 3: 只能删除自己的内容
        if (authorId != currentUserId)
            return (false, "You are not authorized to delete this content");

        // 规则 4: 有回复的内容不能删除
        if (replyCount > 0)
            return (false, "Cannot delete content with replies");

        // 规则 5: 超过时间窗口不能删除
        var elapsed = DateTimeOffset.UtcNow - createdAt;
        if (elapsed.TotalMinutes > editWindowMinutes)
            return (false, $"Edit window expired (limit: {editWindowMinutes} minutes)");

        return (true, string.Empty);
    }

    /// <summary>
    /// 检查用户是否可以编辑评论/帖子
    /// </summary>
    /// <param name="authorId">内容作者ID</param>
    /// <param name="currentUserId">当前用户ID</param>
    /// <param name="userRole">当前用户角色</param>
    /// <param name="replyCount">回复数量</param>
    /// <param name="createdAt">创建时间</param>
    /// <param name="editWindowMinutes">编辑窗口（分钟）</param>
    /// <returns>是否可以编辑及错误消息</returns>
    public static (bool CanEdit, string ErrorMessage) CanEditContent(
        int authorId,
        int currentUserId,
        UserRole userRole,
        int replyCount,
        DateTimeOffset createdAt,
        int editWindowMinutes)
    {
        // 规则 1: 管理员可以编辑任何内容
        if (userRole == UserRole.Administrator)
            return (true, string.Empty);

        // 规则 2: 只能编辑自己的内容
        if (authorId != currentUserId)
            return (false, "You are not authorized to edit this content");

        // 规则 3: 有回复的内容不能编辑
        if (replyCount > 0)
            return (false, "Cannot edit content with replies");

        // 规则 4: 超过时间窗口不能编辑
        var elapsed = DateTimeOffset.UtcNow - createdAt;
        if (elapsed.TotalMinutes > editWindowMinutes)
            return (false, $"Edit window expired (limit: {editWindowMinutes} minutes)");

        return (true, string.Empty);
    }
}