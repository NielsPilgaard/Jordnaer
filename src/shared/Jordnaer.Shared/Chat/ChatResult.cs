namespace Jordnaer.Shared;

public class ChatResult
{
	public List<ChatDto> Chats { get; init; } = [];

	public int TotalCount { get; init; }
}