namespace Jordnaer.Shared;

public class GroupDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public required string ShortDescription { get; set; }
    public string? Description { get; set; }

    public string? Address { get; set; }
    public int? ZipCode { get; set; }
    public string? City { get; set; }

    public int MemberCount { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<string> Categories { get; set; } = new();
}
