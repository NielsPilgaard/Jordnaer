namespace Jordnaer.Shared;

public static class ChatMessageExtensions
{
    public static ChatMessageDto ToChatMessageDto(this ChatMessage chatMessage) =>
        new()
        {
            Id = chatMessage.Id,
            ChatId = chatMessage.ChatId,
            AttachmentUrl = chatMessage.AttachmentUrl,
            Sender = chatMessage.Sender.ToUserSlim(),
            Text = chatMessage.Text,
            SentUtc = chatMessage.SentUtc,
            IsDeleted = chatMessage.IsDeleted
        };

    public static ChatMessageDto ToChatMessageDto(this ChatMessage chatMessage, UserSlim sender) =>
        new()
        {
            Id = chatMessage.Id,
            ChatId = chatMessage.ChatId,
            AttachmentUrl = chatMessage.AttachmentUrl,
            Sender = sender,
            Text = chatMessage.Text,
            SentUtc = chatMessage.SentUtc,
            IsDeleted = chatMessage.IsDeleted
        };
}
