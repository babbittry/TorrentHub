
namespace TorrentHub.Core.DTOs;

public class DuplicateIpUserDto
{
    public required string Ip { get; set; }
    public required List<UserSummaryDto> Users { get; set; }
}

public class UserSummaryDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
}
