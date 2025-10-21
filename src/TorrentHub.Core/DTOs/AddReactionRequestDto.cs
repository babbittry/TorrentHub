using System.ComponentModel.DataAnnotations;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class AddReactionRequestDto
{
    [Required]
    public ReactionType Type { get; set; }
}