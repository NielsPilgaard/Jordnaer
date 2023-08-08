using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(SentUtc), IsDescending = new[] { true })]
public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public UserProfile Sender { get; set; } = null!;
    public required string SenderId { get; set; }

    public Chat? Chat { get; set; }
    public Guid ChatId { get; set; }

    public required string Text { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime SentUtc { get; set; }

    public string? AttachmentUrl { get; set; }
}
