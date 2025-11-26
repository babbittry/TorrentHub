
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs
{
    public class PurchaseItemRequestDto
    {
        [Required]
        public int StoreItemId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public Dictionary<string, object>? Params { get; set; }
    }
}
