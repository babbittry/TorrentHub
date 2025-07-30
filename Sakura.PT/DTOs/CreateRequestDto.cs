namespace Sakura.PT.DTOs;

public class CreateRequestDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public long InitialBounty { get; set; } = 0;
}
