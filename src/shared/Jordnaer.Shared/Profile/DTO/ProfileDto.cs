namespace Jordnaer.Shared;

public class ProfileDto
{
	public required string Id { get; set; }

	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public string? UserName { get; set; }

	public string? PhoneNumber { get; set; }

	public string? Address { get; set; }

	public int? ZipCode { get; set; }

	public string? City { get; set; }

	public string? Description { get; set; }

	public List<Category> Categories { get; set; } = [];

	public List<ChildProfileDto> ChildProfiles { get; set; } = [];

	public DateTime? DateOfBirth { get; set; }

	public string ProfilePictureUrl { get; set; } = null!;

	public int? Age { get; set; }

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

	public string DisplayLocation => ZipCode is not null && City is not null
		? $"{ZipCode}, {City}"
		: ZipCode is not null
			? ZipCode.ToString()!
			: City ?? "Ikke angivet";

	public string DisplayName => FirstName is not null
		? $"{FirstName} {LastName}"
		: LastName ?? string.Empty;
}
