using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.Notifications;

public class NotificationDto
{
	public required Guid Id { get; init; }
	public required string RecipientId { get; init; }

	[MaxLength(200)]
	public required string Title { get; init; }

	[MaxLength(1000)]
	public string? Description { get; init; }

	[MaxLength(2048)]
	public string? ImageUrl { get; init; }

	[MaxLength(2048)]
	public string? LinkUrl { get; init; }

	public NotificationType Type { get; init; }
	public bool IsRead { get; set; }
	public DateTime CreatedUtc { get; init; }
	public DateTime? ReadUtc { get; set; }

	[MaxLength(50)]
	public string? SourceType { get; init; }

	[MaxLength(450)]
	public string? SourceId { get; init; }

	public void MarkAsRead()
	{
		if (!IsRead)
		{
			IsRead = true;
			ReadUtc = DateTime.UtcNow;
		}
	}
}
