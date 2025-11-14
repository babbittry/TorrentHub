using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Services;

public interface IForumTopicService
{
    // 分类管理
    Task<List<ForumCategoryDto>> GetCategoriesAsync();
    
    // 主题 CRUD
    Task<PaginatedResult<ForumTopicDto>> GetTopicsAsync(int categoryId, int page, int pageSize);
    Task<ForumTopicDetailDto> GetTopicByIdAsync(int topicId, int page, int pageSize);
    Task<ForumTopicDetailDto> CreateTopicAsync(CreateForumTopicDto createTopicDto, int authorId);
    Task UpdateTopicAsync(int topicId, UpdateForumTopicDto updateTopicDto, int userId);
    Task DeleteTopicAsync(int topicId, int userId);
    
    // 主题管理
    Task LockTopicAsync(int topicId);
    Task UnlockTopicAsync(int topicId);
    Task PinTopicAsync(int topicId);
    Task UnpinTopicAsync(int topicId);
}