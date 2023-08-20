namespace Jordnaer.Shared;

public static class ChatMessageDtoExtensions
{
    public static ChatMessage ToChatMessage(this ChatMessageDto message, Guid chatId) => new()
    {
        Id = message.Id,
        ChatId = chatId,
        SenderId = message.Sender.Id,
        Text = message.Text,
        AttachmentUrl = message.AttachmentUrl,
        SentUtc = message.SentUtc
    };
}
