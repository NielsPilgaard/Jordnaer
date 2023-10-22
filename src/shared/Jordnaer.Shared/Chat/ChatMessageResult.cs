namespace Jordnaer.Shared;

public class ChatMessageResult
{
    public List<ChatMessageDto> ChatMessages { get; init; } = new();

    public int TotalCount { get; init; }
}