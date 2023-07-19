namespace Jordnaer.Shared.Contracts;

public class Chat
{
    public required string SenderId { get; set; }
    /// <summary>
    /// The display name of the chat.
    /// <para>
    /// This defaults to a concatenated string of recipient names.
    /// </para>
    /// </summary>
    public string DisplayName { get; set; } = null!;

    public List<ChatMessage> Messages { get; set; } = new();
    public List<Guid> RecipientIds { get; set; } = new();
}

