using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.Entities;

public class Invite
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}