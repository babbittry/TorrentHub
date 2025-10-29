namespace TorrentHub.Core.DTOs
{
    public class RssFeedTokenDto
    {
        public int Id { get; set; }
        public Guid Token { get; set; }
        public string FeedType { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string[]? CategoryFilter { get; set; }
        public int MaxResults { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public int UsageCount { get; set; }
        public string? UserAgent { get; set; }
        public string? LastIp { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
    }

    public class CreateRssFeedTokenRequestDto
    {
        public string FeedType { get; set; } = "Latest";
        public string? Name { get; set; }
        public string[]? CategoryFilter { get; set; }
        public int MaxResults { get; set; } = 50;
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public class RssFeedTokenResponseDto
    {
        public RssFeedTokenDto Token { get; set; } = null!;
        public string RssUrl { get; set; } = string.Empty;
    }
}