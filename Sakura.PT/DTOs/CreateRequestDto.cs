namespace Sakura.PT.DTOs;

public class CreateRequestDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ulong InitialBounty { get; set; } = 0UL;
}
