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

	public int UnreadMessageCount { get; set; }

	public List<ChatMessageDto> Messages { get; set; } = [];
	public List<UserSlim> Recipients { get; init; } = [];

	public DateTime LastMessageSentUtc { get; init; }
	public DateTime StartedUtc { get; init; } = DateTime.UtcNow;
	public bool HasUnreadMessages => UnreadMessageCount > 0;
}
