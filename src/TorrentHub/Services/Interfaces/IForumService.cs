using TorrentHub.Core.DTOs;

namespace TorrentHub.Services.Interfaces;

public interface IForumService
{
    Task<List<ForumCategoryDto>> GetCategoriesAsync();
    Task<PaginatedResult<ForumTopicDto>> GetTopicsAsync(int categoryId, int page, int pageSize);
    Task<ForumTopicDetailDto> GetTopicByIdAsync(int topicId, int page, int pageSize);
    Task<ForumTopicDetailDto> CreateTopicAsync(CreateForumTopicDto createTopicDto, int authorId);
    Task<ForumPostDto> CreatePostAsync(int topicId, CreateForumPostDto createPostDto, int authorId);
    Task UpdateTopicAsync(int topicId, UpdateForumTopicDto updateTopicDto, int userId);
    Task UpdatePostAsync(int postId, UpdateForumPostDto updatePostDto, int userId);
    Task DeleteTopicAsync(int topicId, int userId);
    Task DeletePostAsync(int postId, int userId);
    Task LockTopicAsync(int topicId);
    Task UnlockTopicAsync(int topicId);
    Task PinTopicAsync(int topicId);
    Task UnpinTopicAsync(int topicId);
}

