using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared.Contracts;

public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public required UserProfile Sender { get; set; }
    public required string SenderId { get; set; }

    public required Chat Chat { get; set; }
    public Guid ChatId { get; set; }

    public required string Text { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime SentUtc { get; set; } = DateTime.UtcNow;

    public string? AttachmentUrl { get; set; }
}
