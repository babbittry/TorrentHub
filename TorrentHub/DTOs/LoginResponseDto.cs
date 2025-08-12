using TorrentHub.Entities;

namespace TorrentHub.DTOs;

using TorrentHub.Entities;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required User User { get; set; }
}
