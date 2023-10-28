namespace Jordnaer.Shared;

public class GroupDto
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<string> Categories { get; set; } = new();
}
