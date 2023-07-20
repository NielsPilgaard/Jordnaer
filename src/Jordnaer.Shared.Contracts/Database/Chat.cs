using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared.Contracts;

public class Chat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the chat.
    /// <para>
    /// This defaults to a concatenated string of recipient names.
    /// </para>
    /// </summary>
    public required string DisplayName { get; set; }

    public IEnumerable<ChatMessage> Messages { get; set; } = Enumerable.Empty<ChatMessage>();
    public IEnumerable<UserChat> Recipients { get; set; } = Enumerable.Empty<UserChat>();

    public DateTime LastMessageSentUtc { get; set; }
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
}

