using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Entities;

public class SiteSetting
{
    [Key]
    [StringLength(100)]
    public required string Key { get; set; }

    [Required]
    public required string Value { get; set; }
}
