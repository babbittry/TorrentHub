using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UpdateRegistrationSettingsDto
{
    [Required]
    public bool IsOpen { get; set; }
}
