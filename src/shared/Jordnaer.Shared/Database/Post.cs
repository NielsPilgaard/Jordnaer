using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(ZipCode))]
public class Post
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public required Guid Id { get; init; }

	[StringLength(1000, ErrorMessage = "Opslag må højest være 1000 karakterer lang.")]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Opslag skal have mindst 1 karakter.")]
	public required string Text { get; set; }

	public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

	public int? ZipCode { get; set; }

	public string? City { get; set; }

	[ForeignKey(nameof(UserProfile))]
	public required string UserProfileId { get; set; } = null!;

	public UserProfile UserProfile { get; init; } = null!;

	public List<Category> Categories { get; set; } = [];
}