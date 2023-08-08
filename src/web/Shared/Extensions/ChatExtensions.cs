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
                ? new List<UserSlim>()
                : chat.Recipients.Select(recipient => recipient.ToUserSlim()).ToList(),
            Messages = chat.Messages.Count == 0
                ? new List<ChatMessageDto>()
                : chat.Messages.Select(chatMessage => chatMessage.ToChatMessageDto()).ToList()
        };
}
