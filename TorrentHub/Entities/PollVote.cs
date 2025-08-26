
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TorrentHub.Entities;

[Index(nameof(PollId), nameof(UserId), IsUnique = true)]
public class PollVote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [ForeignKey(nameof(PollId))]
    public Poll? Poll { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [StringLength(255)]
    public required string SelectedOption { get; set; }

    [Required]
    public DateTimeOffset VotedAt { get; set; } = DateTimeOffset.UtcNow;
}
