using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Jordnaer.Shared;

[Index(nameof(ZipCode))]
[Index(nameof(UserName), IsUnique = true)]
[Index(nameof(SearchableName))]
public class UserProfile
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public required string Id { get; set; }

	[MaxLength(100, ErrorMessage = "Brugernavn må højest være 100 karakterer langt.")]
	[MinLength(3, ErrorMessage = "Brugernavn skal være mindst 3 karakterer langt.")]
	[RegularExpression("^[\\w@-_\\.^][^\\\\]+$",
		ErrorMessage = "Brugernavn må kun bestå af bogstaver, tal, og udvalgte tegn.")]
	public string? UserName { get; set; }

	[MaxLength(100, ErrorMessage = "Fornavn må højest være 100 karakterer langt.")]
	public string? FirstName { get; set; }

	[MaxLength(250, ErrorMessage = "Efternavn må højest være 250 karakterer langt.")]
	public string? LastName { get; set; }

	public string? SearchableName { get; set; }

	[Phone(ErrorMessage = "Telefon nummeret må kun indeholde tal, mellemrum og +")]
	public string? PhoneNumber { get; set; }

	[MaxLength(500, ErrorMessage = "Adresse må højest være 500 karakterer langt.")]
	public string? Address { get; set; }

	[DanishZipCode(ErrorMessage = "Post nummer skal være mellem 1000 og 9999")]
	public int? ZipCode { get; set; }

	[MaxLength(100, ErrorMessage = "By må højest være 50 karakterer langt.")]
	public string? City { get; set; }

	public Point? Location { get; set; }

	[MaxLength(2000, ErrorMessage = "Beskrivelse må højest være 2000 karakterer langt.")]
	public string? Description { get; set; }

	public List<Category> Categories { get; set; } = [];

	public List<ChildProfile> ChildProfiles { get; set; } = [];

	public List<UserProfile> Contacts { get; set; } = [];

	public List<Group> Groups { get; set; } = [];
	public List<GroupMembership> GroupMemberships { get; set; } = [];

	public DateTime? DateOfBirth { get; set; }

	public string ProfilePictureUrl { get; set; } = ProfileConstants.Default_Profile_Picture;

	public int? Age { get; set; }

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

	[NotMapped]
	public string DisplayName => FirstName is not null
		? $"{FirstName} {LastName}"
		: LastName ?? string.Empty;
}
