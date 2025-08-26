
using Microsoft.AspNetCore.Mvc;

namespace TorrentHub.DTOs;

public class LogSearchDto
{
    [FromQuery(Name = "q")]
    public string? Query { get; set; }

    [FromQuery(Name = "level")]
    public string? Level { get; set; }

    [FromQuery(Name = "offset")]
    public int Offset { get; set; } = 0;

    [FromQuery(Name = "limit")]
    public int Limit { get; set; } = 100;
}
