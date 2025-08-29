using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class PostDto
{
	public required Guid Id { get; init; }

	[StringLength(1000, ErrorMessage = "Opslag må højest være 1000 karakterer lang.")]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Opslag skal have mindst 1 karakter.")]
	public required string Text { get; init; }

	public int? ZipCode { get; set; }
	public string? City { get; set; }

	public DateTimeOffset CreatedUtc { get; init; }

	public required UserSlim Author { get; init; }

	public List<string> Categories { get; set; } = [];
}