using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared;

public class UnreadMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [ForeignKey(nameof(Chat))]
    public Guid ChatId { get; init; }

    [ForeignKey(nameof(UserProfile))]
    public required string SenderId { get; init; }

    [ForeignKey(nameof(UserProfile))]
    public required string RecipientId { get; init; }

    public DateTime MessageSentUtc { get; init; }
}
