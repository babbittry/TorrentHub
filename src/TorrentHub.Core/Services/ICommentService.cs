using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Services;

/// <summary>
/// 统一的评论服务接口 (支持种子、求种、论坛等多种类型)
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// 创建评论
    /// </summary>
    /// <param name="commentableType">评论目标类型</param>
    /// <param name="commentableId">评论目标ID</param>
    /// <param name="request">评论请求</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果和创建的评论DTO</returns>
    Task<(bool Success, string Message, CommentDto? Comment)> PostCommentAsync(
        CommentableType commentableType,
        int commentableId,
        CreateCommentDto request,
        int userId);

    /// <summary>
    /// 更新评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果和更新后的评论DTO</returns>
    Task<(bool Success, string Message, CommentDto? Comment)> UpdateCommentAsync(
        int commentId,
        UpdateCommentDto request,
        int userId);

    /// <summary>
    /// 删除评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId);

    /// <summary>
    /// 懒加载获取评论列表
    /// </summary>
    /// <param name="commentableType">评论目标类型</param>
    /// <param name="commentableId">评论目标ID</param>
    /// <param name="afterFloor">在此楼层之后</param>
    /// <param name="limit">限制数量</param>
    /// <returns>评论列表响应</returns>
    Task<CommentListResponse> GetCommentsLazyAsync(
        CommentableType commentableType, 
        int commentableId, 
        int afterFloor = 0, 
        int limit = 30);

    /// <summary>
    /// 获取单个评论详情
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <returns>评论详情</returns>
    Task<CommentDto?> GetCommentByIdAsync(int commentId);

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>用户评论列表</returns>
    Task<CommentListResponse> GetUserCommentsAsync(int userId, int page = 1, int pageSize = 30);
}