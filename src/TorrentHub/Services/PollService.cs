using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Services;

public class PollService : IPollService
{
    private readonly ApplicationDbContext _context;

    public PollService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PollDto> CreateAsync(CreatePollDto dto, int createdByUserId)
    {
        var poll = new Poll
        {
            Question = dto.Question,
            Options = dto.Options,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Polls.Add(poll);
        await _context.SaveChangesAsync();

        return ToPollDto(poll, null);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int pollId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null)
        {
            return (false, "error.poll.notFound");
        }

        _context.Polls.Remove(poll);
        await _context.SaveChangesAsync();
        return (true, "poll.delete.success");
    }

    public async Task<List<PollDto>> GetAllAsync(int? userId)
    {
        var polls = await _context.Polls.Include(p => p.Votes).OrderByDescending(p => p.CreatedAt).ToListAsync();
        return polls.Select(p => ToPollDto(p, userId)).ToList();
    }

    public async Task<PollDto?> GetByIdAsync(int pollId, int? userId)
    {
        var poll = await _context.Polls.Include(p => p.Votes).FirstOrDefaultAsync(p => p.Id == pollId);
        return poll == null ? null : ToPollDto(poll, userId);
    }

    public async Task<PollDto?> GetLatestActiveAsync(int? userId)
    {
        var poll = await _context.Polls
            .Include(p => p.Votes)
            .Where(p => p.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
        
        return poll == null ? null : ToPollDto(poll, userId);
    }

    public async Task<(bool Success, string Message)> VoteAsync(int pollId, VoteDto dto, int userId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null)
        {
            return (false, "error.poll.notFound");
        }

        if (poll.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return (false, "error.poll.expired");
        }

        if (!poll.Options.Contains(dto.Option))
        {
            return (false, "error.poll.invalidOption");
        }

        var hasVoted = await _context.PollVotes.AnyAsync(v => v.PollId == pollId && v.UserId == userId);
        if (hasVoted)
        {
            return (false, "error.poll.alreadyVoted");
        }

        var vote = new PollVote
        {
            PollId = pollId,
            UserId = userId,
            SelectedOption = dto.Option,
            VotedAt = DateTimeOffset.UtcNow
        };

        _context.PollVotes.Add(vote);
        await _context.SaveChangesAsync();

        return (true, "poll.vote.success");
    }

    private static PollDto ToPollDto(Poll poll, int? userId)
    {
        var results = poll.Options.ToDictionary(option => option, option => 0);
        foreach (var vote in poll.Votes)
        {
            if (results.ContainsKey(vote.SelectedOption))
            {
                results[vote.SelectedOption]++;
            }
        }

        var userVote = userId.HasValue ? poll.Votes.FirstOrDefault(v => v.UserId == userId.Value) : null;

        return new PollDto
        {
            Id = poll.Id,
            Question = poll.Question,
            Results = results,
            ExpiresAt = poll.ExpiresAt,
            TotalVotes = poll.Votes.Count,
            UserVotedOption = userVote?.SelectedOption
        };
    }
}

