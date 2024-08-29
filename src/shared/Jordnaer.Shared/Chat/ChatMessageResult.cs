namespace Jordnaer.Shared;

public class ChatMessageResult
{
	public List<ChatMessageDto> ChatMessages { get; init; } = [];

	public int TotalCount { get; init; }
}