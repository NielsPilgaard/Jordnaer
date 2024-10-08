using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(SentUtc), IsDescending = [true])]
public class ChatMessage
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	public UserProfile Sender { get; set; } = null!;

	[ForeignKey(nameof(UserProfile))]
	public required string SenderId { get; set; }

	[ForeignKey(nameof(Chat))]
	public Guid ChatId { get; set; }

	public required string Text { get; set; }

	public DateTime SentUtc { get; set; }

	public string? AttachmentUrl { get; set; }
}
