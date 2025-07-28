namespace Sakura.PT.DTOs;

public class TorrentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public long Size { get; set; }
    public string UploaderUsername { get; set; } 
    public DateTime CreatedAt { get; set; }
}