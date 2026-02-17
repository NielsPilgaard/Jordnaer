using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.Notifications;

public class CreateNotificationRequest
{
	public required string RecipientId { get; set; }

	[MaxLength(200)]
	public required string Title { get; set; }

	[MaxLength(1000)]
	public string? Description { get; set; }

	[MaxLength(2048)]
	public string? ImageUrl { get; set; }

	[MaxLength(2048)]
	public string? LinkUrl { get; set; }

	public NotificationType Type { get; set; }

	[MaxLength(50)]
	public string? SourceType { get; set; }

	[MaxLength(450)]
	public string? SourceId { get; set; }

	public bool SendEmail { get; set; }
	public string? EmailSubject { get; set; }

	/// <summary>
	/// Pre-rendered HTML email body. When set, overrides the default <see cref="GenericNotification"/> template.
	/// </summary>
	public string? EmailBody { get; set; }
}
