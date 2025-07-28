using Sakura.PT.Enums;

namespace Sakura.PT.DTOs;

public class UserDto
{
   public int Id { get; set; }
   public string Username { get; set; }
   public long Uploaded { get; set; }
   public long Downloaded { get; set; }
   public UserRole Role { get; set; }
}