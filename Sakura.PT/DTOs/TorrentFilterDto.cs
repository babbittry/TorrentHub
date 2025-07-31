namespace Sakura.PT.DTOs;

public class TorrentFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50; // Default to 50 items per page
    public int? Category { get; set; } // Corresponds to TorrentCategory enum value
    public string? SearchTerm { get; set; } // For searching by name or description
    public string SortBy { get; set; } = "CreatedAt"; // Default sort by creation date
    public string SortOrder { get; set; } = "desc"; // Default sort order descending
}
