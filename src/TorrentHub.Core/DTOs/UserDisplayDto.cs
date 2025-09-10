namespace TorrentHub.Core.DTOs;

/// <summary>
/// A lightweight DTO for displaying user information in public contexts like forums, comments, etc.
/// It contains all necessary data for rendering a user's name with their level, color, badge, and signature.
/// </summary>
public class UserDisplayDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? UserLevelName { get; set; }
    public string? UserLevelColor { get; set; }
    public BadgeDto? EquippedBadge { get; set; }
    public string? ShortSignature { get; set; }
    public bool IsColorfulUsernameActive { get; set; }
}