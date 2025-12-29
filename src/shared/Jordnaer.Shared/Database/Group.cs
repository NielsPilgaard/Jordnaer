using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Jordnaer.Shared;

[Index(nameof(Name), IsUnique = true)]
public class Group
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	[Url]
	public string? ProfilePictureUrl { get; set; }

	[MaxLength(500, ErrorMessage = "Adresse må højest være 500 karakterer langt.")]
	public string? Address { get; set; }

	[DanishZipCode(ErrorMessage = "Post nummer skal være mellem 1000 og 9999")]
	public int? ZipCode { get; set; }

	[MaxLength(100, ErrorMessage = "By navn må højest være 100 karakterer langt.")]
	public string? City { get; set; }

	public Point? Location { get; set; }

	/// <summary>
	/// The center coordinates of the zip code area.
	/// Used to show approximate location to non-members when a full address is provided.
	/// </summary>
	public Point? ZipCodeLocation { get; set; }

	[MinLength(2, ErrorMessage = "Gruppe navn skal være mindst 2 karakterer langt.")]
	[MaxLength(128, ErrorMessage = "Gruppens navn må højest være 128 karakterer lang.")]
	public required string Name { get; set; }

	[Required(ErrorMessage = "Påkrævet.")]
	[MaxLength(200, ErrorMessage = "Gruppens beskrivelse må højest være 200 karakterer lang.")]
	public required string ShortDescription { get; set; }

	[MaxLength(4000, ErrorMessage = "Gruppens beskrivelse må højest være 4000 karakterer lang.")]
	public string? Description { get; set; }

	public List<UserProfile> Members { get; set; } = [];
	public List<GroupMembership> Memberships { get; set; } = [];
	public List<Category> Categories { get; set; } = [];

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
