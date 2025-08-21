using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateRegistrationSettingsDto
{
    [Required]
    public bool IsOpen { get; set; }
}
