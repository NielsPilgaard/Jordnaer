using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared;

public class Post
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public required Guid Id { get; init; }

	[StringLength(1000, ErrorMessage = "Opslag må højest være 1000 karakterer lang.")]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Opslag skal have mindst 1 karakter.")]
	public required string Text { get; init; }

	public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

	public int? ZipCode { get; set; }
	public string? City { get; set; }

	[ForeignKey(nameof(UserProfile))]
	public required string UserProfileId { get; init; } = null!;

	public UserProfile UserProfile { get; init; } = null!;

	public List<Category> Categories { get; set; } = [];
}