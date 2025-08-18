using TorrentHub.DTOs;

namespace TorrentHub.Services;

public interface IForumService
{
    Task<List<ForumCategoryDto>> GetCategoriesAsync();
    Task<List<ForumTopicDto>> GetTopicsAsync(int categoryId);
    Task<ForumTopicDetailDto> GetTopicByIdAsync(int topicId);
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
