
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs
{
    public class UpdateUserProfileDto
    {
        public string? AvatarUrl { get; set; }
        public string? Signature { get; set; }

        [RegularExpression("^(en|fr|ja|zh-CN)$", ErrorMessage = "Invalid language code. Allowed values are en, fr, ja, zh-CN.")]
        public string? Language { get; set; }
    }
}

