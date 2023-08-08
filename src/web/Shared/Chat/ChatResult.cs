namespace Jordnaer.Shared;

public class ChatResult
{
    public List<ChatDto> Chats { get; init; } = new();

    public int TotalCount { get; init; }
}