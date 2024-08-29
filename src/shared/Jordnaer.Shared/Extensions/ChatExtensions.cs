namespace Jordnaer.Shared;

public static class ChatExtensions
{
	public static ChatDto ToChatDto(this Chat chat) =>
		new()
		{
			DisplayName = chat.DisplayName,
			Id = chat.Id,
			LastMessageSentUtc = chat.LastMessageSentUtc,
			StartedUtc = chat.StartedUtc,
			Recipients = chat.Recipients.Count == 0
				? []
				: chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
			Messages = chat.Messages.Count == 0
				? []
				: chat.Messages.Select(chatMessage => chatMessage.ToChatMessageDto()).ToList()
		};
}
