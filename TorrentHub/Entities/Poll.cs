
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Entities;

public class Poll
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public required string Question { get; set; }

    [Required]
    public List<string> Options { get; set; } = new();

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }

    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}
