using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class GroupPost
{

	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public required Guid Id { get; init; }

	[StringLength(4000, ErrorMessage = "Opslag må højest være 4000 karakterer lang.")]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Opslag skal have mindst 1 karakter.")]
	public required string Text { get; init; }

	public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

	[ForeignKey(nameof(UserProfile))]
	public required string UserProfileId { get; init; } = null!;

	public UserProfile UserProfile { get; init; } = null!;

	[ForeignKey(nameof(Group))]
	public required Guid GroupId { get; init; }

	public Group Group { get; init; } = null!;
}