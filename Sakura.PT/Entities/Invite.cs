using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sakura.PT.Entities;

public class Invite
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public required string Code { get; set; }
    [Required]
    public int GeneratorUserId { get; set; }
    [ForeignKey(nameof(GeneratorUserId))]
    public User GeneratorUser { get; set; }
    public int? UsedByUserId { get; set; }
    [ForeignKey(nameof(UsedByUserId))]
    public User? UsedByUser { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}