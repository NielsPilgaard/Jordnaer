namespace Jordnaer.Shared;

public static class StartChatExtensions
{
    public static ChatDto ToChatDto(this StartChat startChat) =>
        new()
        {
            Id = startChat.Id,
            Recipients = startChat.Recipients,
            Messages = startChat.Messages,
            LastMessageSentUtc = startChat.LastMessageSentUtc,
            StartedUtc = startChat.StartedUtc,
            DisplayName = startChat.DisplayName,
            UnreadMessageCount = 0
        };
}
