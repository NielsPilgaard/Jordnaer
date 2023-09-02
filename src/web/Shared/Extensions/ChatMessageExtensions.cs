namespace Jordnaer.Shared;

public static class ChatMessageExtensions
{
    public static ChatMessageDto ToChatMessageDto(this ChatMessage chatMessage) =>
        new()
        {
            Id = chatMessage.Id,
            ChatId = chatMessage.ChatId,
            AttachmentUrl = chatMessage.AttachmentUrl,
            SenderId = chatMessage.SenderId,
            Text = chatMessage.Text,
            SentUtc = chatMessage.SentUtc
        };
}
