using TorrentHub.Core.Entities;

namespace TorrentHub.Core.DTOs;

using TorrentHub.Core.Entities;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required User User { get; set; }
}

