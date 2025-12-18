using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class GroupPostDto
{
	public required Guid Id { get; init; }

	[StringLength(4000, ErrorMessage = "Opslag må højest være 4000 karakterer lang.")]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Opslag skal have mindst 1 karakter.")]
	public required string Text { get; init; }

	public DateTimeOffset CreatedUtc { get; init; }

	public required UserSlim Author { get; init; }
}
    