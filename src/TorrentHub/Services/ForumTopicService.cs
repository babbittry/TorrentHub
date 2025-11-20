using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;

namespace TorrentHub.Services;

public class ForumTopicService : IForumTopicService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForumTopicService> _logger;
    private readonly IUserLevelService _userLevelService;
    private readonly ICommentService _commentService;
    private readonly ISettingsService _settingsService;

    public ForumTopicService(
        ApplicationDbContext context,
        ILogger<ForumTopicService> logger,
        IUserLevelService userLevelService,
        ICommentService commentService,
        ISettingsService settingsService)
    {
        _context = context;
        _logger = logger;
        _userLevelService = userLevelService;
        _commentService = commentService;
        _settingsService = settingsService;
    }

    private async Task EnsureForumEnabledAsync()
    {
        var settings = await _settingsService.GetSiteSettingsAsync();
        if (!settings.IsForumEnabled)
        {
            throw new InvalidOperationException("Forum is currently disabled.");
        }
    }

    public async Task<List<ForumCategoryDto>> GetCategoriesAsync()
    {
        await EnsureForumEnabledAsync();
        
        var categories = await _context.ForumCategories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        
        var result = new List<ForumCategoryDto>();
        
        foreach (var category in categories)
        {
            // 统计该分类下的主题数
            var topicCount = await _context.ForumTopics
                .CountAsync(t => t.CategoryId == category.Id);
            
            // 统计该分类下的帖子数(评论数) - 使用新的 Comments 表
            var topicIds = await _context.ForumTopics
                .Where(t => t.CategoryId == category.Id)
                .Select(t => t.Id)
                .ToListAsync();
            
            var postCount = await _context.Comments
                .Where(c => c.CommentableType == CommentableType.ForumTopic &&
                           topicIds.Contains(c.CommentableId))
                .CountAsync();
            
            result.Add(new ForumCategoryDto
            {
                Id = category.Id,
                Code = category.Code,
                DisplayOrder = category.DisplayOrder,
                TopicCount = topicCount,
                PostCount = postCount
            });
        }
        
        return result;
    }

    public async Task<PaginatedResult<ForumTopicDto>> GetTopicsAsync(int categoryId, int page, int pageSize)
    {
        await EnsureForumEnabledAsync();
        
        var query = _context.ForumTopics
            .Include(t => t.Author)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.IsSticky)
            .ThenByDescending(t => t.LastPostTime);

        var totalItems = await query.CountAsync();

        var topics = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var authorIds = topics.Select(t => t.AuthorId).Distinct().ToList();
        var authors = await GetUsersDisplayInfoAsync(authorIds);

        var topicDtos = topics.Select(t =>
        {
            var dto = Mappers.Mapper.ToForumTopicDto(t);
            dto.Author = authors.GetValueOrDefault(t.AuthorId);
            return dto;
        }).ToList();

        return new PaginatedResult<ForumTopicDto>
        {
            Items = topicDtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<ForumTopicDetailDto> GetTopicByIdAsync(int topicId, int page, int pageSize)
    {
        await EnsureForumEnabledAsync();
        
        var topicEntity = await _context.ForumTopics
            .Include(t => t.Author)
            .Include(t => t.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == topicId);

        if (topicEntity == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        var topicAuthorDto = await MapToUserDisplayDtoAsync(topicEntity.Author!);

        var topicDetailDto = new ForumTopicDetailDto
        {
            Id = topicEntity.Id,
            Title = topicEntity.Title,
            Author = topicAuthorDto,
            CreatedAt = topicEntity.CreatedAt,
            IsLocked = topicEntity.IsLocked,
            IsSticky = topicEntity.IsSticky,
            CategoryId = topicEntity.CategoryId,
            CategoryName = topicEntity.Category!.Code.ToString()
        };

        // 使用新的统一评论系统获取帖子
        var comments = await _commentService.GetCommentsLazyAsync(
            CommentableType.ForumTopic, 
            topicId, 
            afterFloor: (page - 1) * pageSize,
            limit: pageSize);

        // 转换为 ForumPostDto 以保持API兼容性
        var postDtos = comments.Items.Select(c => new ForumPostDto
        {
            Id = c.Id,
            Content = c.Content,
            Author = c.User,
            CreatedAt = c.CreatedAt,
            EditedAt = c.EditedAt,
            Floor = c.Floor,
            ParentPostId = c.ParentCommentId,
            ReplyToUser = c.ReplyToUser,
            Depth = c.Depth,
            ReplyCount = c.ReplyCount,
            Reactions = c.Reactions
        }).ToList();

        topicDetailDto.Posts = new PaginatedResult<ForumPostDto>
        {
            Items = postDtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = comments.TotalCount,
            TotalPages = (int)Math.Ceiling(comments.TotalCount / (double)pageSize)
        };

        return topicDetailDto;
    }

    public async Task<ForumTopicDetailDto> CreateTopicAsync(CreateForumTopicDto createTopicDto, int authorId)
    {
        await EnsureForumEnabledAsync();
        
        var user = await _context.Users.FindAsync(authorId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var category = await _context.ForumCategories.FindAsync(createTopicDto.CategoryId);
        if (category == null)
        {
            throw new KeyNotFoundException("Category not found");
        }

        if (category.Code == ForumCategoryCode.Announcement && user.Role != UserRole.Administrator)
        {
            throw new UnauthorizedAccessException("Only administrators can create topics in the Announcements category.");
        }

        var now = DateTimeOffset.UtcNow;

        var topic = new ForumTopic
        {
            Title = createTopicDto.Title,
            AuthorId = authorId,
            CategoryId = createTopicDto.CategoryId,
            CreatedAt = now,
            LastPostTime = now.UtcDateTime,
            IsLocked = false,
            IsSticky = false
        };

        _context.ForumTopics.Add(topic);
        await _context.SaveChangesAsync();

        // 使用新的统一评论系统创建第一条帖子
        var (success, message, comment) = await _commentService.PostCommentAsync(
            CommentableType.ForumTopic,
            topic.Id,
            new CreateCommentDto { Content = createTopicDto.Content },
            authorId);

        if (!success || comment == null)
        {
            // 如果评论创建失败,回滚主题创建
            _context.ForumTopics.Remove(topic);
            await _context.SaveChangesAsync();
            throw new InvalidOperationException($"Failed to create initial post: {message}");
        }

        var authorDto = await MapToUserDisplayDtoAsync(user);
        
        // 转换为 ForumPostDto 以保持API兼容性
        var postDto = new ForumPostDto
        {
            Id = comment.Id,
            Content = comment.Content,
            Author = comment.User,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            Floor = comment.Floor,
            ParentPostId = comment.ParentCommentId,
            ReplyToUser = comment.ReplyToUser,
            Depth = comment.Depth,
            ReplyCount = comment.ReplyCount
        };

        _logger.LogInformation(
            "ForumTopic {TopicId} created by user {UserId} with initial comment {CommentId}",
            topic.Id, authorId, comment.Id);

        return new ForumTopicDetailDto
        {
            Id = topic.Id,
            Title = topic.Title,
            Author = authorDto,
            CreatedAt = topic.CreatedAt,
            IsLocked = topic.IsLocked,
            IsSticky = topic.IsSticky,
            CategoryId = topic.CategoryId,
            CategoryName = category.Code.ToString(),
            Posts = new PaginatedResult<ForumPostDto>
            {
                Items = new List<ForumPostDto> { postDto },
                Page = 1,
                PageSize = 1,
                TotalItems = 1,
                TotalPages = 1
            }
        };
    }

    public async Task UpdateTopicAsync(int topicId, UpdateForumTopicDto updateTopicDto, int userId)
    {
        await EnsureForumEnabledAsync();
        
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (topic.AuthorId != userId && user.Role < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this topic.");
        }

        topic.Title = updateTopicDto.Title;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "ForumTopic {TopicId} updated by user {UserId} (Role: {Role})",
            topicId, userId, user.Role);
    }

    public async Task DeleteTopicAsync(int topicId, int userId)
    {
        await EnsureForumEnabledAsync();
        
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // 只有版主及以上可以删除主题
        if (topic.AuthorId != userId && user.Role < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this topic");
        }

        // 检查是否有评论
        var hasComments = await _context.Comments
            .AnyAsync(c => c.CommentableType == CommentableType.ForumTopic && c.CommentableId == topicId);

        if (hasComments)
        {
            throw new InvalidOperationException("Cannot delete topic with posts. Please delete all posts first or contact an administrator.");
        }

        _context.ForumTopics.Remove(topic);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "ForumTopic {TopicId} deleted by user {UserId} (Role: {Role})",
            topicId, userId, user.Role);
    }

    public async Task LockTopicAsync(int topicId)
    {
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }
        topic.IsLocked = true;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("ForumTopic {TopicId} locked", topicId);
    }

    public async Task UnlockTopicAsync(int topicId)
    {
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }
        topic.IsLocked = false;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("ForumTopic {TopicId} unlocked", topicId);
    }

    public async Task PinTopicAsync(int topicId)
    {
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }
        topic.IsSticky = true;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("ForumTopic {TopicId} pinned", topicId);
    }

    public async Task UnpinTopicAsync(int topicId)
    {
        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }
        topic.IsSticky = false;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("ForumTopic {TopicId} unpinned", topicId);
    }

    private async Task<UserDisplayDto> MapToUserDisplayDtoAsync(User user)
    {
        var dto = Mappers.Mapper.ToUserDisplayDto(user);
        var level = _userLevelService.GetUserLevel(user);
        dto.UserLevelName = level.Name;
        dto.UserLevelColor = level.Color;

        if (user.EquippedBadgeId.HasValue)
        {
            var badge = await _context.Badges.FindAsync(user.EquippedBadgeId.Value);
            if (badge != null)
            {
                dto.EquippedBadge = new BadgeDto { Id = badge.Id, Code = badge.Code };
            }
        }

        return dto;
    }

    private async Task<Dictionary<int, UserDisplayDto>> GetUsersDisplayInfoAsync(List<int> userIds)
    {
        if (!userIds.Any())
        {
            return new Dictionary<int, UserDisplayDto>();
        }

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var badgeIds = users.Where(u => u.EquippedBadgeId.HasValue)
            .Select(u => u.EquippedBadgeId!.Value).Distinct().ToList();
        var badges = await _context.Badges
            .Where(b => badgeIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        var result = new Dictionary<int, UserDisplayDto>();
        foreach (var user in users)
        {
            var dto = Mappers.Mapper.ToUserDisplayDto(user);
            var level = _userLevelService.GetUserLevel(user);
            dto.UserLevelName = level.Name;
            dto.UserLevelColor = level.Color;

            if (user.EquippedBadgeId.HasValue && badges.TryGetValue(user.EquippedBadgeId.Value, out var badge))
            {
                dto.EquippedBadge = new BadgeDto { Id = badge.Id, Code = badge.Code };
            }
            result[user.Id] = dto;
        }
        return result;
    }
}