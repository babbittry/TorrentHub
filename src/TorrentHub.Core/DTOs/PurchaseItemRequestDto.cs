
using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs
{
    public class PurchaseItemRequestDto
    {
        [Required]
        public int StoreItemId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
