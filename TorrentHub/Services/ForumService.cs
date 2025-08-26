using Microsoft.EntityFrameworkCore;
using TorrentHub.Data;
using TorrentHub.DTOs;
using Microsoft.Extensions.Logging;
using TorrentHub.Entities;
using TorrentHub.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorrentHub.Services;

public class ForumService : IForumService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForumService> _logger;

    public ForumService(ApplicationDbContext context, ILogger<ForumService> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<List<ForumTopicDto>> GetTopicsAsync(int categoryId)
    {
        return await _context.ForumTopics
            .Include(t => t.Author)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.IsSticky)
            .ThenByDescending(t => t.LastPostTime)
            .Select(t => new ForumTopicDto
            {
                Id = t.Id,
                Title = t.Title,
                AuthorId = t.AuthorId,
                AuthorName = t.Author!.UserName,
                CreatedAt = t.CreatedAt,
                LastPostTime = t.LastPostTime ?? t.CreatedAt,
                PostCount = t.Posts.Count,
                IsLocked = t.IsLocked,
                IsSticky = t.IsSticky
            })
            .ToListAsync();
    }

    public async Task<ForumTopicDetailDto> GetTopicByIdAsync(int topicId)
    {
        var topic = await _context.ForumTopics
            .Include(t => t.Author)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Author)
            .Where(t => t.Id == topicId)
            .Select(t => new ForumTopicDetailDto
            {
                Id = t.Id,
                Title = t.Title,
                AuthorId = t.AuthorId,
                AuthorName = t.Author!.UserName,
                AuthorAvatar = t.Author!.Avatar,
                CreatedAt = t.CreatedAt,
                IsLocked = t.IsLocked,
                IsSticky = t.IsSticky,
                Posts = t.Posts.OrderBy(p => p.CreatedAt).Select(p => new ForumPostDto
                {
                    Id = p.Id,
                    AuthorId = p.AuthorId,
                    AuthorName = p.Author!.UserName,
                    AuthorAvatar = p.Author!.Avatar,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    EditedAt = p.EditedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        return topic;
    }

    public async Task<ForumTopicDetailDto> CreateTopicAsync(CreateForumTopicDto createTopicDto, int authorId)
    {
        var user = await _context.Users.FindAsync(authorId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
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
            Topic = topic
        };

        topic.Posts.Add(post);

        _context.ForumTopics.Add(topic);
        await _context.SaveChangesAsync();

        return new ForumTopicDetailDto
        {
            Id = topic.Id,
            Title = topic.Title,
            AuthorId = topic.AuthorId,
            AuthorName = user.UserName,
            CreatedAt = topic.CreatedAt,
            IsLocked = topic.IsLocked,
            IsSticky = topic.IsSticky,
            Posts = new List<ForumPostDto>
            {
                new()
                {
                    Id = post.Id,
                    AuthorId = post.AuthorId,
                    AuthorName = user.UserName,
                    Content = post.Content,
                    CreatedAt = post.CreatedAt,
                    EditedAt = post.EditedAt
                }
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

        var post = new ForumPost
        {
            TopicId = topicId,
            AuthorId = authorId,
            Content = createPostDto.Content,
            CreatedAt = now
        };

        topic.LastPostTime = now;

        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        return new ForumPostDto
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorName = user.UserName,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            EditedAt = post.EditedAt
        };
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
}