using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class ForumService : IForumService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForumService> _logger;
    private readonly IUserLevelService _userLevelService;

    public ForumService(ApplicationDbContext context, ILogger<ForumService> logger, IUserLevelService userLevelService)
    {
        _context = context;
        _logger = logger;
        _userLevelService = userLevelService;
    }

    public async Task<List<ForumCategoryDto>> GetCategoriesAsync()
    {
        return await _context.ForumCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ForumCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                DisplayOrder = c.DisplayOrder,
                TopicCount = c.Topics.Count(),
                PostCount = c.Topics.SelectMany(t => t.Posts).Count()
            })
            .ToListAsync();
    }

    public async Task<PaginatedResult<ForumTopicDto>> GetTopicsAsync(int categoryId, int page, int pageSize)
    {
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

        var postsQuery = _context.ForumPosts
            .Where(p => p.TopicId == topicId)
            .OrderBy(p => p.Floor);

        var totalPosts = await postsQuery.CountAsync();

        var posts = await postsQuery
            .Include(p => p.Author) // Eager load author for posts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var postAuthorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var postAuthors = await GetUsersDisplayInfoAsync(postAuthorIds);

        var postDtos = posts.Select(p =>
        {
            var dto = Mappers.Mapper.ToForumPostDto(p);
            dto.Author = postAuthors.GetValueOrDefault(p.AuthorId);
            return dto;
        }).ToList();

        topicDetailDto.Posts = new PaginatedResult<ForumPostDto>
        {
            Items = postDtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalPosts,
            TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize)
        };

        return topicDetailDto;
    }

    public async Task<ForumTopicDetailDto> CreateTopicAsync(CreateForumTopicDto createTopicDto, int authorId)
    {
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
            LastPostTime = now,
            IsLocked = false,
            IsSticky = false
        };

        var post = new ForumPost
        {
            AuthorId = authorId,
            Content = createTopicDto.Content,
            CreatedAt = now,
            Topic = topic,
            Floor = 1
        };

        topic.Posts.Add(post);

        _context.ForumTopics.Add(topic);
        await _context.SaveChangesAsync();

        var authorDto = await MapToUserDisplayDtoAsync(user);
        var postDto = Mappers.Mapper.ToForumPostDto(post);
        postDto.Author = authorDto;

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

    public async Task<ForumPostDto> CreatePostAsync(int topicId, CreateForumPostDto createPostDto, int authorId)
    {
        var user = await _context.Users.FindAsync(authorId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (user.BanStatus.HasFlag(BanStatus.ForumBan) || user.BanStatus.HasFlag(BanStatus.LoginBan))
        {
            throw new UnauthorizedAccessException("You are banned from using the forum.");
        }

        var topic = await _context.ForumTopics.FindAsync(topicId);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        if (topic.IsLocked)
        {
            throw new InvalidOperationException("Topic is locked");
        }

        var now = DateTimeOffset.UtcNow;

        var floor = await _context.ForumPosts.CountAsync(p => p.TopicId == topicId) + 1;

        var post = new ForumPost
        {
            TopicId = topicId,
            AuthorId = authorId,
            Content = createPostDto.Content,
            CreatedAt = now,
            Floor = floor
        };

        topic.LastPostTime = now;

        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var postDto = Mappers.Mapper.ToForumPostDto(post);
        postDto.Author = await MapToUserDisplayDtoAsync(user);
        return postDto;
    }

    public async Task UpdateTopicAsync(int topicId, UpdateForumTopicDto updateTopicDto, int userId)
    {
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
    }

    public async Task UpdatePostAsync(int postId, UpdateForumPostDto updatePostDto, int userId)
    {
        var post = await _context.ForumPosts.FindAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException("Post not found");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (post.AuthorId != userId && user.Role < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this post.");
        }

        post.Content = updatePostDto.Content;
        post.EditedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTopicAsync(int topicId, int userId)
    {
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
            throw new UnauthorizedAccessException("You are not authorized to delete this topic.");
        }

        _context.ForumTopics.Remove(topic);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int postId, int userId)
    {
        var post = await _context.ForumPosts
            .Include(p => p.Topic)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new KeyNotFoundException("Post not found");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (post.AuthorId != userId && user.Role < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this post.");
        }

        var topic = post.Topic;
        if (topic == null)
        {
            throw new InvalidOperationException($"Post with ID {postId} is not associated with any topic.");
        }
        
        var firstPostInTopic = await _context.ForumPosts.OrderBy(p => p.CreatedAt).FirstAsync(p => p.TopicId == topic.Id);

        if (firstPostInTopic.Id == post.Id)
        {
            if (topic.AuthorId != userId && user.Role < UserRole.Moderator)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this topic.");
            }
            _context.ForumTopics.Remove(topic);
        }
        else
        {
            _context.ForumPosts.Remove(post);

            var wasLastPost = topic.LastPostTime <= post.CreatedAt;
            if (wasLastPost)
            {
                var newLastPost = await _context.ForumPosts
                    .Where(p => p.TopicId == topic.Id && p.Id != postId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
                
                topic.LastPostTime = newLastPost?.CreatedAt ?? topic.CreatedAt;
            }
        }

        await _context.SaveChangesAsync();
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
            // No Include needed here as we fetch badges separately
            .ToListAsync();

        var badgeIds = users.Where(u => u.EquippedBadgeId.HasValue).Select(u => u.EquippedBadgeId!.Value).Distinct().ToList();
        var badges = await _context.Badges.Where(b => badgeIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id);

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
