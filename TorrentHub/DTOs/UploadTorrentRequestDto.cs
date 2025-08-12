using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class UploadTorrentRequestDto
{
    [Required]
    public required IFormFile File { get; set; }

    [StringLength(4096)]
    public string? Description { get; set; }

    [Required]
    public TorrentCategory Category { get; set; }
    
    [StringLength(15)]
    public string? ImdbId { get; set; }
}