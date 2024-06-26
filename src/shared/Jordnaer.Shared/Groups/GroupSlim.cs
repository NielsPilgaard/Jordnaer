namespace Jordnaer.Shared;

public class GroupSlim
{
	public Guid Id { get; set; }

	public required string Name { get; set; }

	public string? ProfilePictureUrl { get; set; }

	public required string ShortDescription { get; set; }
	public required string? Description { get; set; }

	public int? ZipCode { get; set; }
	public string? City { get; set; }

	public int MemberCount { get; set; }

	public required string[] Categories { get; set; }
}
