using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared;

public class Notification
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	[ForeignKey(nameof(Recipient))]
	public required string RecipientId { get; set; }

	[MaxLength(200)]
	public required string Title { get; set; }

	[MaxLength(1000)]
	public string? Description { get; set; }

	[MaxLength(2048)]
	[Url]
	public string? ImageUrl { get; set; }

	[MaxLength(2048)]
	public string? LinkUrl { get; set; }

	public NotificationType Type { get; set; }
	public bool IsRead { get; set; }
	public DateTime CreatedUtc { get; set; }
	public DateTime? ReadUtc { get; set; }

	[MaxLength(50)]
	public string? SourceType { get; set; }

	[MaxLength(450)]
	public string? SourceId { get; set; }

	public UserProfile Recipient { get; set; } = null!;
}
