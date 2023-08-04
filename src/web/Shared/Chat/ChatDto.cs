namespace Jordnaer.Shared;

public class ChatDto
{
    public Guid Id { get; init; }

    /// <summary>
    /// The display name of the chat.
    /// <para>
    /// This defaults to a concatenated string of recipient names.
    /// </para>
    /// </summary>
    public string? DisplayName { get; init; }

    public List<ChatMessageDto> Messages { get; init; } = new();
    public List<ChatUserDto> Recipients { get; init; } = new();

    public DateTime LastMessageSentUtc { get; init; }
    public DateTime StartedUtc { get; init; } = DateTime.UtcNow;
}

